using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class SessionModel
	{
		[JsonPropertyName("id")]
		public Guid Id { get; set; }

		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("path")]
		public required string Path { get; set; }

		[JsonPropertyName("type")]
		public SessionType Type { get; set; }

		[JsonPropertyName("kernel")]
		public required KernelModel Kernel { get; set; }
	}

}
