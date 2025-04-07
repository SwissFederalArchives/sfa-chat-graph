using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models
{
	[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
	public class MessageHeader
	{
		[JsonPropertyName("msg_id")]
		public required string Id { get; set; }

		[JsonPropertyName("session")]
		public required string SessionId { get; set; }

		[JsonPropertyName("username")]
		public required string UserName { get; set; }

		[JsonPropertyName("date")]
		public DateTime Timestamp { get; set; }

		[JsonPropertyName("msg_type")]
		public required string MessageType { get; set; }

		[JsonPropertyName("version")]
		public required string Version { get; set; }

		[JsonPropertyName("subshell_id")]
		public string? SubshellId { get; set; }
	}
}
