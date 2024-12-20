using sfa_chat_graph.Server.RDF.Models;

namespace sfa_chat_graph.Server.Models
{
	public class ApiQueryResponse
	{
		public bool IsSuccess { get; set; } 
		public bool IncompatibleSchema { get; set; }
		public string Answer { get; set; }
		public string Error { get; set; }
		public SparqlStarResult Graph { get; set; }
	}
}