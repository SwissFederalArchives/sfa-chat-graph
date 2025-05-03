using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.ChatService.OpenAI;
using sfa_chat_graph.Server.Utils;
using System.Text.Json;
using VDS.RDF.Query;
using VDS.RDF;
using AwosFramework.Generators.FunctionCalling;
using sfa_chat_graph.Server.RDF;
using sfa_chat_graph.Server.Services.CodeExecutionService;
using VDS.RDF.Parsing.Tokens;
using OpenAI.Chat;

namespace sfa_chat_graph.Server.Services.ChatService.Abstract
{
	public abstract class AbstractChatService<TMessage, TContext, TOptions> : ChatServiceBase<TContext> where TContext : AbstractChatContext<TMessage>
	{
		protected readonly ILogger _logger;
		protected readonly IGraphRag _graph;
		protected readonly FunctionCallRegistry _functionCalls;

		protected abstract TOptions GetOptionsWithSettings(float? temperature = null, string functionName = null, bool includeTools = false);
		protected abstract TMessage CreateToolResponse(string id, string content);
		protected abstract Task<Message<TMessage>> CompleteChatAsync(IEnumerable<TMessage> messages, TOptions options);

		protected AbstractChatService(ILogger logger, IGraphRag graph, FunctionCallRegistry functionCalls)
		{
			_logger=logger;
			_graph=graph;
			_functionCalls=functionCalls;
		}

		protected virtual Message<TMessage> CreateGenericToolResponse(TContext ctx, string id, string content)
		{
			var msg = CreateToolResponse(id, content);
			var api = ctx.ToApiMessage(msg);
			return new Message<TMessage>(msg, api);
		}

		protected virtual async Task<Message<TMessage>> HandleQueryResultAsync(TContext ctx, string toolCallId, SparqlResultSet result, string query)
		{
			SparqlResultSet visualisation = null;

			try
			{
				await ctx.NotifyActivityAsync("Getting graph for visualisation", query);
				visualisation = await _graph.GetVisualisationResultAsync(result, query);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get visualisation result for query: {query}", query);
			}

			var csv = LLMFormatter.ToCSV(result, 50);
			var toolMessage = CreateToolResponse(toolCallId, csv);
			var graphToolData = new ApiGraphToolData { DataGraph = result, VisualisationGraph = visualisation, Query = query };
			var apiMessage = ctx.ToApiMessage(toolMessage, graphToolData);
			return new Message<TMessage>(toolMessage, apiMessage);
		}

		protected virtual Message<TMessage> HandleCodeResult(TContext ctx, string toolCallId, CodeExecutionResult result, string code)
		{
			var toolMessage = CreateToolResponse(toolCallId, LLMFormatter.FormatCodeResponse(result));
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

			return new Message<TMessage>(toolMessage, ctx.ToApiMessage(toolMessage, null, codeToolData));
		}

		protected async Task<Message<TMessage>> HandleToolCallAsync(TContext ctx, ApiToolCall toolCall)
		{
			_logger.LogInformation("Handling tool call {ToolCall}", toolCall.ToolId);
			_logger.LogDebug("Tool call arguments: {ToolCall}", toolCall.Arguments.RootElement.ToString());
			await ctx.NotifyActivityAsync($"Handling {toolCall.ToolId} tool call", $"Handling tool which AI Model called, Id is {toolCall.ToolCallId}", toolCall.Arguments.RootElement.ToString());
			var responseObj = await _functionCalls.CallFunctionAsync(toolCall.ToolId, toolCall.Arguments, ctx);
			switch (toolCall.ToolId)
			{
				case FunctionCallRegistry.LIST_GRAPHS:
					var graphs = (IEnumerable<string>)responseObj;
					var toolMessage = CreateGenericToolResponse(ctx, toolCall.ToolCallId, string.Join("\n", graphs));
					return toolMessage;

				case FunctionCallRegistry.GET_SCHEMA:
					var schema = (string)responseObj;
					return CreateGenericToolResponse(ctx, toolCall.ToolCallId, schema);

				case FunctionCallRegistry.QUERY:
					var sparqlResult = (SparqlResultSet)responseObj;
					var queryString = toolCall.Arguments.RootElement.GetProperty("Query").GetString();
					return await HandleQueryResultAsync(ctx, toolCall.ToolCallId, sparqlResult, queryString);

				case FunctionCallRegistry.DESCRIBE:
					var graph = (IGraph)responseObj;
					var csv = LLMFormatter.ToCSV(graph);
					var msg = CreateToolResponse(toolCall.ToolCallId, csv);
					var iri = toolCall.Arguments.RootElement.GetProperty("Iri").GetString();
					var sparqlRes = LLMFormatter.ToResultSet(graph);
					var graphData = new ApiGraphToolData { DataGraph = sparqlRes, VisualisationGraph = sparqlRes, Query = Queries.DescribeQuery(iri) };
					var apiMsg = ctx.ToApiMessage(msg, graphData);
					return new Message<TMessage>(msg, apiMsg);

				case FunctionCallRegistry.EXECUTE_CODE:
					var result = (CodeExecutionResult)responseObj;
					var codeString = toolCall.Arguments.RootElement.GetProperty("Code").GetString();
					return HandleCodeResult(ctx, toolCall.ToolCallId, result, codeString);

				default:
					throw new NotImplementedException($"Tool call {toolCall.ToolId} not implemented yet. Please implement it in the HandleToolCallImplAsync method.");
			}
		}


		protected virtual async Task<Message<TMessage>> TryHandleErrorAsync(TContext ctx, ApiToolCall toolCall, Exception ex, int tries)
		{
			var originalId = toolCall.ToolCallId;
			var options = GetOptionsWithSettings(functionName: toolCall.ToolId);
			while (tries-- > 0)
			{
				await ctx.NotifyActivityAsync($"Handling {toolCall.ToolId} tool error", $"Error handling has {tries} tries left", ex.ToString());
				var messages = ctx.InternalHistory.ToList();
				string errorDetail = null;
				if (ex.Data.Contains("error"))
					errorDetail = $"\nError Detail: {JsonSerializer.Serialize(ex.Data["error"])}";

				var toolMessage = CreateToolResponse(toolCall.ToolCallId, $"You're tool call yielded an error: {ex.Message}\nTry to fix the error and call the tool again. Don't just resend the last tool call, try to fix it\nThis is most likel due to malformed sparql or forgotten prefixes\nAdhere strictly to the sparql syntax, if a group in the select doesn't work try to bind the group inside the select instead{errorDetail}");
				messages.Add(toolMessage);
				var response = await CompleteChatAsync(messages, options);
				if (response.ApiMessage is not ApiToolCallMessage toolCallMessage || toolCallMessage.ToolCalls.Length != 1)
					continue;



				try
				{
					toolCall = toolCallMessage.ToolCalls.First();
					return await HandleToolCallAsync(ctx, toolCall);
				}
				catch (Exception newEx)
				{
					ex = newEx;
					continue;
				}
			}

			await ctx.NotifyActivityAsync($"Handling {toolCall.ToolId} tool error, no more tries left");
			return null;
		}


		protected virtual async Task<Message<TMessage>> TryHandleToolCallAsync(TContext ctx, ApiToolCall toolCall, int maxErrors)
		{
			try
			{
				return await HandleToolCallAsync(ctx, toolCall);
			}
			catch (Exception ex)
			{
				return await TryHandleErrorAsync(ctx, toolCall, ex, maxErrors);
			}
		}

		protected virtual async Task<ToolCallsResult> MakeToolCallsAsync(TContext ctx, Message<TMessage> completion, int maxErrors)
		{
			if (completion.ApiMessage is not ApiToolCallMessage toolCallMessage)
				return ToolCallsResult.NoToolCalls;

			foreach (var toolCall in toolCallMessage.ToolCalls)
			{
				var msg = await TryHandleToolCallAsync(ctx, toolCall, maxErrors);
				if (msg == null)
					return ToolCallsResult.ErrorsExceeded;

				ctx.AddMessage(msg);
			}

			return ToolCallsResult.Success;
		}

		public override async Task<CompletionResult> CompleteAsync(TContext ctx, float temperature, int maxErrors)
		{
			var options = GetOptionsWithSettings(temperature: temperature, includeTools: true);

			try
			{
				ToolCallsResult toolResponse = ToolCallsResult.None;
				do
				{
					await ctx.NotifyActivityAsync("Generating response");
					var completion = await CompleteChatAsync(ctx.InternalHistory, options);
					ctx.AddMessage(completion);
					toolResponse = await MakeToolCallsAsync(ctx, completion, maxErrors);
					if (toolResponse == ToolCallsResult.ErrorsExceeded)
						return new CompletionResult(null, "Max errors exceeded", false);

					if (ctx.Created.Count() > 30)
						return new CompletionResult(ctx.Created.ToArray(), "Max messages exceeded", false);

				} while (toolResponse != ToolCallsResult.NoToolCalls);

				return new CompletionResult(ctx.Created.ToArray(), null, true);
			}
			finally
			{
				await ctx.NotifyDoneAsync();
			}
		}

	}
}
