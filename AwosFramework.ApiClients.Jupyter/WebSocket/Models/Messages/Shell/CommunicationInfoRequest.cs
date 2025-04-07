using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	[MessageType("comm_info_request", ChannelKind.Shell)]
	public class CommunicationInfoRequest
	{
		[JsonPropertyName("target_name")]
		public string? TargetName { get; set; }
	}
}
