using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.IOPub
{
	[MessageType("status", ChannelKind.IOPub)]
	public class KernelStatusMessage
	{
		[JsonPropertyName("execution_state")]
		public ExecutionState ExecutionState { get; set; }
	}
}
