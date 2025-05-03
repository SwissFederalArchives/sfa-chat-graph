namespace SfaChatGraph.Server.Services.EventService
{
	public interface IEventSink<TEvent>
	{
		public Task PushAsync(TEvent @event);
	}
}
