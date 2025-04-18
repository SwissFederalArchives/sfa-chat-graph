namespace sfa_chat_graph.Server.Services.EventService
{
	public interface IEventSink<TEvent>
	{
		public Task PushAsync(TEvent @event);
	}
}
