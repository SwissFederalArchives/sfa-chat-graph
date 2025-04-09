using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Json;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Control
{
	[MessageType("debug_request", ChannelKind.Control)]
	[MessageType("debug_reply", ChannelKind.Control)]
	[MessageType("debug_event", ChannelKind.IOPub)]
	[JsonConverter(typeof(DebugMessageConverter))]
	public class DebugMessage 
	{
		public JsonDocument? Content { get; set; }
	}
}
