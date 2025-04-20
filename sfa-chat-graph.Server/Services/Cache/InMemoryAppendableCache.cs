using Microsoft.Extensions.Options;
using Microsoft.IO;
using sfa_chat_graph.Server.Utils.ServiceCollection;
using SfaChatGraph.Server.Utils;
using System.Buffers;
using System.Collections.Concurrent;

namespace sfa_chat_graph.Server.Services.Cache
{
	[Implementation(typeof(IAppendableCache<,>), typeof(AppendableCacheOptions), ServiceLifetime.Singleton, Key = "InMemory")]
	public class InMemoryAppendableCache<TKey, TValue> : IAppendableCache<TKey, TValue>, IHostedService
	{
		private class CacheItem<T>
		{
			public DateTime Expiry { get; private set; }
			public List<T> Items { get; init; }

			public CacheItem()
			{
				Items = new List<T>(16);
			}

			public void UpdateExpiry(TimeSpan duration)
			{
				Expiry = DateTime.UtcNow + duration;
			}
		}

		private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache = new ConcurrentDictionary<TKey, CacheItem<TValue>>();
		private Task _cleanupLoop;
		private readonly IOptions<AppendableCacheOptions> _options;

		public InMemoryAppendableCache(IOptions<AppendableCacheOptions> options)
		{
			_options=options;
		}


		private async Task CleanupLoop(CancellationToken cancellationToken)
		{
			while (cancellationToken.IsCancellationRequested == false)
			{
				var now = DateTime.UtcNow;
				var min = now + _options.Value.DefaultExpiration;
				foreach (var kvp in _cache)
				{
					if (kvp.Value.Expiry < now)
					{
						_cache.TryRemove(kvp.Key, out _);
					}
					else
					{
						min = min < kvp.Value.Expiry ? min : kvp.Value.Expiry;
					}
				}

				await Task.Delay(min - now, cancellationToken);
			}
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_cleanupLoop = CleanupLoop(cancellationToken);
			return Task.CompletedTask;
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAny(_cleanupLoop, Task.Delay(Timeout.Infinite, cancellationToken));
		}

		public Task AppendAsync(TKey key, IEnumerable<TValue> value)
		{
			var item = _cache.GetOrAdd(key, _ => new CacheItem<TValue>());
			item.Items.AddRange(value);
			item.UpdateExpiry(_options.Value.DefaultExpiration);
			return Task.CompletedTask;
		}

		public Task SetAsync(TKey key, IEnumerable<TValue> value)
		{
			var item = _cache.GetOrAdd(key, _ => new CacheItem<TValue>());
			item.Items.Clear();
			item.Items.AddRange(value);
			item.UpdateExpiry(_options.Value.DefaultExpiration);
			return Task.CompletedTask;
		}

		public async IAsyncEnumerable<TValue> GetAsync(TKey key)
		{
			if (_cache.TryGetValue(key, out var item))
			{
				foreach (var v in item.Items)
					yield return v;
			}
			else
			{
				yield break;
			}
		}

		public Task<bool> ExistsAsync(TKey key) => Task.FromResult(_cache.ContainsKey(key));
	}
}
