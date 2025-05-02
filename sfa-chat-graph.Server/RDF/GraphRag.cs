using J2N.Collections.Generic.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using sfa_chat_graph.Server.Services.ChatService;
using sfa_chat_graph.Server.Utils;
using sfa_chat_graph.Server.Versioning;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Text;
using VDS.Common.Collections.Enumerations;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Patterns;

namespace sfa_chat_graph.Server.RDF
{
	public class GraphRag : IGraphRag
	{
		private readonly ISparqlEndpoint _endpoint;
		private readonly IMongoDatabase _db;
		private readonly IMongoCollection<EndpointCache> _endpoints;
		private readonly IMongoCollection<SchemaCache> _schemas;

		private readonly SparqlQueryParser _parser = new();
		private readonly ILogger _logger;

		public GraphRag(ISparqlEndpoint endpoint, ILoggerFactory loggerFactory, IMongoDatabase db)
		{
			_endpoint = endpoint;
			_logger = loggerFactory.CreateLogger<GraphRag>();
			_db=db;
			_endpoints = db.GetCollectionVersion<EndpointCache>("endpoint-cache", 1);
			_schemas = db.GetCollectionVersion<SchemaCache>("schema-cache", 1);
		}


		private async IAsyncEnumerable<(string name, int count)> GetClassNamesAsync(string graph)
		{
			int offset = 0;
			int limit = 100;
			SparqlResultSet page;

			do
			{
				page = await _endpoint.QueryAsync(Queries.GraphSchemaClassesQuery(graph, offset, limit));
				foreach (var result in page.Results)
				{
					if (result["st"] is IUriNode uriNode && result["count"] is ILiteralNode countNode && int.TryParse(countNode.Value, out var count))
						yield return (uriNode.Uri.ToString(), count);
				}
				offset += limit;
			} while (page.Count >= limit);
		}

		private string FormatClassProperties<T>(Dictionary<string, T> dict) where T : ICollection<string>
		{
			var builder = new StringBuilder();
			foreach (var kvp in dict)
			{
				var predicate = kvp.Key;
				string target = kvp.Value switch
				{
					{ Count: 0 } => null,
					{ Count: 1 } => kvp.Value.First(),
					_ => $"[\n{string.Join(",\n", kvp.Value.Select(y => $"\t\t{y}"))}\n\t]"
				};
				builder.AppendLine($"\t<{predicate}> -> {target}");
			}
			return builder.ToString();
		}

		private async Task<string> GetClassSchemaAsync(string graph, string className)
		{
			int offset = 0;
			int limit = 100;

			SparqlResultSet schemaValues = new();
			SparqlResultSet page;
			do
			{
				page = await _endpoint.QueryAsync(Queries.GraphSchemaPropertiesQuery(graph, className, offset, limit));
				offset += limit;
				schemaValues.Results.AddRange(page.Results);
			} while (page.Count >= limit);


			var dict = schemaValues.Results.GroupBy(x => ((IUriNode)x["p"]).Uri.AbsoluteUri, x => SparqlResultFormatter.FormatSchemaNode(x["type"]))
				.Where(x => x.Key != "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
				.ToDictionary(x => x.Key, x => x.ToFrozenSet());

			return FormatClassProperties(dict);
		}

		public async Task<string> GuessClassSchemaAsync(IChatActivity activities, string graph, string className, int countObjects, int countGuesses, bool randomOffsets = true, int stoppAfter = 50000)
		{
			var result = new Dictionary<string, HashSet<string>>();
			var limit = 10000;
			var pageCount = countObjects / limit;
			var pages = Enumerable.Range(0, pageCount);
			if (randomOffsets)
				pages = pages.OrderBy(x => Random.Shared.Next());

			var pageQueue = new Queue<int>(pages);
			int lastNewProperty = 0;
			while (countGuesses > 0 && pageQueue.Count > 0)
			{
				if (lastNewProperty >= stoppAfter)
					break;

				var page = pageQueue.Dequeue();
				var offset = page * limit;
				var toQuery = Math.Min(countGuesses, limit);
				var classIntances = await QueryAsync(Queries.GraphSchemaClassInstancesQuery(graph, className, offset, toQuery));
				var iris = classIntances.Results.Select(x => x["s"]).OfType<IUriNode>().Select(x => x.Uri.AbsoluteUri);
				var describe = await QueryAsync(Queries.GraphSchemaBatchDescribeQuery(iris));
				lastNewProperty += toQuery;

				foreach (var description in describe.Results)
				{
					if (description["p"] is not IUriNode predUriNode || predUriNode.Uri.AbsoluteUri == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type")
						continue;

					if (result.TryGetValue(predUriNode.Uri.AbsoluteUri, out var objectTypeSet) == false)
					{
						objectTypeSet = new HashSet<string>();
						result.Add(predUriNode.Uri.AbsoluteUri, objectTypeSet);
					}

					var objectType = SparqlResultFormatter.FormatSchemaNode(description["type"]);
					if (objectTypeSet.Add(objectType))
						lastNewProperty = 0;

				}

				countGuesses -= limit;
				activities?.NotifyActivityAsync($"Guessed properties for {className}", $"Found {result.Count} properties total, {countGuesses} are left, early stopp after no new properties {lastNewProperty}/{stoppAfter}");
			}

			return FormatClassProperties(result);
		}

		public async Task<string> GetSchemaAsync(IChatActivity activities, string graph, bool ignoreCached = false)
		{
			var cache = ignoreCached ? null : await _schemas.Find(x => x.Graph == graph && x.Endpoint == _endpoint.Name).FirstOrDefaultAsync();
			if (cache == null)
			{
				await activities?.NotifyActivityAsync("Fetching schema classes");
				var sb = new StringBuilder();
				try
				{
					var classNames = await GetClassNamesAsync(graph).ToArrayAsync();
					foreach (var (className, count) in classNames)
					{
						string schema = null;
						if (count> 50000)
						{
							await activities?.NotifyActivityAsync($"Guessing schema for {className}", $"Schema for {className} has {count} object instances, querying all instance to generate a schema is too slow and strainous for the database, schema is guessed by looking a 10% of all instance at random");
							schema = await GuessClassSchemaAsync(activities, graph, className, count, (int)(count*0.1), true, 50000);
						}
						else
						{
							await activities?.NotifyActivityAsync($"Fetching schema for {className}");
							schema = await GetClassSchemaAsync(graph, className);
						}

						sb.AppendLine($"<{className}> [\n{schema}\n]");
						sb.AppendLine();
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error getting schema for graph {Graph}", graph);
					throw;
				}

				cache = new SchemaCache
				{
					Graph = graph,
					Schema = sb.ToString(),
					Endpoint = _endpoint.Name,
				};

				var update = Builders<SchemaCache>.Update
					.Set(x => x.Schema, cache.Schema)
					.SetOnInsert(x => x.Graph, graph)
					.SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
					.SetOnInsert(x => x.Endpoint, _endpoint.Name);

				cache = await _schemas.FindOneAndUpdateAsync(x => x.Graph == graph && x.Endpoint == _endpoint.Name, 
					update, new FindOneAndUpdateOptions<SchemaCache> { ReturnDocument = ReturnDocument.After, IsUpsert = true });
			}

			return cache.Schema;
		}

		private static bool IsResultVariable(PatternItem node, FrozenSet<string> resultVars)
		{
			return node switch
			{
				VariablePattern variable => resultVars.Contains(variable.VariableName),
				_ => false
			};
		}

		private static bool IsFixedPattern(PatternItem node)
		{
			return node switch
			{
				NodeMatchPattern nodeMatch => nodeMatch.IsFixed && nodeMatch.Node.NodeType == NodeType.Uri,
				_ => false
			};
		}

		private static bool IsVisualizablePattern(TriplePattern pattern, FrozenSet<string> resultVars)
		{
			return
				pattern.PatternType == TriplePatternType.Match &&
				(IsResultVariable(pattern.Subject, resultVars) || IsResultVariable(pattern.Object, resultVars)) &&
				IsFixedPattern(pattern.Predicate) &&
				pattern.Object.IsFixed == false;
		}

		private static string? GetPredicateIri(TriplePattern pattern)
		{
			if (pattern.Predicate is NodeMatchPattern nodeMatch && nodeMatch.Node is UriNode uriNode)
				return uriNode.Uri.ToString();

			return null;
		}

		private static IEnumerable<string> GetResultSetIris(SparqlResultSet set, FrozenSet<string> resultVars)
		{
			return set.Results.SelectMany(result => resultVars.Select(varName => result[varName]))
				.OfType<UriNode>()
				.Select(x => x.ToString())
				.Distinct();
		}

		private static SparqlResult ToResult(Triple triple)
		{
			var result = new SparqlResult();
			result.SetValue("s", triple.Subject);
			result.SetValue("p", triple.Predicate);
			result.SetValue("o", triple.Object);
			return result;
		}

		private async Task<IEnumerable<ISparqlResult>> DescribeAsSparqlResultAsync(string iri)
		{
			var graph = await _endpoint.QueryGraphAsync(Queries.DescribeQuery(iri));
			return graph.Triples.Select(ToResult);
		}

		private async Task<SparqlResultSet> DescribeIrisAsync(string[] iris)
		{
			var tasks = iris.Take(25).Select(DescribeAsSparqlResultAsync).ToArray();
			await Task.WhenAll(tasks);
			var results = tasks.SelectMany(x => x.Result).ToArray();
			return new SparqlResultSet(results);
		}

		public async Task<SparqlResultSet> GetVisualisationResultAsync(SparqlResultSet results, string queryString)
		{
			var query = _parser.ParseFromString(queryString);
			var resultVars = query.Variables.SelectWhere(x => x.Name, x => x.IsResultVariable).ToFrozenSet();
			var triplePatterns = query.RootGraphPattern.ChildGraphPatterns.SelectMany(x => x.TriplePatterns).ToList();
			triplePatterns.AddRange(query.RootGraphPattern.TriplePatterns);
			var predicates = triplePatterns.OfType<TriplePattern>()
				.Where(x => IsVisualizablePattern(x, resultVars))
				.SelectNonNull(GetPredicateIri)
				.ToHashSet();

			var iris = GetResultSetIris(results, resultVars).ToArray();
			SparqlResultSet relatedTriples = null;
			try
			{
				relatedTriples = await _endpoint.QueryAsync(Queries.RelatedTriplesQuery(iris, predicates));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting related triples for query {Query}", queryString);
			}

			if (relatedTriples == null || (relatedTriples.IsEmpty && iris.Length > 0))
				relatedTriples = await DescribeIrisAsync(iris.ToArray());

			return relatedTriples;
		}

		public async Task<string[]> ListGraphsAsync(bool ignoreCached = false)
		{
			var cache = ignoreCached ? null : await _endpoints.Find(x => x.Endpoint == _endpoint.Name).FirstOrDefaultAsync();
			if (cache == null)
			{
				var res = await _endpoint.QueryAsync(Queries.ListGraphsQuery());
				var graphs = res.Select(x => x["g"]).OfType<UriNode>().Select(x => x.Uri.ToString()).ToArray();

				var update = Builders<EndpointCache>.Update
					.Set(x => x.Graphs, graphs)
					.SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
					.SetOnInsert(x => x.Endpoint, _endpoint.Name);

				cache = await _endpoints.FindOneAndUpdateAsync(x => x.Endpoint == _endpoint.Name, update, new FindOneAndUpdateOptions<EndpointCache> { IsUpsert = true, ReturnDocument = ReturnDocument.After });
			}

			return cache.Graphs;
		}

		public Task<SparqlResultSet> QueryAsync(string query)
		{
			try
			{
				return _endpoint.QueryAsync(query);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error executing query {Query}", query);
				throw;
			}
		}

		public Task<IGraph> DescribeAsync(string iri)
		{
			try
			{
				return _endpoint.QueryGraphAsync(Queries.DescribeQuery(iri));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error describing {Iri}", iri);
				throw;
			}
		}
	}
}
