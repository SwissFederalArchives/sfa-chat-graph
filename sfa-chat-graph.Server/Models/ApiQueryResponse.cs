using SfaChatGraph.Server.RDF.Models;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Models
{
	public class ApiQueryResponse
	{
		public bool IsSuccess { get; set; } 
		public bool IncompatibleSchema { get; set; }
		public string Answer { get; set; }
		public string Error { get; set; }
		public SparqlResultSet Graph { get; set; }
	}
}