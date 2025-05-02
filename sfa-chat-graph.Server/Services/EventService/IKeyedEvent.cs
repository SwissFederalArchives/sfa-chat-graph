namespace sfa_chat_graph.Server.Services.EventService
{
	public interface IKeyedEvent<TKey> : IEvent
	{
		public TKey Key { get; }
	}
}
