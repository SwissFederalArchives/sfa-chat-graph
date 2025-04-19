
using MessagePack;
using Microsoft.IO;
using sfa_chat_graph.Server.Utils.MessagePack;
using System.Buffers;

namespace sfa_chat_graph.Server.Services.Cache.MessagePack
{
	public abstract class MessagePackCacheBase<TKey, TValue> : IAppendableCache<TKey, TValue>
	{
		protected static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();
		private MessagePackSerializerOptions _msgPackOptions = new MessagePackSerializerOptions(FormatterResolver.Instance)
			.WithCompression(MessagePackCompression.Lz4BlockArray);


		protected abstract Task SetAsync(TKey key, MemoryStream value);
		protected abstract Task AppendAsync(TKey key, MemoryStream value);
		protected abstract Task<ReadOnlySequence<byte>> GetDataAsync(TKey key);
		public abstract Task<bool> ExistsAsync(TKey key);

		protected async virtual Task<MemoryStream> SerializeAsync(IEnumerable<TValue> items)
		{
			var stream = _streamManager.GetStream();
			foreach (var item in items)
				await MessagePackSerializer.SerializeAsync(stream, item, _msgPackOptions);

			stream.Position = 0;
			return stream;
		}

		public async Task AppendAsync(TKey key, IEnumerable<TValue> items)
		{
			using var stream = await SerializeAsync(items);
			await AppendAsync(key, stream);
		}


		public async IAsyncEnumerable<TValue> GetAsync(TKey key)
		{
			var memory = await GetDataAsync(key);
			var reader = new MessagePackReader(memory);
			var list = new List<TValue>(16);
			while (reader.End == false)
				list.Add(MessagePackSerializer.Deserialize<TValue>(ref reader, _msgPackOptions));

			foreach(var item in list)
				yield return item;
		}

		public async Task SetAsync(TKey key, IEnumerable<TValue> items)
		{
			using var stream = await SerializeAsync(items);
			await SetAsync(key, stream);
		}
	}
}
