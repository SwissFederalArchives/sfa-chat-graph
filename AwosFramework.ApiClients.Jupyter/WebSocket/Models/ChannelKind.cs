using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models
{
	public enum ChannelKind
	{
		Shell,
		IOPub,
		Stdin,
		Control
	}
}
