using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{
	public interface ITransferableBufferHolder : IEnumerable<ReadOnlyMemory<byte>>, IDisposable
	{
		public int Count { get; }

		public IEnumerable<int> BufferLengths { get; }
	}
}
