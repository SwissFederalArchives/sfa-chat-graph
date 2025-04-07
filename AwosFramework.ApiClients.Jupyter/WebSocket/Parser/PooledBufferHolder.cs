using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Parser
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

		public int Length => _bufferCount;
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
	}
}
