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

		public GraphRag(ISparqlEndpoint endpoint)
		{
			_endpoint = endpoint;
		}

		public async Task<string> GetSchemaAsync(string graph, bool ignoreCached = false)
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

			var iris = GetResultSetIris(results, resultVars);
			var relatedTriples = await _endpoint.QueryAsync(Queries.RelatedTriplesQuery(iris, predicates));
			return relatedTriples;
		}

		public async Task<string[]> ListGraphsAsync()
		{
			var res = await _endpoint.QueryAsync(Queries.ListGraphsQuery());
			return res.Select(x => x["g"]).OfType<UriNode>().Select(x => x.Uri.ToString()).ToArray();
		}

		public Task<SparqlResultSet> QueryAsync(string query)
		{
			return _endpoint.QueryAsync(query);
		}

		public Task<IGraph> DescribeAsync( string iri)
		{
			return _endpoint.QueryGraphAsync(Queries.DescribeQuery(iri));
		}
	}
}
