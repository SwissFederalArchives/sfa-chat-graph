using VDS.RDF;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.RDF.Endpoints
{
	public class StardogEndpoint : ISparqlEndpoint
	{
		private readonly SparqlQueryClientWithError<StardogError> _client;
		private readonly HttpClient _httpClient;

		public string Name { get; init; }

		public StardogEndpoint(ILoggerFactory loggerFactory, string endpoint) : this(loggerFactory, new Uri(endpoint))
		{

		}

		public StardogEndpoint(ILoggerFactory loggerFactory, Uri endpoint)
		{
			Name = endpoint.AbsoluteUri;
			_httpClient = new HttpClient();
			_httpClient.Timeout = TimeSpan.FromSeconds(60);
			_client = new SparqlQueryClientWithError<StardogError>(loggerFactory, _httpClient, endpoint);
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
