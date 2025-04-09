using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub
{
	[MessageType("execute_result", ChannelKind.IOPub)]
	public class ExecutionResultMessage : DisplayDataMessage
	{
		[JsonPropertyName("execution_count")]
		public required int ExecutionCount { get; set; }
	}
}
