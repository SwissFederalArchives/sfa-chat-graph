using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Stdin
{
	[MessageType("input_request", ChannelKind.Stdin)]
	public class InputRequest
	{
		[JsonPropertyName("prompt")]
		public required string Prompt { get; set; }

		[JsonPropertyName("password")]
		public bool Password { get; set; }
	}
}
