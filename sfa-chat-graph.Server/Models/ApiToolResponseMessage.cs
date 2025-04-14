using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF.Models;
using System.Text.Json.Serialization;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Models
{
	public class ApiToolResponseMessage : ApiMessage
	{
		public string ToolCallId { get; set; }

		public string Query { get; set; }
		public SparqlResultSet Graph { get; set; }
		public SparqlResultSet GraphData { get; set; }

		public ApiToolResponseMessage() : base(ChatRole.ToolResponse, null)
		{

		}

		public ApiToolResponseMessage(string id, string content, string query = null, SparqlResultSet graph = null, SparqlResultSet graphData = null) : base(ChatRole.ToolResponse, content)
		{
			this.ToolCallId = id;
			this.Graph = graph;
			this.Query = query;
			this.GraphData = graphData;
		}

	}
}
