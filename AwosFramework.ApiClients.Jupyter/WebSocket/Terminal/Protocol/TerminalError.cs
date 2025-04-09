using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol
{
	public class TerminalError : IError
	{
		public Exception? Exception { get; init; }

		public object? ErrorCode => null;

		public static TerminalError FromException(Exception ex) => new TerminalError
		{
			Exception = ex
		};
	}
}
