using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	[MessageType("complete_reply", ChannelKind.Shell)]
	public class CompleteReply : ReplyMessage
	{
		[JsonPropertyName("matches")]
		public required string[] Matches { get; set; }
		
		[JsonPropertyName("cursor_start")]
		public int CursorStart { get; set; }

		[JsonPropertyName("cursor_end")]
		public int CursorEnd { get; set; }
		
		[JsonPropertyName("metadata")]
		public JsonDocument? MetaData { get; set; }

	}
}
