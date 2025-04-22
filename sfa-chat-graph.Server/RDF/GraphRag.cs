using sfa_chat_graph.Server.Utils;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.Utils;
using System.Collections.Frozen;
using System.ComponentModel;
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

		public async Task<string> GetSchemaAsync(string graph, bool ignoreCached = false)
		{
			try
			{

				if (ignoreCached == false && _schemaCache.TryGetValue(graph, out var schema))
					return schema;

				int offset = 0;
				int limit = 100;
				var result = new SparqlResultSet();
				SparqlResultSet page;

				do
				{
					page = await _endpoint.QueryAsync(Queries.GraphSchemaQuery(graph, offset, limit));
					offset += limit;
					result.Results.AddRange(page.Results);
				} while (page.Count >= limit);

				var res = SparqlResultFormatter.ToLLMSchema(result);
				_schemaCache[graph] = res;
				return res;
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
