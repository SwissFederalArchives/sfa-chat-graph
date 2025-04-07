using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	[MessageType("inspect_request", ChannelKind.Shell)]
	public class InspectRequest
	{
		[JsonPropertyName("code")]
		public required string Code { get; set; }

		[JsonPropertyName("cursor_pos")]
		public int CursorPos { get; set; }

		[JsonPropertyName("detail_level")]
		public int DetailLevel { get; set; }
	}
}
