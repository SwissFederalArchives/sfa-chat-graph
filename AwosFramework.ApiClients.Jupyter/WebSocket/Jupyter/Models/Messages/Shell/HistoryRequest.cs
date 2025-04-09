using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	[MessageType("history_request", ChannelKind.Shell)]
	public class HistoryRequest
	{
		[JsonPropertyName("output")]
		public bool Output { get; set; }
		
		[JsonPropertyName("raw")]
		public bool Raw { get; set; }

		[JsonPropertyName("hist_access_type")]
		public HistoryAccessType AccessType { get; set; }
		
		[JsonPropertyName("session")]
		public int Session { get; set; }

		[JsonPropertyName("start")]
		public int Start { get; set; }

		[JsonPropertyName("stop")]
		public int Stop { get; set; }

		[JsonPropertyName("n")]
		public int NCells { get; set; }

		[JsonPropertyName("pattern")]
		public string? Pattern { get; set; }

		[JsonPropertyName("unique")]
		public bool Unique { get; set; }
	}
}
