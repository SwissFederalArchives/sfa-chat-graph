using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class KernelSpecDetail
	{
		[JsonPropertyName("argv")]
		public required string[] Arguments { get; set; }

		[JsonPropertyName("env")]
		public Dictionary<string, string>? Environment { get; set; }

		[JsonPropertyName("display_name")]
		public required string DisplayName { get; set; }

		[JsonPropertyName("language")]
		public required string Language { get; set; }

		[JsonPropertyName("interrupt_mode")]
		public KernelInterruptMode InterruptMode { get; set; }

		[JsonPropertyName("metadata")]
		public JsonDocument? MetaData { get; set; }
	}
}
