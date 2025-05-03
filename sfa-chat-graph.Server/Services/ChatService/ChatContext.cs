using OpenAI.Chat;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.EventService;

namespace SfaChatGraph.Server.Services.ChatService
{	
	public class ChatContext : IChatActivity
	{
		public Guid ChatId { get; init; }
		private List<ApiMessage> _created { get; init; } = new();
		public ApiMessage[] History { get; init; }
		private int _nextMessageIndex;

		public IEnumerable<ApiMessage> Created => _created;

		private readonly IEventSink<ChatEvent> _events;

		public virtual void AddUserMessage(ApiMessage message)
		{
			if (message.Role != ChatRole.User)
				throw new ArgumentException($"Message role must be {ChatRole.User} but was {message.Role}");

			AddCreated(message);
		}

		protected void AddCreated(ApiMessage message)
		{
			message.Index = _nextMessageIndex++;
			_created.Add(message);
		}

		public async Task NotifyActivityAsync(string activity, string? detail, string? trace = null)
		{
			await _events?.PushAsync(ChatEvent.CActivity(ChatId, activity, detail, trace));
		}

		public Task NotifyActivityAsync(string activity) => NotifyActivityAsync(activity, null);

		public async Task NotifyDoneAsync()
		{
			await _events?.PushAsync(ChatEvent.CDone(ChatId));
		}

		public IEnumerable<ApiToolResponseMessage> ToolResponses => History.OfType<ApiToolResponseMessage>().Concat(_created.OfType<ApiToolResponseMessage>());

		public ChatContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history)
		{
			this.History = history.ToArray();
			this.ChatId =chatId;
			_events = events;
			_nextMessageIndex = history.Max(x => x.Index) + 1;
		}
	}
}
