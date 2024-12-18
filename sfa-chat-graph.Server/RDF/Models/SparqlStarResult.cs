using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.RDF.Models
{
	public class SparqlStarResult
	{
		[JsonPropertyName("head")]
		public SparqlStarHead Head { get; set; }

		[JsonIgnore]
		public SparqlStarObject[] Results { get; set; }
	}
}
