namespace sfa_chat_graph.Server.Services.EventService
{
	public interface IEventTarget<TMessage> : IAsyncDisposable
	{
		public Task<(IEventTarget<TMessage> target, bool error)> SendAsync(TMessage message);
		public Task<IEventTarget<TMessage>> WaitForCloseAsync(CancellationToken token);
		public Task CloseAsync();
		public long Id { get; }
	}
}
