using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Base
{
	public interface IError
	{
		public Exception Exception { get; }
		public object? ErrorCode { get; }
	}
}
