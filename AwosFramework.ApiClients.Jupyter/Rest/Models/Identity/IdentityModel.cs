using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Identity
{
	public class IdentityModel
	{
		[JsonPropertyName("username")]
		public required string UserName { get; set; }

		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("display_name")]
		public required string DisplayName { get; set; }

		[JsonPropertyName("initials")]
		public required string Initials { get; set; }

		[JsonPropertyName("avatar_url")]
		public string? AvatarUrl { get; set; }

		[JsonPropertyName("color")]
		public string? Color { get; set; }
	}
}
