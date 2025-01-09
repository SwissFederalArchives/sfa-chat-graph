using System.Text.Json.Serialization;

namespace SfaChatGraph.Server.RDF.Models
{
	public class SparqlStarHead
	{
		[JsonPropertyName("vars")]
		public string[] Vars { get; set; }
	}
}
