using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class StartKernelRequest
	{
		[JsonPropertyName("name")]
		public required string SpecName { get; set; }

		[JsonPropertyName("path")]
		public string? Path { get; set; }	
	}
}
