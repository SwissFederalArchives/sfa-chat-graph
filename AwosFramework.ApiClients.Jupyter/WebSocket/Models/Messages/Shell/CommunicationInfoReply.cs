using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	[MessageType("comm_info_reply", ChannelKind.Shell)]
	public class CommunicationInfoReply : ReplyMessage
	{
		[JsonPropertyName("comms")]
		public required Dictionary<Guid, CommunicationInfoRequest> CommunicationInfo { get; set; }
	}
}
