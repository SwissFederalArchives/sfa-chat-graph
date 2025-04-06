using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class KernelSpecsResponse
	{
		[JsonPropertyName("default")]
		public required string Default { get; set; }

		[JsonPropertyName("kernelspecs")]	
		public required Dictionary<string, KernelSpecModel> KernelSpecs { get; set; }
	}
}
