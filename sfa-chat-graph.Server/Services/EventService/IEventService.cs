using System.Net.WebSockets;

namespace sfa_chat_graph.Server.Services.EventService
{
	public interface IEventService<TChannel, TEvent, TTarget, TMessage> where TEvent : IEvent where TTarget : IEventTarget<TMessage>
	{
		public void RegisterTarget(TChannel key, TTarget target);		
		public IEventChannel<TChannel, TEvent, TTarget, TMessage> GetChannel(TChannel key);
	}
}
