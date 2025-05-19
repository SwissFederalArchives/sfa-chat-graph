using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.RDF.Endpoints
{
	public class SparqlQueryClientWithError<TErr> : SparqlQueryClient
	{
		private readonly RelativeCachingUriFactory _uriFactory;
		private readonly INamespaceMapper _namespaceMapper;
		private readonly ILogger _logger;

		public SparqlQueryClientWithError(ILoggerFactory loggerFactory, HttpClient httpClient, Uri endpointUri) : base(httpClient, endpointUri)
		{
			_uriFactory = new RelativeCachingUriFactory(new CachingUriFactory(UriFactory.Root), endpointUri);
			_namespaceMapper = new NamespaceMapper(_uriFactory);
			_logger = loggerFactory.CreateLogger<SparqlQueryClientWithError<TErr>>();
		}

		public new async Task<SparqlResultSet> QueryWithResultSetAsync(string sparqlQuery)
		{
			SparqlResultSet results = new SparqlResultSet();
			await QueryWithResultSetAsync(sparqlQuery, new ResultSetHandler(results), CancellationToken.None);
			return results;
		}

		public new async Task<SparqlResultSet> QueryWithResultSetAsync(string sparqlQuery, CancellationToken cancellationToken)
		{
			SparqlResultSet results = new SparqlResultSet();
			await QueryWithResultSetAsync(sparqlQuery, new ResultSetHandler(results), cancellationToken);
			return results;
		}

		private async Task ThrowIfErrorAsync(HttpResponseMessage response)
		{
			if (!response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
				TErr error = default;
				if (content.Length > 0 && response.Content.Headers.ContentType.MediaType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						error = JsonSerializer.Deserialize<TErr>(content);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error deserializing error response from server [ContentType={ContentType}]\n{Content}", response.Content.Headers.ContentType, content);
					}
				}

				_logger.LogError("Error querying SPARQL endpoint: {StatusCode} {ReasonPhrase} {Content}", (int)response.StatusCode, response.ReasonPhrase, content);
				throw new RdfQueryException($"Server reports {(int)response.StatusCode}: {response.ReasonPhrase}.") { Data = { ["error"] = error, ["status"] = (int)response.StatusCode } };
			}
		}

		public new async Task QueryWithResultSetAsync(string sparqlQuery, ISparqlResultsHandler resultsHandler, CancellationToken cancellationToken)
		{
			resultsHandler.BaseUri ??= EndpointUri;
			using HttpResponseMessage response = await QueryInternal(sparqlQuery, ResultsAcceptHeader, cancellationToken);
			await ThrowIfErrorAsync(response);
			MediaTypeHeaderValue ctype = response.Content.Headers.ContentType;
			ISparqlResultsReader resultsParser = MimeTypesHelper.GetSparqlParser(ctype.MediaType);
			Stream stream = await response.Content.ReadAsStreamAsync();
			using StreamReader input = (string.IsNullOrEmpty(ctype.CharSet) ? new StreamReader(stream) : new StreamReader(stream, Encoding.GetEncoding(ctype.CharSet)));
			resultsParser.Load(resultsHandler, input, _uriFactory);
		}

		public new async Task<IGraph> QueryWithResultGraphAsync(string sparqlQuery)
		{
			Graph g = new Graph
			{
				BaseUri = EndpointUri
			};
			await QueryWithResultGraphAsync(sparqlQuery, new GraphHandler(g), CancellationToken.None);
			return g;
		}

		public new async Task<IGraph> QueryWithResultGraphAsync(string sparqlQuery, CancellationToken cancellationToken)
		{
			Graph g = new Graph
			{
				BaseUri = EndpointUri
			};
			await QueryWithResultGraphAsync(sparqlQuery, new GraphHandler(g), cancellationToken);
			return g;
		}

		public new async Task QueryWithResultGraphAsync(string sparqlQuery, IRdfHandler handler)
		{
			await QueryWithResultGraphAsync(sparqlQuery, handler, CancellationToken.None);
		}

		public new async Task QueryWithResultGraphAsync(string sparqlQuery, IRdfHandler handler, CancellationToken cancellationToken)
		{
			using HttpResponseMessage response = await QueryInternal(sparqlQuery, RdfAcceptHeader, cancellationToken);
			await ThrowIfErrorAsync(response);
			MediaTypeHeaderValue ctype = response.Content.Headers.ContentType;
			IRdfReader rdfParser = MimeTypesHelper.GetParser(ctype.MediaType);
			Stream stream = await response.Content.ReadAsStreamAsync();
			using StreamReader input = (string.IsNullOrEmpty(ctype.CharSet) ? new StreamReader(stream) : new StreamReader(stream, Encoding.GetEncoding(ctype.CharSet)));
			rdfParser.Load(handler, input, _uriFactory);
		}

	}
}
