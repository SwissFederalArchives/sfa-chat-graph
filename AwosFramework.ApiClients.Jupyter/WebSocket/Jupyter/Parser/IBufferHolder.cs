using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{
	public interface IBufferHolder : ITransferableBufferHolder
	{
		public ReadOnlyMemory<byte> this[int index] { get; }
	}
}
