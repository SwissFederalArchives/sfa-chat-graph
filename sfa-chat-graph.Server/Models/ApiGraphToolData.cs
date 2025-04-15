using VDS.RDF;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Models
{
	public class ApiGraphToolData
	{
		public string Query { get; set; }
		public SparqlResultSet VisualisationGraph { get; set; }
		public SparqlResultSet DataGraph { get; set; }
	}
}
