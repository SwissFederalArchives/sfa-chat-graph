using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Parser
{
	public class ListBufferHolder : IBufferHolder
	{
		private readonly List<Memory<byte>> _buffers = new();

		public ReadOnlyMemory<byte> this[int index] => _buffers[index];

		public int Length => _buffers.Count;

		public void Dispose()
		{
		}

		public void Add(Memory<byte> buffer)
		{
			_buffers.Add(buffer);
		}
	}
}
