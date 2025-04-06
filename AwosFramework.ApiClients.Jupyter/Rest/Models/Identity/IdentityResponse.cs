using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Identity
{
	public class IdentityResponse
	{
		[JsonPropertyName("identity")]
		public required IdentityModel Identity { get; set; }

		[JsonPropertyName("permissions")]
		public required Dictionary<string, string[]> Permissions { get; set; }
	}
}
