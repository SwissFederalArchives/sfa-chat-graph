using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Stdin
{
	[MessageType("input_reply", ChannelKind.Stdin)]
	public class InputReply
	{
		[JsonPropertyName("value")]
		public required string Value { get; set; }
	}
}
