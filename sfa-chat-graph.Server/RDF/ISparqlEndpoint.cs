using VDS.RDF;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.RDF
{
	public interface ISparqlEndpoint
	{
		public string Name { get; }
		public Task<SparqlResultSet> QueryAsync(string query);
		public Task<IGraph> QueryGraphAsync(string query);
	}
}
