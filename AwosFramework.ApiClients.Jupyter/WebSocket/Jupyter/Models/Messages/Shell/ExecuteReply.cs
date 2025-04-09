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
	[MessageType("execute_reply", ChannelKind.Shell)]
	public class ExecuteReply : ReplyMessage
	{
		[JsonPropertyName("execution_count")]
		public int ExecutionCount { get; set; }

		[JsonPropertyName("payload")]
		public Dictionary<string, JsonDocument>[]? Payload { get; set; }

		[JsonPropertyName("user_expressions")]
		public Dictionary<string, UserExpressionResult>? UserExpressions { get; set; }


	}
}
