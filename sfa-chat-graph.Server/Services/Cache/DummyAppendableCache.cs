using Json.Schema;
using SfaChatGraph.Server.Utils.ServiceCollection;

namespace SfaChatGraph.Server.Services.Cache
{
	[ServiceImplementation(typeof(IAppendableCache<,>), typeof(AppendableCacheOptions), ServiceLifetime.Singleton, Key = "None")]
	public class DummyAppendableCache<TKey, TValue> : IAppendableCache<TKey, TValue>
	{
		public Task AppendAsync(TKey key, IEnumerable<TValue> value)
		{
			return Task.CompletedTask;
		}

		public Task<bool> ExistsAsync(TKey key)
		{
			return Task.FromResult(false);
		}

		public IAsyncEnumerable<TValue> GetAsync(TKey key)
		{
			return null;
		}

		public Task SetAsync(TKey key, IEnumerable<TValue> value)
		{
			return Task.CompletedTask;
		}
	}
}
