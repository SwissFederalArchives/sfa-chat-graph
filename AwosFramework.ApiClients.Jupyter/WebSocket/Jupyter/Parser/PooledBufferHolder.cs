using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{
	public class PooledBufferHolder : IDisposable, IWritableBufferHolder
	{
		private readonly int _headerLength;
		private readonly int _bufferCount;
		private readonly byte[] _buffers;
		private readonly ArrayPool<byte> _memoryPool;

		public static readonly PooledBufferHolder Empty = new PooledBufferHolder(0, 0, ArrayPool<byte>.Shared);

		public unsafe PooledBufferHolder(int bufferCount, int bufferSize, ArrayPool<byte> memoryPool)
		{
			_memoryPool = memoryPool;
			_bufferCount = bufferCount;
			_headerLength = bufferCount * sizeof(int) * 2;
			if (_headerLength > 0)
				_buffers = _memoryPool.Rent(_headerLength + bufferSize);
			else
				_buffers = Array.Empty<byte>();
		}

		private void IndexRangeCheck(int index)
		{
			if (index >= _bufferCount || index < 0)
				throw new IndexOutOfRangeException("Invalid buffer index");
		}

		public int Count => _bufferCount;


		public unsafe ReadOnlyMemory<byte> this[int index]
		{
			get
			{
				IndexRangeCheck(index);
				var indexOffset = index * _bufferCount * sizeof(int) * 2;
				int offset = Unsafe.As<byte, int>(ref _buffers[indexOffset]);
				int length = Unsafe.As<byte, int>(ref _buffers[indexOffset + sizeof(int)]);
				return _buffers.AsMemory(offset, length);
			}
		}

		private int GetLength(int bufferIndex)
		{
			IndexRangeCheck(bufferIndex);
			var indexOffset = bufferIndex * _bufferCount * sizeof(int) * 2;
			int length = Unsafe.As<byte, int>(ref _buffers[indexOffset + sizeof(int)]);
			return length;
		}

		private int GetOffset(int bufferIndex)
		{
			if (bufferIndex == 0)
				return _headerLength;

			var indexOffset = (bufferIndex-1) * _bufferCount * sizeof(int) * 2;
			int offset = Unsafe.As<byte, int>(ref _buffers[indexOffset]);
			int length = Unsafe.As<byte, int>(ref _buffers[indexOffset + sizeof(int)]);
			return offset + length;
		}

		public unsafe Memory<byte> WriteAccess(int bufferIndex, int bufferSize)
		{
			IndexRangeCheck(bufferIndex);
			var bufferOffset = GetOffset(bufferIndex);
			var indexOffset = bufferIndex * _bufferCount * sizeof(int) * 2;
			Unsafe.As<byte, int>(ref _buffers[indexOffset]) = bufferOffset;
			Unsafe.As<byte, int>(ref _buffers[indexOffset + sizeof(int)]) = bufferSize;
			return _buffers.AsMemory(bufferOffset, bufferSize);
		}

		public void Dispose()
		{
			_memoryPool.Return(_buffers);
		}


		#region Buffer Length Enumerator
		public IEnumerable<int> BufferLengths => new BufferLengthEnumerator(this);

		private struct BufferLengthEnumerator : IEnumerable<int>, IEnumerator<int>
		{
			private int _index = -1;
			private int _current = 0;

			private readonly PooledBufferHolder _holder;

			public BufferLengthEnumerator(PooledBufferHolder holder)
			{
				_holder=holder;
			}

			private int CurrentOrThrow()
			{
				if (_index < 0 || _index >= _holder.Count)
					throw new InvalidOperationException("Enumerator out of range");

				return _current;
			}

			public int Current => CurrentOrThrow();
			object IEnumerator.Current => CurrentOrThrow();

			public void Dispose()
			{
			}

			public IEnumerator<int> GetEnumerator() => this;
			IEnumerator IEnumerable.GetEnumerator() => this;

			public bool MoveNext()
			{
				if(++_index >= _holder.Count)
					return false;

				_current = _holder.GetLength(_index);
				return true;
			}

			public void Reset()
			{
				_index = -1;
			}

		}
		#endregion


		#region Buffer Enumerator
		public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator() => new BufferEnumerator(this);
		IEnumerator IEnumerable.GetEnumerator() => new BufferEnumerator(this);
		private struct BufferEnumerator : IEnumerator<ReadOnlyMemory<byte>>
		{
			private readonly PooledBufferHolder _holder;
			private int _index;
			private ReadOnlyMemory<byte> _current;

			public BufferEnumerator(PooledBufferHolder holder)
			{
				_holder = holder;
				_index = -1;
			}

			private ReadOnlyMemory<byte> CurrentOrThrow()
			{
				if (_index < 0 || _index >= _holder.Count)
					throw new InvalidOperationException("Enumerator out of range");

				return _current;
			}

			public ReadOnlyMemory<byte> Current => CurrentOrThrow();
			object IEnumerator.Current => CurrentOrThrow();

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (++_index >= _holder.Count)
					return false;

				_current = _holder[_index];
				return true;
			}

			public void Reset()
			{
				_index = -1;
			}
		}
		#endregion

	}
}
