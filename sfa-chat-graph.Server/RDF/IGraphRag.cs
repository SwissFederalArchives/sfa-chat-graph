using OpenAI.Chat;
using SfaChatGraph.Server.FunctionCalling;
using SfaChatGraph.Server.RDF.Models;

namespace SfaChatGraph.Server.RDF
{
	public interface IGraphRag
	{
		public string Schema { get; }
		public Task InitAsync(bool ignoreExisiting = false);
		public Task<SparqlStarResult> QueryAsync(string query);
		public IEnumerable<ChatTool> CallableFunctions { get; }
		public Task<object> CallFunctionAsync(IServiceProvider provider, string function, string json);
	}
}
