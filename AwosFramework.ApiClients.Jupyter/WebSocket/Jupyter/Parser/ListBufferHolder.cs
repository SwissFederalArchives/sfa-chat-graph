using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{
	public class ListBufferHolder : IBufferHolder
	{
		private readonly List<Memory<byte>> _buffers = new();

		public ReadOnlyMemory<byte> this[int index] => _buffers[index];

		public int Count => _buffers.Count;

		public IEnumerable<int> BufferLengths => _buffers.Select(x => x.Length);

		public void Dispose()
		{
		}

		public void Add(Memory<byte> buffer)
		{
			_buffers.Add(buffer);
		}

		public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator() => _buffers.Select(x => (ReadOnlyMemory<byte>)x).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _buffers.GetEnumerator();
	}
}
