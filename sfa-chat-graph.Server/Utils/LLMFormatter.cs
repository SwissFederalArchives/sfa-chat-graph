using AngleSharp.Text;
using AwosFramework.ApiClients.Jupyter.Utils;
using Microsoft.EntityFrameworkCore;
using SfaChatGraph.Server.Services.CodeExecutionService;
using SfaChatGraph.Server.Utils;
using System.Text;
using VDS.Common.Collections.Enumerations;
using VDS.RDF;
using VDS.RDF.Query;

namespace SfaChatGraph.Server.Utils
{
	public class LLMFormatter
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

		public static string FormatSchemaNode(INode node) => node switch
		{
			IUriNode uriNode => $"<{uriNode.Uri}>",
			ILiteralNode literalNode => $"{literalNode.Value}",
			_ => "BLANK"
		};

		private static string EscapeCSVValue(string line)
		{
			line = line.Replace("\r", "\\r").Replace("\n", "\\n");
			if (line.Contains(";"))
				return $"\"{line.Replace("\"", "\"\"")}\"";

			return line;
		}

		private static string CsvFormatNode(INode node)
		{
			var str = node switch
			{
				UriNode uriNode => uriNode.Uri.ToString(),
				LiteralNode literalNode => literalNode.Value,
				_ => string.Empty
			};

			return EscapeCSVValue(str);
		}

		public static SparqlResultSet ToResultSet(IGraph graph, string[] header = null)
		{
			header ??= ["s", "p", "o"];
			var set = new SparqlResultSet();
			foreach (var triple in graph.Triples)
			{
				var result = new SparqlResult(header.ZipPair(triple.Nodes));
				set.Results.Add(result);
			}

			return set;
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

			var variables = resultSet.Variables.Select(EscapeCSVValue).ToArray();
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

		public static string FormatCodeResponse(CodeExecutionResult result)
		{
			if (result.Success == false)
			{
				return $"The code yielded Errors:\n{result.Error}";
			}
			else
			{
				var builder = new StringBuilder();
				builder.AppendLine("The code yielded the following results:");
				foreach (var (isLast, fragment) in result.Fragments.IsLast())
				{
					builder.Append($"Fragment: {fragment.Id}");
					if (string.IsNullOrEmpty(fragment.Description) == false)
					{
						builder.Append("Description: ");
						builder.AppendLine(fragment.Description);
					}

					foreach (var (key, value) in fragment.BinaryData)
					{
						if (key.StartsWith("text/") || key.Equals("application/json", StringComparison.OrdinalIgnoreCase))
						{
							builder.Append("Data[");
							builder.Append(key);
							builder.AppendLine("]:");
							builder.AppendLine(value.Ellipsis(512, "result cut off after 512 chars"));
						}
						else
						{
							builder.AppendLine($"BinaryData[{key}]: tool-data://{fragment.BinaryIDs[key]}");
						}
					}

					if (isLast == false)
						builder.AppendLine();

				}

				return builder.ToString();
			}
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
