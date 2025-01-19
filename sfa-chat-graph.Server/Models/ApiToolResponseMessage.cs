using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF.Models;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.Models
{
	public class ApiToolResponseMessage : ApiMessage
	{
		public string ToolCallId { get; set; }
		public SparqlStarResult Graph { get; set; }

		[JsonConstructor]
		public ApiToolResponseMessage() : base(ChatRole.ToolResponse, null)
		{

		}

		public ApiToolResponseMessage(string id, string content, SparqlStarResult graph = null) : base(ChatRole.ToolResponse, content)
		{
			this.ToolCallId = id;
			this.Graph = graph;
		}

	}
}
