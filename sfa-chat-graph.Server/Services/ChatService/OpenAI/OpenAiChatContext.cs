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

		public override void AddUserMessage(ApiMessage message)
		{
			base.AddUserMessage(message);
			_openAiHistory.Add(message.AsOpenAIMessage());
		}

		public void AddMessage(ApiMessage message)
		{
			_openAiHistory.Add(message.AsOpenAIMessage());
			AddCreated(message);
		}

		public void AddMessage(ChatMessage message)
		{
			_openAiHistory.Add(message);
			AddCreated(message.AsApiMessage());
		}

		public void AddMessage(Message message)
		{
			_openAiHistory.Add(message.OpenAi);
			AddCreated(message.Api);
		}
	}
}
