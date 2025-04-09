using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub
{
	[MessageType("display_data", ChannelKind.IOPub)]
	[MessageType("update_display_data", ChannelKind.IOPub)]
	public class DisplayDataMessage
	{
		[JsonPropertyName("data")]
		public required Dictionary<string, object> Data { get; set; }

		[JsonPropertyName("metadata")]
		public Dictionary<string, JsonDocument>? MetaData { get; set; }

		[JsonPropertyName("transient")]
		public Dictionary<string, object>? TransientData { get; set; }	
	}
}
