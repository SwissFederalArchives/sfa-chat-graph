using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	[MessageType("is_complete_request", ChannelKind.Shell)]
	public class IsCompleteRequest 
	{
		[JsonPropertyName("code")]
		public required string Code { get; set; }
	}
}
