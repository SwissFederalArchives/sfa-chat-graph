using J2N.Threading.Atomic;
using SfaChatGraph.Server.Utils;
using System.Buffers;
using System.Net.WebSockets;

namespace sfa_chat_graph.Server.Services.EventService.WebSockets
{
	public class WebsocketTarget : IEventTarget<ReadOnlySequence<byte>>
	{
		private static AtomicInt64 _idCounter = new AtomicInt64();
		private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create();

		private readonly CancellationTokenSource _cts = new();
		private readonly WebSocket _socket;
		private readonly ILogger _logger;
		private readonly byte[] _buffer;
		private readonly Task _closeTask;
		private readonly WebSocketMessageType _messageType;

		public bool IsDisposed { get; private set; }
		public long Id { get; init; }

		public WebsocketTarget(WebSocket socket, ILoggerFactory loggerFactory, WebSocketMessageType messageType)
		{
			_socket = socket;
			_buffer = _pool.Rent(4096);
			Id = _idCounter.IncrementAndGet();
			_logger = loggerFactory.CreateLogger($"{nameof(WebsocketTarget)}[{Id}]");
			_closeTask = socket.ReceiveAsync(_buffer, _cts.Token).ContinueWith(HandleReceive);
			_messageType=messageType;
			_logger.LogInformation("New target created: {TargetId}", Id);
		}


		public async Task<(IEventTarget<ReadOnlySequence<byte>> target, bool error)> SendAsync(ReadOnlySequence<byte> message)
		{
			if (IsDisposed)
				return (this, true);

			try
			{
				foreach (var (seq, isLast) in message.AsIsLast())
					await _socket.SendAsync(seq, _messageType, isLast, CancellationToken.None);
			}
			catch (WebSocketException ex)
			{
				_logger.LogError(ex, "Error while sending");
				return (this, true);
			}

			return (this, false);
		}

		private async Task HandleReceive(Task<WebSocketReceiveResult> task)
		{
			if (task.IsFaulted)
			{
				_logger.LogError(task.Exception, "Error while receiving");
				return;
			}

			var result = task.Result;
			if (result.CloseStatus.HasValue)
			{
				await _socket.CloseAsync(result.CloseStatus.Value, "ACK", CancellationToken.None);
			}
			else
			{
				await _socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Client is not supposed to send data", CancellationToken.None);
				_logger.LogError("Client sent data to server: {Message}", result.MessageType.ToString());
			}
		}

		public async Task<IEventTarget<ReadOnlySequence<byte>>> WaitForCloseAsync(CancellationToken token)
		{
			if (IsDisposed)
				return this;

			await Task.WhenAny(Task.Delay(Timeout.Infinite, token), _closeTask);
			return this;
		}

		public async Task CloseAsync()
		{
			await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
		}

		public async ValueTask DisposeAsync()
		{
			if(IsDisposed)
				return;

			if (_socket.State == WebSocketState.Open)
				await CloseAsync();

			IsDisposed = true;
			_pool.Return(_buffer);
			_socket.Dispose();
			_cts.Cancel();
			_logger.LogInformation("Target disposed: {TargetId}", Id);
		}
	}
}
