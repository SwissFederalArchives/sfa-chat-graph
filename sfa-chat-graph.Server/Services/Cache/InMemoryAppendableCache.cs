
using sfa_chat_graph.Server.Utils.ServiceCollection;

namespace sfa_chat_graph.Server.Services.Cache
{
	[Implementation(typeof(IAppendableCache<,>), typeof(AppendableCacheOptions), ServiceLifetime.Singleton, Key = "InMemory")]
	public class InMemoryAppendableCache<TKey, TValue> : IAppendableCache<TKey, TValue>, IHostedService
	{
		public Task AppendAsync(TKey key, IEnumerable<TValue> value)
		{
			throw new NotImplementedException();
		}

		public Task<bool> ExistsAsync(TKey key)
		{
			throw new NotImplementedException();
		}

		public IAsyncEnumerable<TValue> GetAsync(TKey key)
		{
			throw new NotImplementedException();
		}

		public Task SetAsync(TKey key, IEnumerable<TValue> value)
		{
			throw new NotImplementedException();
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
