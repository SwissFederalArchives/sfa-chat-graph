using SfaChatGraph.Server.Services.EventService.InMemory;
using System.Text.Json;

namespace SfaChatGraph.Server.Services.ChatService.Events
{
	public class ChatServiceEventService : JsonWebsocketEventService<Guid, ChatEvent>
	{
		public ChatServiceEventService(ILoggerFactory loggerFactory, JsonSerializerOptions options = null) : base(loggerFactory, options)
		{
		}
	}
}
