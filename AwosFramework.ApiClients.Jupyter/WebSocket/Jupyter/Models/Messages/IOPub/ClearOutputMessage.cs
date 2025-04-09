using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub
{
	[MessageType("clear_output", ChannelKind.IOPub)]
	public class ClearOutputMessage
	{
		[JsonPropertyName("wait")]
		public bool Wait { get; set; }
	}
}
