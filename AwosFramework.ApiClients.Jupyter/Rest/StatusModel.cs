using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest
{
	public class StatusModel
	{
		[JsonPropertyName("connections")]
		public int Connections { get; set; }

		[JsonPropertyName("kernels")]
		public int Kernels { get; set; }

		[JsonPropertyName("last_activity")]
		public DateTime LastActivity { get; set; }

		[JsonPropertyName("started")]
		public DateTime Started { get; set; }
	}
}
