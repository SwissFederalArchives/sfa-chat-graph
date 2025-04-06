using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	public class Checkpoint
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("last_modified")]
		public DateTime LastModified { get; set; }
	}
}
