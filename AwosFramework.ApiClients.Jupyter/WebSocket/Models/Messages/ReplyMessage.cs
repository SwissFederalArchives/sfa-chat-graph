using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages
{
	public abstract class ReplyMessage
	{
		[JsonPropertyName("status")]
		public StatusType Status { get; set; }

		[JsonPropertyName("ename")]
		public string? ExceptionName { get; set; }

		[JsonPropertyName("evalue")]
		public string? ExceptionValue { get; set; }

		[JsonPropertyName("traceback")]
		public string[]? StackTrace { get; set; }
	}
}
