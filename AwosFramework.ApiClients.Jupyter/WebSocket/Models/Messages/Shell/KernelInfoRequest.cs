using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	[MessageType("kernel_info_request", ChannelKind.Shell)]
	public class KernelInfoRequest
	{
	}
}
