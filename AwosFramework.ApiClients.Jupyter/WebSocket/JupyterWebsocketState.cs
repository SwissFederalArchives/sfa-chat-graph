using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket
{
	public enum JupyterWebsocketState
	{
		Disconnected,
		Connected,
		Connecting,
		Reconnecting,
		Disconnecting,
		Disposed,
		Errored
	}
}
