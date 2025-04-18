using sfa_chat_graph.Server.Services.EventService.InMemory;
using System.Text.Json;

namespace sfa_chat_graph.Server.Services.ChatService.Events
{
	public class ChatServiceEventService : JsonWebsocketEventService<Guid, ChatEvent>
	{
		public ChatServiceEventService(ILoggerFactory loggerFactory, JsonSerializerOptions options = null) : base(loggerFactory, options)
		{
		}
	}
}
