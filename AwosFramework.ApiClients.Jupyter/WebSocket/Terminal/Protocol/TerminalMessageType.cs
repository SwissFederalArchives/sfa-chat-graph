using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol
{
	public enum TerminalMessageType
	{
		Setup,
		Stdout,
		Stdin,
		SetSize,
		Unknown
	}
}
