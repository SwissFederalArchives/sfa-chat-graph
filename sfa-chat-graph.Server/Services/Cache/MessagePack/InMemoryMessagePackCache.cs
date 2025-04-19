using Microsoft.Extensions.Options;
using Microsoft.IO;
using sfa_chat_graph.Server.Utils.ServiceCollection;
using SfaChatGraph.Server.Utils;
using System.Buffers;
using System.Collections.Concurrent;

namespace sfa_chat_graph.Server.Services.Cache.MessagePack
{
	[Implementation(typeof(IAppendableCache<,>), typeof(AppendableCacheOptions), ServiceLifetime.Singleton, Key = "InMemory")]
	public class InMemoryMessagePackCache<TKey, TValue> : MessagePackCacheBase<TKey, TValue>, IHostedService
	{
		private class CacheItem
		{
			public RecyclableMemoryStream Stream { get; init; }
			public DateTime Expiry { get; private set; }

			public CacheItem(RecyclableMemoryStream stream)
			{
				Stream = stream;
			}

			public void UpdateExpiry(TimeSpan duration)
			{
				Expiry = DateTime.UtcNow + duration;
			}
		}

		private readonly ConcurrentDictionary<TKey, CacheItem> _cache = new ConcurrentDictionary<TKey, CacheItem>();
		private Task _cleanupLoop;
		private readonly IOptions<AppendableCacheOptions> _options;

		public InMemoryMessagePackCache(IOptions<AppendableCacheOptions> options)
		{
			_options=options;
		}

		public override Task<bool> ExistsAsync(TKey key)
		{
			return Task.FromResult(_cache.ContainsKey(key));
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
						await kvp.Value.Stream.DisposeAsync();
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
			_cache.Values.ForEach(x => x.Stream.Dispose());
		}

		protected override async Task AppendAsync(TKey key, MemoryStream value)
		{
			if (_cache.TryGetValue(key, out var item) == false)
				await SetAsync(key, value);

			await value.CopyToAsync(item.Stream);
			item.UpdateExpiry(_options.Value.DefaultExpiration);
		}

		protected override Task<ReadOnlySequence<byte>> GetDataAsync(TKey key)
		{
			if (_cache.TryGetValue(key, out var item) == false)
				return Task.FromResult(ReadOnlySequence<byte>.Empty);

			var sequence = item.Stream.GetReadOnlySequence();
			return Task.FromResult(sequence);
		}

		protected override async Task SetAsync(TKey key, MemoryStream value)
		{
			if (_cache.TryGetValue(key, out var item) == false)
			{
				item = new CacheItem(_streamManager.GetStream());
				_cache.TryAdd(key, item);
			}

			item.Stream.SetLength(0);
			await value.CopyToAsync(item.Stream);
			item.UpdateExpiry(_options.Value.DefaultExpiration);
		}
	}
}
