using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	[MessageType("execute_request", ChannelKind.Shell)]
	public class ExecuteRequest
	{
		[JsonPropertyName("code")]
		public required string Code { get; set; }

		[JsonPropertyName("silent")]
		public bool Silent { get; set; }

		[JsonPropertyName("store_history")]
		public bool StoreHistory { get; set; } = true;

		[JsonPropertyName("user_expressions")]
		public Dictionary<string, string>? UserExpressions { get; set; }

		[JsonPropertyName("allow_stdin")]
		public bool AllowStdin { get; set; }

		[JsonPropertyName("stop_on_error")]
		public bool StopOnError { get; set; }
	}
}
