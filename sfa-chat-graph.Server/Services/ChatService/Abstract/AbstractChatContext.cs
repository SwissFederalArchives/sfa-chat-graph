using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.ChatService.Events;
using sfa_chat_graph.Server.Services.ChatService.OpenAI;
using sfa_chat_graph.Server.Services.EventService;

namespace sfa_chat_graph.Server.Services.ChatService.Abstract
{
	public abstract class AbstractChatContext<TMessage> : ChatContext
	{
		private readonly List<TMessage> _messages;
		public IEnumerable<TMessage> InternalHistory => _messages;

		public AbstractChatContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history) : base(chatId, events, history)
		{
			_messages = history.Select(ToInternalMessage).ToList();
		}

		public abstract TMessage ToInternalMessage(ApiMessage message);
		public abstract ApiMessage ToApiMessage(TMessage message, ApiGraphToolData graphToolData = null, ApiCodeToolData codeToolData = null);

		public override void AddUserMessage(ApiMessage message)
		{
			base.AddUserMessage(message);
			_messages.Add(ToInternalMessage(message));
		}

		public void AddMessage(ApiMessage message)
		{
			_messages.Add(ToInternalMessage(message));
			AddCreated(message);
		}

		public void AddMessage(TMessage message)
		{
			_messages.Add(message);
			AddCreated(ToApiMessage(message));
		}

		public void AddMessage(Message<TMessage> message)
		{
			_messages.Add(message.InternalMessage);
			AddCreated(message.ApiMessage);
		}
	}
}
