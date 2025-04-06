using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Terminal
{
	public class TerminalModel
	{
		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("last_activity")]
		public DateTime LastActivity { get; set; }
	}
}
