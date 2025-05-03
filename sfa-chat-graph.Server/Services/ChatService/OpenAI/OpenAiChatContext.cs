using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.ChatService.Abstract;
using sfa_chat_graph.Server.Services.ChatService.Events;
using sfa_chat_graph.Server.Services.EventService;

namespace sfa_chat_graph.Server.Services.ChatService.OpenAI
{
	public class OpenAIChatContext : AbstractChatContext<ChatMessage>
	{


		public OpenAIChatContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history) : base(chatId, events, history)
		{
		}

		public override ApiMessage ToApiMessage(ChatMessage message, ApiGraphToolData graphToolData = null, ApiCodeToolData codeToolData = null)
		{
			if (graphToolData != null)
				return message.AsApiMessage(graphToolData);

			if(codeToolData != null)
				return message.AsApiMessage(codeToolData);

			return message.AsApiMessage();
		}

		public override ChatMessage ToInternalMessage(ApiMessage message) => message.AsOpenAIMessage();
	}
}
