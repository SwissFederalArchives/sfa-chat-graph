using AwosFramework.Generators.FunctionCalling;
using OpenAI;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Utils;
using System.Diagnostics;
using System.Text.Json;
using System;
using VDS.RDF.Query;
using VDS.RDF;
using SfaChatGraph.Server.Utils;
using Microsoft.EntityFrameworkCore.Query.Internal;
using SfaChatGraph.Server.RDF;
using sfa_chat_graph.Server.Services.CodeExecutionService;
using System.Text;
using AwosFramework.ApiClients.Jupyter.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using sfa_chat_graph.Server.Services.EventService;
using sfa_chat_graph.Server.Services.ChatService.Events;

namespace sfa_chat_graph.Server.Services.ChatService.OpenAI
{
	public partial class OpenAIChatService : ChatServiceBase<OpenAiChatContext>
	{
		private readonly FunctionCallRegistry _functionCalls;
		private readonly IGraphRag _graphDb;
		private readonly ChatClient _client;
		private readonly ChatTool[] _chatTools;
		private readonly ILogger _logger;

		private const string CHAT_SYS_PROMPT = $"""
		You are an helpfull assistant which answers questions with the help of generating sparql queries for the current database. Use your tool calls to query the database with sparql.
		To include IRI's try to also select intermediate values as response as long as they don't mess with the query, for example if you get a list of names, get a list of names and the respective iris of the subjects.
		If you encounter any query issues, try fixing them yourselve by using the provided exception message and calling the tool again.	
		Format your answers in markdown. Use tables or codeblocks where you see fit.
		If the conversation switches to a specific graph and you've obtained it's iri with list_graphs tool call get_schema next to get a ontology of the graph

		The following tools are available:
		- list_graphs: Use this tool to get a list of all graphs in the database
		- get_schema: Use this tool to get the schema of a graph use this as well if the user asks for a schema
		- query: Use this tool to query the database with sparql. When querying the graph database, try to include the IRI's in the query response as well even if not directly needed. This is important to know which part of the graph was used for the answer.
		- execute_code: Use this tool to write python code to visualize data or fully analyze large datasets, the code execution state is not stored, so variables from another call won't be accessible in the next call
		""";

		private static readonly SystemChatMessage ChatSystemMessage = new SystemChatMessage(CHAT_SYS_PROMPT);

		public OpenAIChatService(ChatClient client, FunctionCallRegistry functionCalls, ILoggerFactory loggerFactory, ICodeExecutionService codeExecutionService, IGraphRag graphDb)
		{
			_client = client;
			_functionCalls = functionCalls;
			_chatTools = functionCalls.GetFunctionCallMetas().Select(x => x.AsChatTool()).ToArray();
			_graphDb=graphDb;
			_logger = loggerFactory.CreateLogger<OpenAIChatService>();
		}

		record ToolHandleResponse(bool RequiresAction, bool ErrorsExceeded);
		private ChatCompletionOptions GetErrorHandlingOptions(ChatToolCall toolCall)
		{
			var options = new ChatCompletionOptions
			{
				Temperature = 0.15F,
				ToolChoice = ChatToolChoice.CreateFunctionChoice(toolCall.FunctionName)
			};

			options.Tools.AddRange(_chatTools);
			return options;
		}

		private async Task<Message> HandleQueryResultAsync(string toolCallId, SparqlResultSet result, string query, ChatContext ctx)
		{
			SparqlResultSet visualisation = null;

			try
			{
				await ctx.NotifyActivityAsync("Getting graph for visualisation", query);
				visualisation = await _graphDb.GetVisualisationResultAsync(result, query);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get visualisation result for query: {query}", query);
			}

			var csv = SparqlResultFormatter.ToCSV(result, 50);
			var toolMessage = CreateToolMessage(toolCallId, csv);
			var graphToolData = new ApiGraphToolData { DataGraph = result, VisualisationGraph = visualisation, Query = query };
			var apiMessage = toolMessage.AsApiMessage(graphToolData);
			return new Message(toolMessage, apiMessage);
		}

		private Message HandleCodeResult(string toolCallId, CodeExecutionResult result, string code)
		{
			var toolMessage = CreateToolMessage(toolCallId, FormatCodeResponse(result));
			var codeToolData = new ApiCodeToolData
			{
				Language = result.Language,
				Code = code,
				Success = result.Success,
				Error = result.Error,
				Data = result.Fragments?.SelectMany(x => x.BinaryData.Select(item =>
			{
				var mimeType = item.Key;
				var isText = mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || mimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase);
				return new ApiToolData
				{
					Description = x.Description,
					Id = x.BinaryIDs[item.Key],
					Content = item.Value,
					IsBase64Content = isText == false,
					MimeType = mimeType
				};
			}))?.ToArray()
			};

			var apiMessage = toolMessage.AsApiMessage(codeToolData);
			return new Message(toolMessage, apiMessage);
		}

		private static string FormatCodeResponse(CodeExecutionResult result)
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

		private static ToolChatMessage CreateToolMessage(string id, string content)
		{
			return ChatMessage.CreateToolMessage(id, $"ToolCallId: {id}\r\nResult: {content}");
		}

		private async Task<Message> HandleToolCallImplAsync(ChatToolCall toolCall, ChatContext ctx)
		{
			var toolParameters = JsonDocument.Parse(toolCall.FunctionArguments);
			var detail = JsonSerializer.Serialize(new { FunctionName = toolCall.FunctionName, Arguments = toolParameters });
			await ctx.NotifyActivityAsync($"Handling {toolCall.FunctionName} tool call", detail);
			var responseObj = await _functionCalls.CallFunctionAsync(toolCall.FunctionName, toolParameters, ctx);
			switch (toolCall.FunctionName)
			{
				case FunctionCallRegistry.LIST_GRAPHS:
					var graphs = (IEnumerable<string>)responseObj;
					var toolMessage = CreateToolMessage(toolCall.Id, string.Join("\n", graphs));
					return toolMessage;

				case FunctionCallRegistry.GET_SCHEMA:
					var schema = (string)responseObj;
					return CreateToolMessage(toolCall.Id, schema);

				case FunctionCallRegistry.QUERY:
					var sparqlResult = (SparqlResultSet)responseObj;
					var queryString = toolParameters.RootElement.GetProperty("Query").GetString();
					return await HandleQueryResultAsync(toolCall.Id, sparqlResult, queryString, ctx);

				case FunctionCallRegistry.DESCRIBE:
					var graph = (IGraph)responseObj;
					var csv = SparqlResultFormatter.ToCSV(graph);
					var msg = CreateToolMessage(toolCall.Id, csv);
					var iri = toolParameters.RootElement.GetProperty("Iri").GetString();
					var sparqlRes = SparqlResultFormatter.ToResultSet(graph);
					var graphData = new ApiGraphToolData { DataGraph = sparqlRes, VisualisationGraph = sparqlRes, Query = Queries.DescribeQuery(iri) };
					var apiMsg = msg.AsApiMessage();
					return new Message(msg, apiMsg);

				case FunctionCallRegistry.EXECUTE_CODE:
					var result = (CodeExecutionResult)responseObj;
					var codeString = toolParameters.RootElement.GetProperty("Code").GetString();
					return HandleCodeResult(toolCall.Id, result, codeString);

				default:
					throw new NotImplementedException($"Tool call {toolCall.FunctionName} not implemented yet. Please implement it in the HandleToolCallImplAsync method.");
			}
		}

		private async Task<Message> TryHandleErrorAsync(OpenAiChatContext context, ChatMessage completion, ChatToolCall toolCall, Exception ex, int tries)
		{
			var originalId = toolCall.Id;
			var opts = GetErrorHandlingOptions(toolCall);
			while (tries-- > 0)
			{
				await context.NotifyActivityAsync($"Handling {toolCall.FunctionName} tool error, {tries+1} tries left", ex.ToString());
				var messages = context.OpenAIHistory.ToList();
				string errorDetail = null;
				if (ex.Data.Contains("error"))
					errorDetail = $"\nError Detail: {JsonSerializer.Serialize(ex.Data["error"])}";

				var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, $"You're tool call yielded an error: {ex.Message}\nTry to fix the error and call the tool again. Don't just resend the last tool call, try to fix it\nThis is most likel due to malformed sparql or forgotten prefixes\nAdhere strictly to the sparql syntax, if a group in the select doesn't work try to bind the group inside the select instead{errorDetail}");
				messages.Add(toolMessage);
				var response = await _client.CompleteChatAsync(messages, opts);
				if (response == null || response.Value.ToolCalls.Count != 1)
					continue;

				completion = ChatMessage.CreateAssistantMessage(response);
				toolCall = response.Value.ToolCalls.First();

				try
				{
					toolCall.Id = originalId;
					return await HandleToolCallImplAsync(toolCall, context);
				}
				catch (Exception newEx)
				{
					ex = newEx;
					continue;
				}
			}

			await context.NotifyActivityAsync($"Handling {toolCall.FunctionName} tool error, no more tries left");
			return null;
		}

		private async Task<Message> HandleToolCallAsync(OpenAiChatContext ctx, ChatMessage completion, ChatToolCall toolCall, int maxErrors)
		{
			try
			{
				return await HandleToolCallImplAsync(toolCall, ctx);
			}
			catch (Exception ex)
			{
				return await TryHandleErrorAsync(ctx, completion, toolCall, ex, maxErrors);
			}
		}

		private async Task<bool> HandleToolCallsAsync(OpenAiChatContext ctx, ChatMessage completionMessage, ChatCompletion completion, int maxErrors)
		{
			foreach (var toolCall in completion.ToolCalls)
			{
				var msg = await HandleToolCallAsync(ctx, completionMessage, toolCall, maxErrors);
				if (msg == null)
					return true;

				ctx.AddMessage(msg);
			}

			return false;
		}


		private async Task<ToolHandleResponse> HandleResponseAsync(OpenAiChatContext ctx, ChatCompletion completion, AssistantChatMessage response, int maxErrors)
		{
			await ctx.NotifyActivityAsync("Handling response");
			switch (completion.FinishReason)
			{
				case ChatFinishReason.Stop:
					return new ToolHandleResponse(false, false);

				case ChatFinishReason.ToolCalls:
					var errorsExceeded = await HandleToolCallsAsync(ctx, response, completion, maxErrors);
					return new ToolHandleResponse(true, errorsExceeded);

				default:
					return null;
			}
		}

		public override OpenAiChatContext CreateContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history)
		{
			return new OpenAiChatContext(chatId, events, history, ChatSystemMessage);
		}

		public override async Task<CompletionResult> CompleteAsync(OpenAiChatContext ctx, float temperature, int maxErrors)
		{
			var options = new ChatCompletionOptions { Temperature = temperature };
			options.Tools.AddRange(_chatTools);

			ToolHandleResponse toolResponse = null;
			do
			{
				await ctx.NotifyActivityAsync("Generating response");
				var completion = await _client.CompleteChatAsync(ctx.OpenAIHistory, options);
				var response = ChatMessage.CreateAssistantMessage(completion);
				ctx.AddMessage(response);
				toolResponse = await HandleResponseAsync(ctx, completion, response, maxErrors);
				if (toolResponse.ErrorsExceeded)
					return new CompletionResult(null, "Max errors exceeded", false);

				if(ctx.Created.Count > 30)
					return new CompletionResult(ctx.Created.ToArray(), "Max messages exceeded", false);

			} while (toolResponse?.RequiresAction == true);

			await ctx.NotifyDoneAsync();
			return new CompletionResult(ctx.Created.ToArray(), null, true);
		}
	}
}
