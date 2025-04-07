using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	[MessageType("history_reply", ChannelKind.Shell)]
	public class HistoryReply : ReplyMessage
	{
		[JsonPropertyName("history")]
		public HistoryEntry[]? History { get; set; }
	}
}
