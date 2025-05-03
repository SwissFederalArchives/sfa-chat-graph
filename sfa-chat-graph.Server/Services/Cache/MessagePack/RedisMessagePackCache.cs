
using Microsoft.Extensions.Options;
using SfaChatGraph.Server.Utils.ServiceCollection;
using Sprache;
using StackExchange.Redis;
using System.Buffers;

namespace SfaChatGraph.Server.Services.Cache.MessagePack
{
	[ServiceImplementation(typeof(IAppendableCache<,>), typeof(AppendableCacheOptions), ServiceLifetime.Singleton, Key = "Redis")]
	public class RedisMessagePackCache<TKey, TValue> : MessagePackCacheBase<TKey, TValue>
	{
		private readonly IDatabaseAsync _redis;
		private readonly IOptions<AppendableCacheOptions> _options;

		public RedisMessagePackCache(IDatabaseAsync redis, IOptions<AppendableCacheOptions> options)
		{
			_redis=redis;
			_options=options;
		}

		public override Task<bool> ExistsAsync(TKey key) => _redis.KeyExistsAsync(key.ToString());
		protected override async Task AppendAsync(TKey key, MemoryStream value)
		{
			await _redis.StringAppendAsync(key.ToString(), RedisValue.CreateFrom(value));
		}

		protected async override Task<ReadOnlySequence<byte>> GetDataAsync(TKey key)
		{
			var data = await _redis.StringGetAsync(key.ToString());
			if (data.IsNullOrEmpty)
				return ReadOnlySequence<byte>.Empty;

			
			return new ReadOnlySequence<byte>( data);
		}

		protected override async Task SetAsync(TKey key, MemoryStream value)
		{
			await _redis.StringSetAsync(key.ToString(), RedisValue.CreateFrom(value), _options.Value.DefaultExpiration);
		}
	}
}
