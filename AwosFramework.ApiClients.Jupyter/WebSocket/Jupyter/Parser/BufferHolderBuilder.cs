using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{
	public class BufferHolderBuilder
	{
		private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new RecyclableMemoryStreamManager();
		private static readonly ArrayPool<int> _arrayPool = ArrayPool<int>.Shared;

		private readonly RecyclableMemoryStream _stream;
		private int[] _length;
		private int _count;

		private BufferHolderBuilder(int initialCount)
		{
			_count = 0;
			_length = _arrayPool.Rent(initialCount);
			_stream = _memoryStreamManager.GetStream();
		}

		private void EnsureLengthCapacity()
		{
			if (_count >= _length.Length)
			{
				var newLength = Math.Max(_length.Length * 2, _count + 1);
				var newArray = _arrayPool.Rent(newLength);
				Array.Copy(_length, newArray, _count);
				_arrayPool.Return(_length);
				_length = newArray;
			}
		}

		public void Add(Action<Stream> streamAction)
		{
			var oldPos = _stream.Position;
			streamAction?.Invoke(_stream);
			var length = (int)(_stream.Position - oldPos);
			EnsureLengthCapacity();
			_length[_count++] = length;
		}

		public void Add(ReadOnlySpan<byte> buffer)
		{
			var oldPos = _stream.Position;
			_stream.Write(buffer);
			var length = (int)(_stream.Position - oldPos);
			EnsureLengthCapacity();
			_length[_count++] = length;
		}

		public void Add(ReadOnlyMemory<byte> buffer) => Add(x => x.Write(buffer.Span));
		public void Add(byte[] buffer) => Add(x => x.Write(buffer));
		public void Add(byte[] buffer, int offset, int count) => Add(x => x.Write(buffer, offset, count));
		public void Add(string str, Encoding? encoding = null) => Add(x => x.Write((encoding ?? Encoding.UTF8).GetBytes(str)));


		public IBufferHolder Build() => new StreamBufferHolder(_count, _stream, _length);

		public static BufferHolderBuilder Create(int initialCount = 8) => new BufferHolderBuilder(initialCount);

		private class StreamBufferHolder : IBufferHolder
		{
			public int Count { get; init; }
			private readonly RecyclableMemoryStream _bufferStream;
			private readonly int[] _lengths;

			public IEnumerable<int> BufferLengths => _lengths.Take(Count);

			private ReadOnlyMemory<byte> GetBuffer(int index)
			{
				var offset = _lengths.Take(index).Sum();
				var length = _lengths[index];
				var buffer = _bufferStream.GetBuffer();
				return buffer.AsMemory(offset, length);
			}

			public ReadOnlyMemory<byte> this[int index] => GetBuffer(index);

			public StreamBufferHolder(int count, RecyclableMemoryStream stream, int[] lengths)
			{
				Count = count;
				_bufferStream = stream;
				_lengths = lengths;
			}

			public void Dispose()
			{
				_arrayPool.Return(_lengths);
				_bufferStream.Dispose();
			}

			#region Buffer Enumerator
			private struct StreamBufferHolderEnumerator : IEnumerator<ReadOnlyMemory<byte>>
			{
				private int _index = -1;
				private int _offset = 0;
				private readonly StreamBufferHolder _holder;
				private readonly byte[] _buffer;
				private ReadOnlyMemory<byte> _current;



				private ReadOnlyMemory<byte> CurrentOrThrow()
				{
					if (_index < 0 || _index >= _holder.Count)
						throw new InvalidOperationException("Enumerator out of range");

					return _current;
				}

				public ReadOnlyMemory<byte> Current => CurrentOrThrow();
				object IEnumerator.Current => Current;

				public StreamBufferHolderEnumerator(StreamBufferHolder holder)
				{
					_holder = holder;
					_buffer = _holder._bufferStream.GetBuffer();

				}


				public bool MoveNext()
				{
					if (++_index >= _holder.Count)
						return false;

					var len = _holder._lengths[_index];
					_current = _buffer.AsMemory(_offset, len);
					_offset += len;
					return true;
				}

				public void Reset()
				{
					_index = -1;
					_offset = 0;
				}

				public void Dispose()
				{
				}
			}
			public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator() => new StreamBufferHolderEnumerator(this);
			IEnumerator IEnumerable.GetEnumerator() => new StreamBufferHolderEnumerator(this);
			#endregion
		}
	}
}
