using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell
{
	public class UserExpressionResult : ReplyMessage
	{
		[JsonPropertyName("data")]
		public Dictionary<string, object>? Data { get; set; }

		[JsonPropertyName("metadata")]
		public JsonDocument? MetaData { get; set; }
	}
}
