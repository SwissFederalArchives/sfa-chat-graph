using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	[MessageType("is_complete_reply", ChannelKind.Shell)]
	public class IsCompleteReply : ReplyMessage
	{
		[JsonPropertyName("indent")]
		public string? Indent { get; set; }
	}
}
