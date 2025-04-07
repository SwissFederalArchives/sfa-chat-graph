using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket
{
	public class JupyterWebsocketClient : IDisposable
	{
		private ClientWebSocket _socket;
		private Uri _endpoint;
		private IMemoryOwner<byte> _buffer;
		private ParserState _parserState;
		private Task? _readTask;
		private CancellationTokenSource? _cancelReadTask;
		public event Action<WebsocketMessage>? OnReceive;
		public Task ReadTask => _readTask;

		public JupyterWebsocketClient(Uri endpoint, Guid kernelId, Guid sessionId, string? token = null)
		{
			if (endpoint.Segments.Last().Equals("api", StringComparison.OrdinalIgnoreCase) == false)
				endpoint = new Uri(endpoint, "api/");

			var tempUri = $"kernels/{kernelId}/channels?session_id={sessionId}";

			_socket = new ClientWebSocket();
			_endpoint = new Uri(endpoint, tempUri);
			_buffer = MemoryPool<byte>.Shared.Rent(4096);
			_parserState = new ParserState(ArrayPool<byte>.Shared);
			if (token != null)
				_socket.Options.SetRequestHeader("Authorization", $"token {token}");

			_socket.Options.AddSubProtocol("v1.kernel.websocket.jupyter.org");
		}

		public async Task ConnectAsync()
		{
			await _socket.ConnectAsync(_endpoint, CancellationToken.None);
			if (_readTask != null)
				await DisconnectAsync();

			_cancelReadTask = new CancellationTokenSource();
			_readTask = ReceiveAsync(_cancelReadTask.Token);
		}

		public async Task DisconnectAsync()
		{
			if (_readTask == null)
			{
				_cancelReadTask?.Cancel();
				await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
				await _readTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
				_readTask = null;
				_parserState.Reset();
			}
		}

		public void Dispose()
		{
			_buffer.Dispose();
			_parserState.Reset();
		}

		private void HandleResult(WebsocketMessage message)
		{
			OnReceive?.Invoke(message);
		}

		private async Task ReceiveAsync(CancellationToken token)
		{
			int receiveOffset = 0;
			var memory = _buffer.Memory;
			while (token.IsCancellationRequested == false)
			{
				var received = await _socket.ReceiveAsync(memory.Slice(receiveOffset), token);
				var receivedCount = received.Count;
				int countRead = 0;

				do
				{
					countRead += WebsocketFrameParser.Parse(memory.Slice(countRead, receivedCount - countRead).Span, ref _parserState);
					if (_parserState.IsErrorState(out var errorCode))
					{
						// handle error
						if (received.EndOfMessage == false)
							await _socket.WaitForEndOfMessageAsync(_buffer.Memory, token);

						countRead = receivedCount;
						break;
					}

					if (_parserState.State == WebsocketFrameParserState.End)
					{
						HandleResult(_parserState.PartialMessage);
					}

				} while (countRead < receivedCount);

				receiveOffset = receivedCount - countRead;
			}
		}


	}
}
