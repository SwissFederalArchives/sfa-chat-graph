using Json.Schema.Generation.Intents;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using OpenAI.Chat;
using SfaChatGraph.Server.RDF.Models;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Data;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;
using AwosFramework.Generators.FunctionCalling;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Patterns;
using Lucene.Net.Util;
using sfa_chat_graph.Server.RDF.Models;

namespace SfaChatGraph.Server.RDF
{
	public class OntotextStorage : IAsyncQueryableStorage, IGraphRag
	{
		private HttpClient _client;
		private readonly SparqlQueryParser _parser = new SparqlQueryParser();

		public string Repository { get; init; }
		public string Endpoint { get; init; }
		public string Schema { get; private set; }
		public string Graph { get; private set; }

		public IAsyncStorageServer AsyncParentServer { get; init; }

		private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { Converters = { new SparqlStarConverter() } };

		static OntotextStorage()
		{
		}

		public OntotextStorage(IAsyncStorageServer parent, string endpoint, string repository)
		{
			Endpoint = endpoint;
			Repository = repository;
			AsyncParentServer = parent;
			_client = new HttpClient()
			{
				BaseAddress = new Uri(new Uri(Endpoint), "repositories/")
			};

		}


		[FunctionCall("schema")]
		[Description("Gets a description of the ontology of the curren rdf database")]
		public string FunctionCall_GetSchema()
		{
			return Schema;
		}

		[FunctionCall("query_async")]
		[Description("Function to query the database using valid sparql code. Use the schema supplied to you to check if the IRI's you use actually exist. You can use Prefixes to tidy your code, just make sure to define them as well. Prefixed IRI cannot contain further slashes, prefix:part is legal prefix:part/part is not. Make sure to not use slashes when using prefixes.\ntry to include the IRI's in the query response as well even if not directly needed. This is important to know which part of the graph was used for the answer.")]
		public async Task<GraphRagQueryResult> FunctionCall_QueryAsync([Description("The sparql code")] string query)
		{
			var sparql = _parser.ParseFromString(query);
			var resultVars = sparql.Variables
				.Where(x => x.IsResultVariable)
				.Select(x => x.Name)
				.ToFrozenSet();

			var predicates = sparql.RootGraphPattern.TriplePatterns.OfType<TriplePattern>()
				.Where(x => x.PatternType == TriplePatternType.Match &&
					((x.Subject is VariablePattern subPattern &&
					resultVars.Contains(subPattern.VariableName)) ||
					(x.Object is VariablePattern objPattern) &&
					resultVars.Contains(objPattern.VariableName)) &&
					x.Predicate is NodeMatchPattern predPattern &&
					predPattern.IsFixed &&
					predPattern.Node is UriNode &&
					x.Object.IsFixed == false
				).Select(x => ((UriNode)((NodeMatchPattern)x.Predicate).Node).Uri.ToString()).Distinct().ToArray();

			var res = await QueryRepositoryAsSparqlStarAsync(query);
			var iris = res.Results.SelectMany(x => x.GetNamedTerms()).Where(x => x.term != null && x.term.Type == "uri" && resultVars.Contains(x.key));
			var iriList = string.Join(", ", iris.Select(x => $"<{x.term.Value}>"));

			var visualisationQuery = $$"""
			select ?s ?p ?o where {
				?s ?p ?o .
				filter(?s in ({{iriList}}) || ?o in ({{iriList}}))
				filter(?p in ({{string.Join(", ", predicates.Select(x => $"<{x}>"))}}))
			}
			order by (IF(isLiteral(?o), 0, 1))
			limit 25
			""";

			var visResult = await QueryRepositoryAsSparqlStarAsync(visualisationQuery);

			return new GraphRagQueryResult {
				RagResult = res,
				VisualisationResult = visResult
			};
		}

		public async Task ChangeGraphAsync(string grah)
		{
			this.Graph = grah;
			await InitAsync(true);
		}

		private async Task<T> QueryRepositoryAsJsonAsync<T>(string query, JsonSerializerOptions options = null)
		{
			var stream = await QueryRepositoryAsJsonStreamAsync(query);
			return await JsonSerializer.DeserializeAsync<T>(stream, options);
		}

		private async Task<SparqlStarResult> QueryRepositoryAsSparqlStarAsync(string query)
		{
			using var stream = await QueryRepositoryAsJsonStreamAsync(query);
			return await JsonSerializer.DeserializeAsync<SparqlStarResult>(stream, _jsonOptions);
		}

		private async Task<Stream> QueryRepositoryAsJsonStreamAsync(string query)
		{
			var data = new Dictionary<string, string>
			{
				{ "query", query }
			};

			var formContent = new FormUrlEncodedContent(data);
			var request = new HttpRequestMessage(HttpMethod.Post, $"/repositories/{Repository}");
			request.Content = formContent;
			request.Headers.Add("Accept", "application/x-graphdb-table-results+json,application/json");

			var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			var stream = await response.Content.ReadAsStreamAsync();
			return stream;
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

		public async Task<object> QueryAsync(string sparqlQuery, CancellationToken cancellationToken)
		{
			var sparqlStar = await QueryRepositoryAsSparqlStarAsync(sparqlQuery);
			var factory = new NodeFactory();
			var result = new SparqlResultSet(sparqlStar.Results.Select(
				x => new SparqlResult(x.GetNamedTerms().Select(
					nt => new KeyValuePair<string, INode>(nt.key, nt.term?.GetNode(factory) ?? factory.CreateBlankNode()))
				)
			));

			return result;
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

		private IEnumerable<Triple> LoadTriplesFromSparqlStar(INodeFactory factory, SparqlStarResult sparqlStar)
		{
			foreach (var binding in sparqlStar.Results)
				yield return new Triple(binding["s"].GetNode(factory), binding["p"].GetNode(factory), binding["o"].GetNode(factory));
		}

		public async Task LoadGraphAsync(IGraph g, string graphName, CancellationToken cancellationToken)
		{
			if (Uri.IsWellFormedUriString(graphName, UriKind.RelativeOrAbsolute) == false)
				throw new ArgumentException($"{graphName} is not a valid URI", nameof(graphName));

			var query = $$"""
			SELECT ?s ?p ?o FROM {
				GRAPH <{{graphName}}> {
					?s ?p ?o .
				}
			}
			""";

			var sparqlStar = await QueryRepositoryAsSparqlStarAsync(query);
			var triples = LoadTriplesFromSparqlStar(g, sparqlStar);
			g.Assert(triples);
		}

		public async Task LoadGraphAsync(IRdfHandler handler, string graphName, CancellationToken cancellationToken)
		{
			if (Uri.IsWellFormedUriString(graphName, UriKind.RelativeOrAbsolute) == false)
				throw new ArgumentException($"{graphName} is not a valid URI", nameof(graphName));

			var query = $$"""
			SELECT ?s ?p ?o FROM {
				GRAPH <{{graphName}}> {
					?s ?p ?o .
				}
			}
			""";

			var sparqlStar = await QueryRepositoryAsSparqlStarAsync(query);
			var triples = LoadTriplesFromSparqlStar(handler, sparqlStar);
			handler.Apply(triples);
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
			ListGraphsAsync(CancellationToken.None).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, t.Exception), state);
				}
				else
				{
					callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, t.Result), state);
				}
			});
		}

		public async Task<IEnumerable<string>> ListGraphsAsync(CancellationToken cancellationToken)
		{
			var query = """
			select distinct ?g where {
				graph ?g {
					?s ?p ?o .
				}
			}
			""";

			var sparqlStar = await QueryRepositoryAsSparqlStarAsync(query);
			return sparqlStar.Results.Select((x) => x[0].Value);
		}

		public void Dispose()
		{
			_client.Dispose();
		}


		public async Task InitAsync(bool ignoreExisting = false)
		{
			if (string.IsNullOrEmpty(Schema) == false && ignoreExisting == false)
				return;

			var query = $$"""
					select distinct ?st ?p ?ot where { 
				    graph <{{this.Graph}}> {
				       ?s a ?st .
				       optional { 
				            ?s ?p ?o .
				       		optional { 
								?o a ?ot . 
							}  
				        }
				    }
				}
			""";

			var rdfType = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

			var data = await this.QueryAsync(query, CancellationToken.None) as SparqlResultSet;
			var builder = new StringBuilder();
			foreach (var group in data.Results.GroupBy(x => (x["st"] as IUriNode).Uri.ToString()))
			{
				builder.AppendLine($"<{group.Key}>: [");
				foreach (var row in group)
				{
					var predicate = (row["p"] as IUriNode).Uri;
					if (predicate.Equals(rdfType))
						continue;

					var ot = row["ot"];
					builder.Append($"\t<{predicate}> -> ");
					switch (ot)
					{
						case IUriNode uriNode:
							builder.Append($"<{uriNode.Uri}>");
							break;

						default:
							builder.Append("literal");
							break;
					}

					builder.AppendLine();
				}
				builder.AppendLine("]");
				builder.AppendLine();
			}

			Schema = builder.ToString();
		}

		Task<SparqlStarResult> IGraphRag.QueryAsync(string query)
		{
			return QueryRepositoryAsSparqlStarAsync(query);
		}
	}
}
