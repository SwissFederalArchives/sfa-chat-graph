using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.ChatService.Events;
using sfa_chat_graph.Server.Services.EventService;

namespace sfa_chat_graph.Server.Services.ChatService.OpenAI
{
	public class OpenAiChatContext : ChatContext
	{
		private readonly List<ChatMessage> _openAiHistory;
		public IEnumerable<ChatMessage> OpenAIHistory => _openAiHistory;

		public OpenAiChatContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history, ChatMessage sysPrompt) : base(chatId, events, history)
		{
			_openAiHistory = history.Select(x => x.AsOpenAIMessage()).ToList();
			_openAiHistory.Insert(0, sysPrompt);
		}

		public void AddMessage(ApiMessage message)
		{
			Created.Add(message);
			_openAiHistory.Add(message.AsOpenAIMessage());
		}

		public void AddMessage(ChatMessage message)
		{
			Created.Add(message.AsApiMessage());
			_openAiHistory.Add(message);
		}

		public void AddMessage(Message message)
		{
			_openAiHistory.Add(message.OpenAi);
			Created.Add(message.Api);
		}
	}
}
