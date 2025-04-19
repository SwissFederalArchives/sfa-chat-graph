namespace sfa_chat_graph.Server.Services.Cache
{
	public interface IAppendableCache<TKey, TValue>
	{
		public Task AppendAsync(TKey key, IEnumerable<TValue> value);
		public Task SetAsync(TKey key, IEnumerable<TValue> value);
		public IAsyncEnumerable<TValue> GetAsync(TKey key);
		public Task<bool> ExistsAsync(TKey key);
	}
}
