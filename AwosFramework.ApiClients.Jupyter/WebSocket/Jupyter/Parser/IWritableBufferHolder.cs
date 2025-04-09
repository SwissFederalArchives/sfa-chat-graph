using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser
{
	public interface IWritableBufferHolder : IBufferHolder
	{
		public unsafe Memory<byte> WriteAccess(int bufferIndex, int bufferSize);
	}
}
