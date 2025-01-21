using AwosFramework.Generators.FunctionCalling;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.RDF.Models;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Models;
using SfaChatGraph.Server.Utils;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SfaChatGraph.Server.Controllers
{
	[ApiController]
	[Route("/api/v1/rdf")]
	public class RdfController : ControllerBase
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IGraphRag _graphDb;
		private readonly ChatClient _chatClient;
		private SystemChatMessage _schemaSysMessage;
		private SystemChatMessage _answerSysMessage;
		private Task _initTask;
		private ILogger _logger;
		private readonly FunctionCallRegistry _functionCallingRegistry;
		private readonly Lazy<ChatTool[]> _chatTools;

		public RdfController(IGraphRag graphDb, ChatClient chatClient, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, FunctionCallRegistry functionCallingRegistry)
		{
			_graphDb=graphDb;
			_logger=loggerFactory.CreateLogger<RdfController>();
			_chatClient=chatClient;
			_initTask = _graphDb.InitAsync().ContinueWith(x =>
			{
				_schemaSysMessage=new SystemChatMessage($"You're an assistant generating SPARQL Code to answer the users questions. Only answer in code blocks like \n```sparql\nselect * where \n{{ ?s <http://example.com> ?o .\n}}\n```\nYou are allowed to use Prefixes. Remember that when using prefixes you can't have slashs after a prefixed value. Make sure to produce valid SPARQL syntax where Uri are formatted like <uri>\n If the schema doesn't cover the needed information reply with `<INCOMPATIBLE_SCHEMA>`. The current database has the following schema: \n\n{graphDb.Schema}");
			});

			_answerSysMessage=new SystemChatMessage("Your job is to answer the user's question given the result of a knowledgegraph lookup. If the question can't be answered with the given data clearly state so");
			_serviceProvider=serviceProvider;
			_functionCallingRegistry=functionCallingRegistry;
			_chatTools = new Lazy<ChatTool[]>(() => _functionCallingRegistry.GetFunctionCallMetas().Select(x => x.AsChatTool()).ToArray());
		}

		[HttpGet("describe")]
		[ProducesResponseType<SparqlStarResult>(StatusCodes.Status200OK)]
		public async Task<IActionResult> DescribeAsync([FromQuery] string subject)
		{
			var graph = await _graphDb.QueryAsync($"DESCRIBE <{subject}>");
			if (graph.Results.Length==0)
				return NotFound();



			return Ok(graph);
		}


		[HttpPost("chat")]
		[ProducesResponseType<ApiMessage[]>(StatusCodes.Status200OK)]
		public async Task<IActionResult> ChatAsync([FromBody] ApiChatRequest chat)
		{
			var sysPrompt = $"""
			You are an helpfull assistant which answers questions with the help of generating sparql queries for the current database. Use your tool calls to query the database with sparql.
			When querying the graph database, try to include the IRI's in the query response as well even if not directly needed. This is important to know which part of the graph was used for the answer.
			To include IRI's try to also select intermediate values as response as long as they don't mess with the query, for example if you get a list of names, get a list of names and the respective iris of the subjects.
			If you encounter any query issues, try fixing them yourselve by using the provided exception message and calling the tool again.
			
			Format your answers in markdown. Use tables or codeblocks where you see fit.
			
			The scheme of the current database is:
			{_graphDb.Schema}
			""";

			var options = new ChatCompletionOptions();
			foreach (var cf in _chatTools.Value)
				options.Tools.Add(cf);

			List<ChatMessage> messages = [ChatMessage.CreateSystemMessage(sysPrompt)];
			messages.AddRange(chat.History.Select(x => x.AsOpenAIMessage()));
			var apiMessages = new List<ApiMessage>();

			bool requiresAction = true;
			do
			{
				var response = await _chatClient.CompleteChatAsync(messages, options);
				var message = ChatMessage.CreateAssistantMessage(response);
				messages.Add(message);
				apiMessages.Add(message.AsApiMessage());

				switch (response.Value.FinishReason)
				{
					case ChatFinishReason.Stop:
						{
							requiresAction = false;
							break;
						}

					case ChatFinishReason.ToolCalls:
						{
							foreach (var toolCall in response.Value.ToolCalls)
							{
								try
								{
									var toolParameters = JsonDocument.Parse(toolCall.FunctionArguments);
									var toolResponse = await _functionCallingRegistry.CallFunctionAsync(toolCall.FunctionName, toolParameters);

									if (toolResponse is string stringData)
									{
										var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, stringData);
										messages.Add(toolMessage);
										apiMessages.Add(toolMessage.AsApiMessage());
									}
									else if (toolResponse is GraphRagQueryResult graphRagRes)
									{
										var graphData = graphRagRes.RagResult;
										var builder = new StringBuilder();
										builder.AppendLine(string.Join(";", graphData.Head.Vars));
										foreach (var row in graphData.Results)
											builder.AppendLine(string.Join(";", graphData.Head.Vars.Select(x => row[x]?.Value)));

										var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, builder.ToString());
										messages.Add(toolMessage);
										apiMessages.Add(toolMessage.AsApiMessage(graphRagRes.VisualisationResult));
									}
								}
								catch (Exception ex)
								{
									if (chat.MaxErrors-- <= 0)
									{
										return BadRequest($"Error in ToolCall: {ex.Message}");
									}
									else
									{
										var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, $"You're tool call yielded an error: {ex.Message}\nthis is most likel due to malformed sparql or forgotten prefixes ");
										messages.Add(toolMessage);

										// remove error from api response
										apiMessages.RemoveAll(x => x is ApiToolCallMessage tcm && tcm.ToolCalls.Any(y => y.ToolCallId ==toolCall.Id));
									}

									Debug.WriteLine(ex);
								}
							}
							break;
						}

					default:
						return BadRequest($"Unexpected ChatFinishReason: {response.Value.FinishReason}");
				}
			} while (requiresAction);

			return Ok(apiMessages);
		}

		[HttpPost("query")]
		[ProducesResponseType<ApiQueryResponse>(StatusCodes.Status200OK)]
		public async Task<IActionResult> QueryAsync([FromBody] ApiQueryRequest request)
		{
			await _initTask;
			var message = new UserChatMessage(request.Query);
			string lastError = null;
			do
			{
				try
				{
					var response = await _chatClient.CompleteChatAsync(_schemaSysMessage, message);
					var text = response.Value.Content[0].Text;
					if (text.Contains("<INCOMPATIBLE_SCHEMA>"))
						return Ok(new ApiQueryResponse { IsSuccess = false, IncompatibleSchema = true, Answer = text });

					var start = text.IndexOf("```sparql\n");
					var end = text.IndexOf("\n```", start);
					var sparql = text.Substring(start + 10, end - start - 10);


					var result = await _graphDb.QueryAsync(sparql);
					var builder = new StringBuilder();
					builder.AppendLine(string.Join(",", result.Head.Vars));
					foreach (var item in result.Results)
						builder.AppendLine(string.Join(", ", result.Head.Vars.Select(x => item[x]?.Value)));

					var userMessage = $"Question: {request.Query}\n\nQuery: ```sparql\n{sparql}\n```\n\nData: {builder.ToString()}";
					response = await _chatClient.CompleteChatAsync(_answerSysMessage, new UserChatMessage(userMessage));
					return Ok(new ApiQueryResponse { IsSuccess = true, IncompatibleSchema = false, Graph = result, Answer = response.Value.Content[0].Text });
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Error in processing AI results");
					lastError = e.ToString();
				}

			} while (request.MaxErrors-- > 0);

			return Ok(new ApiQueryResponse { IsSuccess = false, Error = lastError });

		}
	}
}
