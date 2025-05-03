using System.Net.WebSockets;

namespace SfaChatGraph.Server.Services.EventService
{
	public interface IEventService<TChannel, TEvent, TTarget, TMessage> where TEvent : IEvent where TTarget : IEventTarget<TMessage>
	{
		public void RegisterTarget(TChannel key, TTarget target);		
		public IEventChannel<TChannel, TEvent, TTarget, TMessage> GetChannel(TChannel key);
		public Task PushAsync<T>(T keyedEvent) where T : TEvent, IKeyedEvent<TChannel>;
	}
}
