using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.ChatService.Events;
using sfa_chat_graph.Server.Services.EventService;

namespace sfa_chat_graph.Server.Services.ChatService
{	
	public class ChatContext
	{
		public Guid ChatId { get; init; }
		public List<ApiMessage> Created { get; init; } = new();
		public ApiMessage[] History { get; init; }

		private readonly IEventSink<ChatEvent> _events;

		public async Task NotifyActivityAsync(string activity, string? detail = null)
		{
			await _events?.PushAsync(ChatEvent.CActivity(ChatId, activity, detail));
		}

		public async Task NotifyDoneAsync()
		{
			await _events?.PushAsync(ChatEvent.CDone(ChatId));
		}

		public IEnumerable<ApiToolResponseMessage> ToolResponses => History.OfType<ApiToolResponseMessage>().Concat(Created.OfType<ApiToolResponseMessage>());

		public ChatContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history)
		{
			this.History = history.ToArray();
			this.ChatId =chatId;
			_events = events;
		}
	}
}
