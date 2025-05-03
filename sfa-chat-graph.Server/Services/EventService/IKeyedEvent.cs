namespace SfaChatGraph.Server.Services.EventService
{
	public interface IKeyedEvent<TKey> : IEvent
	{
		public TKey Key { get; }
	}
}
