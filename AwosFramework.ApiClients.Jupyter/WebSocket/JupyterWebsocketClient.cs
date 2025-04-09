using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.IOPub;
using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using AwosFramework.ApiClients.Jupyter.WebSocket.Protocol;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AwosFramework.ApiClients.Jupyter.WebSocket
{
	public class JupyterWebsocketClient : WebsocketClientBase<JupyterWebsocketOptions, IWebsocketProtocol, WebsocketMessage, WebsocketError>
	{
		
		private readonly Channel<WebsocketMessage> _receiveChannel;
		private readonly Channel<WebsocketMessage> _sendChannel;
		private readonly Dictionary<string, ObservableSource<WebsocketMessage>> _observableMessages = new();

		public ChannelReader<WebsocketMessage> MessageReader => _receiveChannel.Reader;
		private void ThrowIfDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(JupyterWebsocketClient));
		}

		private void SetState(WebsocketState state)
		{
			ThrowIfDisposed();
			if (state != State)
			{
				State = state;
				StateChanged?.Invoke(state);
				_logger?.LogInformation("Websocket state changed to {State}", state);
			}
		}

		private static ClientWebSocket CreateWebSocket(JupyterWebsocketOptions options)
		{
			var socket = new ClientWebSocket();
			if (options.HasToken(out var token))
				socket.Options.SetRequestHeader("Authorization", $"token {token}");

			foreach (var protocol in IWebsocketProtocol.Implementations.Keys)
				if (string.IsNullOrEmpty(protocol) == false)
					socket.Options.AddSubProtocol(protocol);

			return socket;
		}

		public JupyterWebsocketClient(JupyterWebsocketOptions options)
		{
			_logger = options.LoggerFactory?.CreateLogger<JupyterWebsocketClient>();
			Options = options;
			_socket = CreateWebSocket(options);


			var sendOptions = new UnboundedChannelOptions { SingleReader = true };
			_sendChannel = Channel.CreateUnbounded<WebsocketMessage>(sendOptions);
			if (options.MaxMessages.HasValue)
			{
				var channelOptions = new BoundedChannelOptions(options.MaxMessages.Value) { SingleWriter = true, FullMode = BoundedChannelFullMode.DropOldest };
				_receiveChannel = Channel.CreateBounded<WebsocketMessage>(channelOptions);
			}
			else
			{
				_receiveChannel = Channel.CreateUnbounded<WebsocketMessage>();
			}
		}

		public JupyterWebsocketClient(Uri endpoint, Guid kernelId, Guid? sessionId = null, string? token = null) : this(new JupyterWebsocketOptions(endpoint, kernelId, sessionId, token))
		{


		}

		public async Task ConnectAsync()
		{
			this.ThrowIfDisposed();
			if (IsDisconnected)
			{
				SetState(WebsocketState.Connecting);
				_stopSocket = new CancellationTokenSource();
				await _socket.ConnectAsync(Options.GetConnectionUri(), _stopSocket.Token);
				_stopSocket.Token.ThrowIfCancellationRequested();
				var protocol = IWebsocketProtocol.CreateInstance(_socket.SubProtocol, Options);
				IOTask = IOLoopAsync(protocol, _stopSocket.Token);
				SetState(WebsocketState.Connected);
			}
		}

		public async Task DisconnectAsync()
		{
			this.ThrowIfDisposed();
			if (IsConnected)
			{
				SetState(WebsocketState.Disconnecting);
				_stopSocket?.Cancel();
				await IOTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
				await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
				SetState(WebsocketState.Disconnected);
			}
		}

		private void DisposeImpl()
		{
			_stopSocket?.Cancel();
			_socket.Dispose();
			while (_receiveChannel.Reader.TryRead(out var msg))
				msg.Dispose();

			while (_sendChannel.Reader.TryRead(out var msg))
				msg.Dispose();
		}

		public void Dispose()
		{
			DisposeImpl();
			SetState(WebsocketState.Disposed);
		}

		private void HandleResult(WebsocketMessage message)
		{
			OnReceive?.Invoke(message);
			_logger?.LogDebug("Received message {MessageType} on channel {Channel}", message.Header.MessageType, message.Channel);
			if (message.Content is KernelStatusMessage status && status.ExecutionState == ExecutionState.Idle && message.ParentHeader != null && _observableMessages.TryGetValue(message.ParentHeader.Id, out var observable))
			{
				_observableMessages.Remove(message.ParentHeader.Id);
				observable.NotifyCompleted();
			}

			if(message.ParentHeader != null && message.Content is not KernelStatusMessage && _observableMessages.TryGetValue(message.ParentHeader.Id, out var ovservable))
			{
				ovservable.NotifyItem(message);
			}
			else
			{
				_receiveChannel.Writer.TryWrite(message);
			}
		}

		private async Task SocketSendAsync(ReadOnlyMemory<byte> message, bool lastMessage)
		{
			await _socket.SendAsync(message, WebSocketMessageType.Binary, lastMessage, _stopSocket!.Token);
		}

		private WebsocketMessage CreateMessageFromPayload(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, WebsocketMessage? parent = null)
		{
			ArgumentNullException.ThrowIfNull(payload, nameof(payload));
			var attrs = payload.GetType().GetCustomAttributes<MessageTypeAttribute>().ToArray();
			var attr = attrs.Length > 1 ? attrs.FirstOrDefault(x => x.MessageType.Contains("request")) : attrs.FirstOrDefault();
			if (attr == null)
				throw new ArgumentException($"Type {payload.GetType()} does not have a MessageTypeAttribute");

			var message = new WebsocketMessage
			{
				TransferableBuffers = buffers,
				Channel = attr.Channel,
				Content = payload,
				ParentHeader = parent?.Header,
				MetaData = metaData,
				Header = new Models.MessageHeader
				{
					Id = Guid.NewGuid().ToString(),
					MessageType = attr.MessageType,
					SessionId = Options.SessionId.ToString(),
					UserName = Options.UserName,
					Version = attr.Version,
					SubshellId = null,
					Timestamp = DateTime.UtcNow
				}
			};

			return message;
		}

		public async Task<IObservable<WebsocketMessage>> SendAndObserveAsync(WebsocketMessage message)
		{
			var observable = new ObservableSource<WebsocketMessage>();
			_observableMessages[message.Header!.Id] = observable;
			await SendAsync(message);
			return observable;
		}

		public async Task<IObservable<WebsocketMessage>> SendAndObserveAsync(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, WebsocketMessage? parent = null)
		{
			var message = CreateMessageFromPayload(payload, buffers, metaData, parent);
			var observable = new ObservableSource<WebsocketMessage>();
			_observableMessages[message.Header!.Id] = observable;
			await SendAsync(message);
			return observable;
		}


		public async Task<WebsocketMessage> SendAsync(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, WebsocketMessage? parent = null)
		{
			var message = CreateMessageFromPayload(payload, buffers, metaData, parent);
			await SendAsync(message);
			return message;
		}

		public async Task SendAsync(WebsocketMessage message)
		{
			await _sendChannel.Writer.WriteAsync(message, _stopSocket.Token);
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

		private async Task<(bool success, Task? read, Task? send, IWebsocketProtocol protocol, CancellationToken token)> HandleReconnectAsync(CancellationToken token, IWebsocketProtocol protocol)
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
						protocol = IWebsocketProtocol.CreateInstance(_socket.SubProtocol, Options);
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

		private async Task IOLoopAsync(IWebsocketProtocol protocol, CancellationToken token)
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
							this.Exception = returned.Exception;
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

		private async Task SendLoopAsync(IWebsocketProtocol protocol, CancellationToken token)
		{
			while (token.IsCancellationRequested == false)
			{
				var message = await _sendChannel.Reader.ReadAsync(token);
				if (message == null)
					break;

				OnSend?.Invoke(message);
				var countWritten = await protocol.SendAsync(message, SocketSendAsync);
			}
		}

		private async Task ReceiveLoopAsync(IWebsocketProtocol protocol, CancellationToken token)
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
							HandleResult(message);

					} while (countRead < receivedCount);

					receiveOffset = receivedCount - countRead;
				}
			}
			finally
			{
				Options.ArrayPool.Return(_bufferRaw);
			}
		}


	}
}
