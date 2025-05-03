using Microsoft.IO;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;

namespace SfaChatGraph.Server.Services.EventService.WebSockets
{
	public class WebsocketJsonEventProtocol<TEvent> : IEventProtocol<TEvent, ReadOnlySequence<byte>>
	{
		private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new RecyclableMemoryStreamManager();
		private readonly RecyclableMemoryStream _memoryStream;
		private readonly JsonSerializerOptions _jsonOptions;

		public WebsocketJsonEventProtocol(JsonSerializerOptions? jsonOptions = null)
		{
			_memoryStream = _memoryStreamManager.GetStream();
			_jsonOptions = jsonOptions ?? JsonSerializerOptions.Default;
		}

		public WebSocketMessageType MessageType => WebSocketMessageType.Text;

		public async Task<ReadOnlySequence<byte>> SerializeAsync(TEvent @event)
		{
			_memoryStream.SetLength(0);
			await JsonSerializer.SerializeAsync(_memoryStream, @event, _jsonOptions);
			return _memoryStream.GetReadOnlySequence();
		}
	}
}
