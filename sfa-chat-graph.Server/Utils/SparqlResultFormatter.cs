using System.Text;
using VDS.RDF;
using VDS.RDF.Query;

namespace sfa_chat_graph.Server.Utils
{
	public class SparqlResultFormatter
	{
		private static string FormatSchemaTriple(ISparqlResult result)
		{
			var pred = result["p"];
			var ot = result["ot"];
			return ot switch
			{
				IUriNode uriNode => $"\t{pred} -> <{ot}>",
				_ => $"\t{pred} -> LITERAL"
			};
		}

		private static string CsvFormatNode(INode node)
		{
			var str = node switch
			{
				UriNode uriNode => $"<{uriNode.Uri}>",
				LiteralNode literalNode => literalNode.Value,
				_ => node.ToString()
			};

			if (str.Contains(";"))
			{
				str = $"\"{str.Replace("\"", "\\\"")}\"";
			}

			return str;
		}

		public static string ToCSV(IGraph graph, int? maxLines = null)
		{
			if (graph.Triples.Count == 0)
				return "Query yielded empty collection";

			var builder = new StringBuilder();
			builder.AppendLine("subject;predicate;object");
			foreach (var triple in graph.Triples)
			{
				if (maxLines.HasValue && --maxLines < 0)
				{
					builder.AppendLine("Output too large and cut off...");
					break;
				}

				builder.AppendLine($"{CsvFormatNode(triple.Subject)};{CsvFormatNode(triple.Predicate)};{CsvFormatNode(triple.Object)}");
			}

			return builder.ToString();
		}

		public static string ToCSV(SparqlResultSet resultSet, int? maxLines = null)
		{
			if (resultSet.ResultsType == SparqlResultsType.Boolean)
				return $"boolean: {resultSet.Result}";

			if (resultSet.Results.Count == 0)
				return "Query yielded empty collection";

			var variables = resultSet.Variables.ToArray();
			var builder = new StringBuilder();
			builder.AppendLine(string.Join(";", variables));
			foreach (var result in resultSet)
			{
				if (maxLines.HasValue && --maxLines < 0)
				{
					builder.AppendLine("Output too large and cut off...");
					break;
				}

				var line = string.Join(";", variables.Select(x => CsvFormatNode(result[x])));
				builder.AppendLine(line);
			}

			return builder.ToString();
		}

		public static string ToLLMSchema(SparqlResultSet resultSet)
		{
			var builder = new StringBuilder();
			var grouped = resultSet.GroupBy(x => x["st"]);
			foreach (var group in grouped)
			{
				builder.AppendLine($"<{group.Key}> [");
				foreach (var related in group)
					builder.AppendLine(FormatSchemaTriple(related));

				builder.AppendLine("]");
				builder.AppendLine();
			}
			return builder.ToString();
		}
	}
}
