using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using sfa_chat_graph.Server.RDF.Models;
using System.Data;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;

namespace sfa_chat_graph.Server.RDF
{
	public class OntotextStorage : IAsyncQueryableStorage
	{
		private HttpClient _client;
		public string Repository { get; set; }
		public string Endpoint { get; init; }
		public IAsyncStorageServer AsyncParentServer { get; init; }

		public OntotextStorage(IAsyncStorageServer parent, string endpoint, string repository)
		{
			Endpoint = endpoint;
			Repository = repository;
			AsyncParentServer = parent;
			_client = new HttpClient()
			{
				BaseAddress = new Uri(Endpoint)
			};
		}

		private async Task<JsonDocument> QueryStorageAsync(string query)
		{
			var data = new Dictionary<string, string>
			{
				{ "query", query }
			};

			var formContent = new FormUrlEncodedContent(data);
			var request = new HttpRequestMessage(HttpMethod.Post, Repository)
			{
				Content = formContent,
				Headers =
				{
					{"Accept", "application/json"}
				}
			};

			var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadFromJsonAsync<JsonDocument>();
		}




		public bool IsReady => throw new NotImplementedException();

		public bool IsReadOnly => throw new NotImplementedException();

		public IOBehaviour IOBehaviour => IOBehaviour.IsQuadStore | IOBehaviour.HasNamedGraphs | IOBehaviour.IsReadOnly;

		public bool UpdateSupported => false;
		public bool DeleteSupported => false;
		public bool ListGraphsSupported => true;


		public void Query(string sparqlQuery, AsyncStorageCallback callback, object state)
		{
			QueryAsync(sparqlQuery, CancellationToken.None).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQuery, t.Exception), state);
				}
				else
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQuery, sparqlQuery, t.Result), state);
				}
			});
		}

		public void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery, AsyncStorageCallback callback, object state)
		{
			QueryAsync(rdfHandler, resultsHandler, sparqlQuery, CancellationToken.None).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQuery, t.Exception), state);
				}
				else
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQuery, sparqlQuery, rdfHandler, resultsHandler), state);
				}

			});
		}

		public Task<object> QueryAsync(string sparqlQuery, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task QueryAsync(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void LoadGraph(IGraph g, Uri graphUri, AsyncStorageCallback callback, object state) => LoadGraph(g, graphUri.ToString(), callback, state);
		public void LoadGraph(IGraph g, string graphUri, AsyncStorageCallback callback, object state)
		{
			LoadGraphAsync(g, graphUri, CancellationToken.None).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadGraph, t.Exception), state);
				}
				else
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadGraph, g), state);
				}
			});
		}

		public void LoadGraph(IRdfHandler handler, Uri graphUri, AsyncStorageCallback callback, object state) => LoadGraph(handler, graphUri.ToString(), callback, state);

		public void LoadGraph(IRdfHandler handler, string graphUri, AsyncStorageCallback callback, object state)
		{
			LoadGraphAsync(handler, graphUri, CancellationToken.None).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadGraph, t.Exception), state);
				}
				else
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadGraph, handler), state);
				}
			});
		}


		private SparqlStarResult ReadSparqlStarResult(INodeFactory factory, JsonDocument sparqlStar)
		{
			var res = sparqlStar.RootElement.Deserialize<SparqlStarResult>();
			var converter = new SparqlStarConverter(res.Head.Vars);
			var options = new JsonSerializerOptions
			{
				Converters = { converter }
			};

			res.Results = sparqlStar.RootElement.GetProperty("results").GetProperty("bindings").Deserialize<SparqlStarObject[]>(options);
			return res;
		}

		private IEnumerable<Triple> LoadTriplesFromJson(INodeFactory factory, JsonDocument sparqlStar)
		{
			var res = ReadSparqlStarResult(factory, sparqlStar);
			foreach (var binding in res.Results)
			{
				yield return new Triple(binding["s"].GetNode(factory), binding["p"].GetNode(factory), binding["o"].GetNode(factory));
			}
		}

		public async Task LoadGraphAsync(IGraph g, string graphName, CancellationToken cancellationToken)
		{
			if(Uri.IsWellFormedUriString(graphName, UriKind.RelativeOrAbsolute) == false)
				throw new ArgumentException($"{graphName} is not a valid URI", nameof(graphName));

			var query = $$"""
			SELECT ?s ?p ?o FROM {
				GRAPH <{{graphName}}> {
					?s ?p ?o .
				}
			}
			""";
			
			var json = await QueryStorageAsync(query);

		}

		public Task LoadGraphAsync(IRdfHandler handler, string graphName, CancellationToken cancellationToken)
		{
			handler.
		}

		public void SaveGraph(IGraph g, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public Task SaveGraphAsync(IGraph g, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void UpdateGraph(Uri graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public void UpdateGraph(string graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public Task UpdateGraphAsync(string graphName, IEnumerable<Triple> additions, IEnumerable<Triple> removals, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void DeleteGraph(Uri graphUri, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public void DeleteGraph(string graphUri, AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public Task DeleteGraphAsync(string graphName, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void ListGraphs(AsyncStorageCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<string>> ListGraphsAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
