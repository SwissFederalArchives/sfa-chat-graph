using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Control
{
	[MessageType("shutdown_request", ChannelKind.Control)]
	public class ShutdownRequest
	{
		[JsonPropertyName("restart")]
		public bool Restart { get; set; }
	}
}
