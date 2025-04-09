using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class KernelIdentification
	{
		[JsonPropertyName("id")]
		public Guid? Id { get; set; }

		[JsonPropertyName("name")]
		public string? SpecName { get; set; }
	
		
	}
}
