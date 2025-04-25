using sfa_chat_graph.Server.Utils;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.Utils;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Text;
using VDS.Common.Collections.Enumerations;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Patterns;

namespace sfa_chat_graph.Server.RDF
{
	public class GraphRag : IGraphRag
	{
		private readonly ISparqlEndpoint _endpoint;
		private readonly Dictionary<string, string> _schemaCache = new();
		private readonly SparqlQueryParser _parser = new();
		private string[] _graphsCache = null;
		private readonly ILogger _logger;

		public GraphRag(ISparqlEndpoint endpoint, ILoggerFactory loggerFactory)
		{
			_endpoint = endpoint;
			_logger = loggerFactory.CreateLogger<GraphRag>();
		}


		private async IAsyncEnumerable<string> GetClassNamesAsync(string graph)
		{
			int offset = 0;
			int limit = 100;
			SparqlResultSet page;

			do
			{
				page = await _endpoint.QueryAsync(Queries.GraphSchemaClassesQuery(graph, offset, limit));
				foreach (var result in page.Results)
				{
					if (result["st"] is UriNode uriNode)
						yield return uriNode.Uri.ToString();
				}
				offset += limit;
			} while (page.Count >= limit);
		}

		private async Task<string> GetClassSchemaAsync(string graph, string className)
		{
			var builder = new StringBuilder();
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


			var dict = schemaValues.Results.GroupBy(x => ((IUriNode)x["p"]).Uri.AbsoluteUri, x => SparqlResultFormatter.FormatSchemaNode(x["ot"]))
				.ToDictionary(x => x.Key, x => x.Distinct().ToArray());

			foreach (var kvp in dict)
			{
				var predicate = kvp.Key;
				string target = kvp.Value switch
				{
					{ Length: 0 } => null,
					{ Length: 1 } => kvp.Value[0],
					_ => $"[\n{string.Join(",\n", kvp.Value.Select(y => $"\t\t{y}"))}\n\t]"
				};

				builder.AppendLine($"\t<{predicate}> -> {target}");
			}

			return builder.ToString();
		}

		public async Task<string> GetSchemaAsync(string graph, bool ignoreCached = false)
		{
			try
			{
				var sb = new StringBuilder();
				var classNames = await GetClassNamesAsync(graph).ToArrayAsync();
				foreach (var className in classNames)
				{
					var schema = await GetClassSchemaAsync(graph, className);
					sb.AppendLine($"<{className}> [\n{schema}\n]");
					sb.AppendLine();
				}

				return sb.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting schema for graph {Graph}", graph);
				throw;
			}
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
			if (_graphsCache == null || ignoreCached)
			{
				var res = await _endpoint.QueryAsync(Queries.ListGraphsQuery());
				_graphsCache = res.Select(x => x["g"]).OfType<UriNode>().Select(x => x.Uri.ToString()).ToArray();
			}

			return _graphsCache;
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
