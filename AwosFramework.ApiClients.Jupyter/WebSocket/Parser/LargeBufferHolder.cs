using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Parser
{
	public class LargeBufferHolder : IWritableBufferHolder
	{
		private readonly byte[][] _buffers;
		public int Length => _buffers.Length;

		public LargeBufferHolder(int bufferCount)
		{
			_buffers = new byte[bufferCount][];
		}


		public void Dispose() { }
		public ReadOnlyMemory<byte> this[int index] => _buffers[index];
		public Memory<byte> WriteAccess(int bufferIndex, int bufferSize)
		{
			_buffers[bufferIndex] = new byte[bufferSize];
			return _buffers[bufferIndex];
		}
	}
}
