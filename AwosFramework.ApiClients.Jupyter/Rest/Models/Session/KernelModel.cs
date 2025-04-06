using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class KernelModel : KernelIdentification
	{
		[JsonPropertyName("last_activity")]
		public DateTime LastActivity { get; set; }

		[JsonPropertyName("execution_state")]
		public ExecutionState ExecutionState { get; set; }

		[JsonPropertyName("connections")]
		public int Connections { get; set; }
	}
}
