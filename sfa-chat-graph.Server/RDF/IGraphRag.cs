using sfa_chat_graph.Server.RDF.Models;

namespace sfa_chat_graph.Server.RDF
{
	public interface IGraphRag
	{
		public string Schema { get; }
		public Task InitAsync(bool ignoreExisiting = false);
		public Task<SparqlStarResult> QueryAsync(string query);
	}
}
