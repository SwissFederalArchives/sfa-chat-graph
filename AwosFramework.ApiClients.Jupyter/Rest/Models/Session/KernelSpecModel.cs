using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class KernelSpecModel
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("spec")]
		public required KernelSpecDetail Spec { get; set; }

		[JsonPropertyName("resources")]
		public Dictionary<string, string>? Resources { get; set; }
	}
}
