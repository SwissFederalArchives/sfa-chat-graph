using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub
{
	[MessageType("execute_input", ChannelKind.IOPub)]
	public class ExecuteInputMessage
	{
		[JsonPropertyName("code")]
		public required string Code { get; set; }

		[JsonPropertyName("execution_count")]
		public int ExecutionCount { get; set; }
	}
}
