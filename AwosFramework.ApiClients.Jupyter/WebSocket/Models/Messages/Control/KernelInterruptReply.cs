using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Control
{
	[MessageType("interrupt_reply", ChannelKind.Control)]
	public class KernelInterruptReply : ReplyMessage
	{
	}
}
