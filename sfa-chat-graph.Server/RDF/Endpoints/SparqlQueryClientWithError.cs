using System.Net.Http.Headers;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.RDF.Endpoints
{
	public class SparqlQueryClientWithError<TErr> : SparqlQueryClient
	{
		private readonly RelativeCachingUriFactory _uriFactory;
		public SparqlQueryClientWithError(HttpClient httpClient, Uri endpointUri) : base(httpClient, endpointUri)
		{
			_uriFactory = new RelativeCachingUriFactory(new CachingUriFactory(UriFactory.Root), endpointUri);
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

		public new async Task QueryWithResultSetAsync(string sparqlQuery, ISparqlResultsHandler resultsHandler, CancellationToken cancellationToken)
		{
			using HttpResponseMessage response = await QueryInternal(sparqlQuery, ResultsAcceptHeader, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadFromJsonAsync<TErr>();
				throw new RdfQueryException($"Server reports {(int)response.StatusCode}: {response.ReasonPhrase}.") { Data = { ["error"] = error } };
			}

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
			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadFromJsonAsync<TErr>();
				throw new RdfQueryException($"Server reports {(int)response.StatusCode}: {response.ReasonPhrase}.") { Data = { ["error"] = error } };
			}

			MediaTypeHeaderValue ctype = response.Content.Headers.ContentType;
			IRdfReader rdfParser = MimeTypesHelper.GetParser(ctype.MediaType);
			Stream stream = await response.Content.ReadAsStreamAsync();
			using StreamReader input = (string.IsNullOrEmpty(ctype.CharSet) ? new StreamReader(stream) : new StreamReader(stream, Encoding.GetEncoding(ctype.CharSet)));
			rdfParser.Load(handler, input, _uriFactory);
		}

	}
}
