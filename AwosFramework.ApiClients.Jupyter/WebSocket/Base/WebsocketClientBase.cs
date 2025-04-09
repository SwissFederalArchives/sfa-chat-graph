using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Base
{
	public abstract class WebsocketClientBase<TOptions, TProtocol, TMsg, TErr> where TOptions : IWebsocketOptions where TProtocol : IProtocol<TMsg, TErr> where TErr : IError
	{
		public event Action<WebsocketState>? StateChanged;
		public event Action<TMsg>? OnSend;
		public event Action<TMsg>? OnReceive;


		protected readonly ILogger? _logger;
		private CancellationTokenSource? _stopSocket;
		private ClientWebSocket _socket;
		protected CancellationToken CancellationToken => _stopSocket?.Token ?? CancellationToken.None;


		public TOptions Options { get; init; }

		[MemberNotNullWhen(true, nameof(IOTask))]
		public bool IsConnected => State == WebsocketState.Connected;
		public bool IsDisconnected => State == WebsocketState.Disconnected;
		public bool IsDisposed => State == WebsocketState.Disposed || State == WebsocketState.Errored;
		
		public WebsocketState State { get; private set; } = WebsocketState.Disconnected;
		public Task? IOTask { get; private set; }
		public Exception? Exception { get; private set; }

		private void ThrowIfDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(JupyterWebsocketClient));
		}

		protected void SetState(WebsocketState state)
		{
			ThrowIfDisposed();
			if (state != State)
			{
				State = state;
				StateChanged?.Invoke(state);
				_logger?.LogInformation("Websocket state changed to {State}", state);
			}
		}

		public WebsocketClientBase(TOptions options)
		{
			Options = options ?? throw new ArgumentNullException(nameof(options));
			_logger = options.LoggerFactory?.CreateLogger(GetType());
			_socket = CreateWebSocket(options);
			IOTask = null;
		}

		protected abstract Task HandleResultAsync(TMsg message);
		protected abstract Task<TMsg> NextMessagAsync(CancellationToken token); 

		private async Task SocketSendAsync(ReadOnlyMemory<byte> message, bool lastMessage)
		{
			await _socket.SendAsync(message, WebSocketMessageType.Binary, lastMessage, _stopSocket!.Token);
		}

		private async Task SendLoopAsync(TProtocol protocol, CancellationToken token)
		{
			while (token.IsCancellationRequested == false)
			{
				var message = await NextMessagAsync(token);
				if (message == null)
					break;

				OnSend?.Invoke(message);
				var countWritten = await protocol.SendAsync(message, SocketSendAsync);
			}
		}

		private async Task ReceiveLoopAsync(TProtocol protocol, CancellationToken token)
		{
			var _bufferRaw = Options.ArrayPool.Rent(1024*1024*16);
			try
			{
				var memory = _bufferRaw.AsMemory();
				int receiveOffset = 0;
				while (token.IsCancellationRequested == false)
				{
					var received = await _socket.ReceiveAsync(memory.Slice(receiveOffset), token);
					_logger?.LogDebug("Received {Count} bytes, End of message: {EndOfMessage}", received.Count, received.EndOfMessage);
					var receivedCount = received.Count;
					int countRead = 0;

					do
					{
						var result = await protocol.ReadAsync(memory[countRead..receivedCount], received.EndOfMessage);
						countRead += result.CountRead;

						if (result.IsError(out var error))
						{
							if (received.EndOfMessage == false)
								await _socket.WaitForEndOfMessageAsync(memory, token);

							countRead = receivedCount;
							_logger?.LogError(error.Exception, "Error parsing message: {ErrorCode}", error.ErrorCode);
						}

						if (result.IsCompleted(out var message))
						{
							OnReceive?.Invoke(message);
							await HandleResultAsync(message);
						}

					} while (countRead < receivedCount);

					receiveOffset = receivedCount - countRead;
				}
			}
			finally
			{
				Options.ArrayPool.Return(_bufferRaw);
			}
		}


		private static ClientWebSocket CreateWebSocket(IWebsocketOptions options)
		{
			var socket = new ClientWebSocket();
			if (options.HasToken(out var token))
				socket.Options.SetRequestHeader("Authorization", $"token {token}");

			foreach (var protocol in IProtocol<TMsg, TErr>.Implementations.Keys)
				if (string.IsNullOrEmpty(protocol) == false)
					socket.Options.AddSubProtocol(protocol);

			return socket;
		}




		private async Task<bool> TryConnectAsync(CancellationToken token)
		{
			try
			{
				await _socket.ConnectAsync(Options.GetConnectionUri(), token);
				return true;
			}
			catch (WebSocketException)
			{
				return false;
			}
		}

		private async Task<(bool success, Task? read, Task? send, TProtocol protocol, CancellationToken token)> HandleReconnectAsync(CancellationToken token, TProtocol protocol)
		{
			_stopSocket?.Cancel();
			if (Options.TryReconnect)
			{
				_logger?.LogInformation("Reconnecting to websocket...");
				SetState(WebsocketState.Reconnecting);
				_stopSocket = new CancellationTokenSource();
				token = _stopSocket.Token;

				var count = Options.MaxReconnectTries.Value;
				while (count-- > 0)
				{
					var result = await TryConnectAsync(token);
					if (result)
					{
						protocol?.Dispose();
						protocol = (TProtocol)IProtocol<TMsg, TErr>.CreateInstance(_socket.SubProtocol, Options);
						var receive = ReceiveLoopAsync(protocol, token);
						var send = SendLoopAsync(protocol, token);
						SetState(WebsocketState.Connected);
						_logger?.LogInformation("Reconnected to websocket");
						return (true, receive, send, protocol, token);
					}

					await Task.Delay(Options.ReconnectDelay);
					_logger?.LogDebug(count, "Reconnect attempt {Attempt}/{MaxAttempts} failed", Options.MaxReconnectTries-count, Options.MaxReconnectTries);
				}

			}
			return (false, null, null, protocol, token);
		}

		private async Task IOLoopAsync(TProtocol protocol, CancellationToken token)
		{
			try
			{
				var receive = ReceiveLoopAsync(protocol, token);
				var send = SendLoopAsync(protocol, token);

				while (token.IsCancellationRequested == false)
				{
					var returned = await Task.WhenAny(receive, send);
					if (returned.IsFaulted && token.IsCancellationRequested == false) // restart logic
					{
						// only try to reconnect if its actually a websocket disconnect error
						if (returned.Exception.InnerException is WebSocketException wse && wse.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
						{
							var (success, newReceive, newSend, newProtocol, newToken) = await HandleReconnectAsync(token, protocol);
							if (success)
							{
								receive = newReceive!;
								send = newSend!;
								protocol = newProtocol;
								token = newToken;
								continue;
							}
						}
						else
						{
							// something else failed, transition to errored state
							_stopSocket?.Cancel();
							Exception = returned.Exception;
							SetState(WebsocketState.Errored);
							DisposeImpl();
							_logger?.LogError(returned.Exception, "Unexpected Websocket Exception {Message}, shutting down socket...", returned.Exception.Message);
							return;
						}

					}
				}

				SetState(WebsocketState.Disconnected);
			}
			finally
			{
				protocol.Dispose();
				IOTask = null;
			}
		}


		protected virtual void DisposeImpl()
		{
			_stopSocket?.Cancel();
			_socket.Dispose();
		}

		public void Dispose()
		{
			DisposeImpl();
			SetState(WebsocketState.Disposed);
		}

		public async Task ConnectAsync()
		{
			ThrowIfDisposed();
			if (IsDisconnected)
			{
				SetState(WebsocketState.Connecting);
				_stopSocket = new CancellationTokenSource();
				await _socket.ConnectAsync(Options.GetConnectionUri(), _stopSocket.Token);
				_stopSocket.Token.ThrowIfCancellationRequested();
				var protocol = (TProtocol)IProtocol<TMsg, TErr>.CreateInstance(_socket.SubProtocol, Options);
				IOTask = IOLoopAsync(protocol, _stopSocket.Token);
				SetState(WebsocketState.Connected);
			}
		}

		public async Task DisconnectAsync()
		{
			ThrowIfDisposed();
			if (IsConnected)
			{
				SetState(WebsocketState.Disconnecting);
				_stopSocket?.Cancel();
				await IOTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
				await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
				SetState(WebsocketState.Disconnected);
			}
		}


	}
}
