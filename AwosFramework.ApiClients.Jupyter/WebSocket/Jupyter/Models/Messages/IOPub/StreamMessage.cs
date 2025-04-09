using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub
{
	[MessageType("stream", ChannelKind.IOPub)]
	public class StreamMessage
	{
		[JsonPropertyName("name")]
		public StreamType Stream { get; set; }

		[JsonPropertyName("text")]
		public required string Text { get; set; } = string.Empty;
	}
}
