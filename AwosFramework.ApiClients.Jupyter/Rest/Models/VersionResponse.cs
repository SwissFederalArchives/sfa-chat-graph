using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models
{
	public class VersionResponse
	{
		[JsonPropertyName("version")]
		public string Version { get; set; }
	}
}
