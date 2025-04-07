using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	[MessageType("inspect_reply", ChannelKind.Shell)]
	public class InspectReply : ReplyMessage
	{
		[JsonPropertyName("found")]
		public bool Found { get; set; }

		[JsonPropertyName("data")]
		public JsonDocument? Data { get; set; }

		[JsonPropertyName("metadata")]
		public JsonDocument? MetaData { get; set; }
	}
}
