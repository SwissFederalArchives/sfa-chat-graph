using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace SfaChatGraph.Server.RDF.Models
{
	public class SparqlStarResult
	{
		[JsonIgnore]
		public FrozenDictionary<string, int> Mapping { get; init; }

		[JsonPropertyName("head")]
		public SparqlStarHead Head { get; set; }

		[JsonIgnore]
		public SparqlStarObject[] Results { get; set; }

		public SparqlStarResult(SparqlStarHead head, SparqlStarObject[] results, FrozenDictionary<string, int> mapping)
		{
			Head=head;
			Results=results;
			Mapping=mapping;
		}
	}
}
