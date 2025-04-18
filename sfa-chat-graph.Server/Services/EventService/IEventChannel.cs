namespace sfa_chat_graph.Server.Services.EventService
{
	public interface IEventChannel<TChannel, TEvent, TTarget, TMessage> : IEventSink<TEvent> where TEvent : IEvent where TTarget : IEventTarget<TMessage>
	{
		public void RegisterTarget(TTarget sink);
		public TChannel ChannelKey { get; }
	}
}
