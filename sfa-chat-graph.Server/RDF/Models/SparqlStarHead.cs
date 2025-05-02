using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.RDF.Models
{
	public class SparqlStarHead
	{
		[JsonPropertyName("vars")]
		public string[] Vars { get; set; }
	}
}
