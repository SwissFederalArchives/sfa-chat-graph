using System.Text.Json.Serialization;
using VDS.RDF;

namespace sfa_chat_graph.Server.RDF.Models
{
	public class SparqlStarTerm
	{
		[JsonPropertyName("type")]
		public string Type { get; set; }

		[JsonPropertyName("datatype")]
		public string DataType { get; set; }

		[JsonPropertyName("value")]
		public string Value { get; set; }

		public INode GetNode(INodeFactory g)
		{
			switch (Type)
			{
				case "uri":
					return g.CreateUriNode(new Uri(Value));
				case "literal":
					return string.IsNullOrEmpty(DataType) ? g.CreateLiteralNode(Value) : g.CreateLiteralNode(Value, DataType);
				default:
					throw new NotSupportedException($"Unknown node type {Type}");
			}
		}
	}
}
