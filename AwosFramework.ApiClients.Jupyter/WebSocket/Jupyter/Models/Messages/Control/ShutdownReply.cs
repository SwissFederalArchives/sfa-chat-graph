using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Control
{
	[MessageType("shutdown_reply", ChannelKind.Control)]
	public class ShutdownReply : ReplyMessage
	{
		[JsonPropertyName("restart")]
		public bool Restart { get; set; } 
	}
}
