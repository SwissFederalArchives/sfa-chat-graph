using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub
{
	[MessageType("error", ChannelKind.IOPub)]
	public class ErrorMessage
	{
		[JsonPropertyName("ename")]
		public string? ExceptionName { get; set; }

		[JsonPropertyName("evalue")]
		public string? ExceptionValue { get; set; }

		[JsonPropertyName("traceback")]
		public string[]? StackTrace { get; set; }
	}
}
