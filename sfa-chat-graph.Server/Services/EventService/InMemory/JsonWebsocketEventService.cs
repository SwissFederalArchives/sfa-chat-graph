using SfaChatGraph.Server.Services.EventService.WebSockets;
using System.Buffers;
using System.Text.Json;

namespace SfaChatGraph.Server.Services.EventService.InMemory
{
	public class JsonWebsocketEventService<TChannel, TEvent> : InMemoryEventService<TChannel, TEvent, WebsocketTarget, ReadOnlySequence<byte>> where TEvent : IEvent
	{
		public JsonWebsocketEventService(ILoggerFactory loggerFactory, JsonSerializerOptions? options = null) : base(new WebsocketJsonEventProtocol<TEvent>(options), loggerFactory)
		{
		}
	}
}
