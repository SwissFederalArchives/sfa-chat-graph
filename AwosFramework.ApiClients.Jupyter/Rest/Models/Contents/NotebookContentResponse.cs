using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	internal class NotebookContentResponse
	{
		[JsonPropertyName("content")]
		public required JsonDocument Content { get; set; }
	}
}
