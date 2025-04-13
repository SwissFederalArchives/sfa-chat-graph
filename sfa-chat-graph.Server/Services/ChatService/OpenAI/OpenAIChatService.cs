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

namespace sfa_chat_graph.Server.Services.ChatService.OpenAI
{
	public class OpenAIChatService : IChatService
	{
		private readonly FunctionCallRegistry _functionCalls;
		private readonly IGraphRag _graphDb;
		private readonly ChatClient _client;
		private readonly ChatTool[] _chatTools;

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
		""";

		private static readonly SystemChatMessage ChatSystemMessage = new SystemChatMessage(CHAT_SYS_PROMPT);


		public OpenAIChatService(ChatClient client, FunctionCallRegistry functionCalls, ICodeExecutionService codeExecutionService)
		{
			_client = client;
			_functionCalls = functionCalls;
			_chatTools = functionCalls.GetFunctionCallMetas().Select(x => x.AsChatTool()).ToArray();
		}


		record Message(ChatMessage OpenAi, ApiMessage Api)
		{
			public Message(ChatMessage msg) : this(msg, msg.AsApiMessage())
			{

			}

			public static implicit operator Message(ChatMessage msg) => new(msg);
		}

		record ToolHandleResponse(IEnumerable<Message> Messages, bool RequiresAction, bool ErrorsExceeded);



		private ChatCompletionOptions GetErrorHandlingOptions(ChatToolCall toolCall)
		{
			var options = new ChatCompletionOptions
			{
				Temperature = 0,
				ToolChoice = ChatToolChoice.CreateFunctionChoice(toolCall.FunctionName)
			};

			options.Tools.AddRange(_chatTools);
			return options;
		}

		private async Task<Message> HandleQueryResultAsync(string toolCallId, SparqlResultSet result, string query)
		{
			var visualisation = await _graphDb.GetVisualisationResultAsync(result, query);
			var csv = SparqlResultFormatter.ToCSV(result);
			var toolMessage = ToolChatMessage.CreateToolMessage(toolCallId, csv);
			var apiMessage = toolMessage.AsApiMessage(query, visualisation);
			return new Message(toolMessage, apiMessage);
		}

		private async Task<Message> HandleToolCallImplAsync(ChatToolCall toolCall)
		{
			var toolParameters = JsonDocument.Parse(toolCall.FunctionArguments);
			var responseObj = await _functionCalls.CallFunctionAsync(toolCall.FunctionName, toolParameters);
			switch (toolCall.FunctionName)
			{
				case FunctionCallRegistry.LIST_GRAPHS:
					var graphs = (IEnumerable<string>)responseObj;
					var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, string.Join("\n", graphs));
					return toolMessage;

				case FunctionCallRegistry.GET_SCHEMA:
					var schema = (string)responseObj;
					return ToolChatMessage.CreateToolMessage(toolCall.Id, schema);

				case FunctionCallRegistry.QUERY:
					var sparqlResult = (SparqlResultSet)responseObj;
					var queryString = toolParameters.RootElement.GetProperty("Query").GetString();
					return await HandleQueryResultAsync(toolCall.Id, sparqlResult, queryString);

				case FunctionCallRegistry.DESCRIBE:
					var graph = (IGraph)responseObj;
					var csv = SparqlResultFormatter.ToCSV(graph);
					return ToolChatMessage.CreateToolMessage(toolCall.Id, csv);

				default:
					throw new NotImplementedException($"Tool call {toolCall.FunctionName} not implemented yet. Please implement it in the HandleToolCallImplAsync method.");
			}
		}

		private async Task<Message> TryHandleErrorAsync(IEnumerable<ChatMessage> history, ChatMessage completion, ChatToolCall toolCall, Exception ex, int tries)
		{
			var originalId = toolCall.Id;
			var opts = GetErrorHandlingOptions(toolCall);
			while (tries-- > 0)
			{
				var messages = history.ToList();
				messages.Add(completion);
				var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, $"You're tool call yielded an error: {ex.Message}\nthis is most likel due to malformed sparql or forgotten prefixes ");
				messages.Add(toolMessage);
				var response = await _client.CompleteChatAsync(messages, opts);
				if (response == null || response.Value.FinishReason != ChatFinishReason.ToolCalls || response.Value.ToolCalls.Count != 1)
					continue;

				completion = ChatMessage.CreateAssistantMessage(response);
				toolCall = response.Value.ToolCalls.First();

				try
				{
					var res = await HandleToolCallImplAsync(toolCall);
					if (res.Api is ApiToolResponseMessage toolResponse)
						toolResponse.ToolCallId = originalId;

					var newOpenAiMsg = ToolChatMessage.CreateToolMessage(originalId, res.OpenAi.Content);
					return new Message(newOpenAiMsg, res.Api);
				}
				catch (Exception newEx)
				{
					ex = newEx;
					continue;
				}
			}

			return null;
		}

		private async Task<Message> HandleToolCallAsync(IEnumerable<ChatMessage> history, ChatMessage completion, ChatToolCall toolCall, int maxErrors)
		{
			try
			{
				return await HandleToolCallImplAsync(toolCall);
			}
			catch (Exception ex)
			{
				return await TryHandleErrorAsync(history, completion, toolCall, ex, maxErrors);
			}
		}

		private async Task<bool> HandleToolCallsAsync(IEnumerable<ChatMessage> history, ChatMessage completionMessage, ChatCompletion completion, List<Message> chatMessages, int maxErrors)
		{
			foreach (var toolCall in completion.ToolCalls)
			{
				var msg = await HandleToolCallAsync(history, completionMessage, toolCall, maxErrors);
				if (msg == null)
					return false;

				chatMessages.Add(msg);
			}

			return true;
		}


		private async Task<ToolHandleResponse> HandleResponseAsync(IEnumerable<ChatMessage> history, ChatCompletion completion, int maxErrors)
		{
			var response = ChatMessage.CreateAssistantMessage(completion);
			List<Message> list = [response];

			switch (completion.FinishReason)
			{
				case ChatFinishReason.Stop:
					return new ToolHandleResponse(list, false, false);

				case ChatFinishReason.ToolCalls:
					var errorsExceeded = await HandleToolCallsAsync(history, response, completion, list, maxErrors);
					return new ToolHandleResponse(list, true, errorsExceeded);

				default:
					return null;
			}
		}

		public async Task<CompletionResult> CompleteAsync(ApiMessage[] history, float temperature, int maxErrors)
		{
			var options = new ChatCompletionOptions { Temperature = temperature };
			options.Tools.AddRange(_chatTools);

			List<ChatMessage> messages = [ChatSystemMessage];
			messages.AddRange(history.Select(x => x.AsOpenAIMessage()));

			var resultMessages = new List<Message>();
			ToolHandleResponse response = null;
			do
			{
				var chatResponse = await _client.CompleteChatAsync(messages, options);
				var message = ChatMessage.CreateAssistantMessage(chatResponse);
				messages.Add(message);
				response = await HandleResponseAsync(messages, chatResponse, maxErrors);
				if (response.ErrorsExceeded)
					return new CompletionResult(null, false);

				messages.AddRange(response.Messages.Select(x => x.OpenAi));
				resultMessages.AddRange(response.Messages);
			} while (response?.RequiresAction == true);

			return new CompletionResult(resultMessages.Select(x => x.Api).ToArray(), true);
		}
	}
}
