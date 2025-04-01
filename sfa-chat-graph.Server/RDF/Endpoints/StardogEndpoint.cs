using VDS.RDF;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.RDF.Endpoints
{
	public class StardogEndpoint : ISparqlEndpoint
	{
		private readonly SparqlQueryClient _client;
		private readonly HttpClient _httpClient;

		public StardogEndpoint(string endpoint) : this(new Uri(endpoint))
		{
		}

		public StardogEndpoint(Uri endpoint)
		{
			_httpClient = new HttpClient();
			_client = new SparqlQueryClient(_httpClient, endpoint);
		}

		public Task<IGraph> QueryGraphAsync(string query)
		{
			return _client.QueryWithResultGraphAsync(query);
		}

		public Task<SparqlResultSet> QueryAsync(string query)
		{
			return _client.QueryWithResultSetAsync(query);
		}
	}
}
