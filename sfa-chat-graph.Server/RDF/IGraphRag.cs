using OpenAI.Chat;
using SfaChatGraph.Server.RDF.Models;

namespace SfaChatGraph.Server.RDF
{
	public interface IGraphRag
	{
		public string Schema { get; }
		public Task InitAsync(bool ignoreExisiting = false);
		public Task<SparqlStarResult> QueryAsync(string query);
	}
}
