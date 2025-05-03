using OpenAI.Chat;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Services.ChatService.Abstract;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.EventService;

namespace SfaChatGraph.Server.Services.ChatService.OpenAI
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
