using AwosFramework.Generators.FunctionCalling;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.RDF.Models;
using sfa_chat_graph.Server.Utils;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Models;
using SfaChatGraph.Server.Utils;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Writing.Formatting;

namespace SfaChatGraph.Server.Controllers
{
	[ApiController]
	[Route("/api/v1/rdf")]
	public class RdfController : ControllerBase
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IGraphRag _graphDb;
		private readonly ChatClient _chatClient;
		private SystemChatMessage _answerSysMessage;
		private Task _initTask;
		private ILogger _logger;
		private readonly FunctionCallRegistry _functionCallingRegistry;
		private readonly Lazy<ChatTool[]> _chatTools;

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

		private static ChatCompletionOptions _completionOptions;

		public RdfController(IGraphRag graphDb, ChatClient chatClient, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, FunctionCallRegistry functionCallingRegistry)
		{
			_graphDb=graphDb;
			_logger=loggerFactory.CreateLogger<RdfController>();
			_chatClient=chatClient;
			_answerSysMessage=new SystemChatMessage("Your job is to answer the user's question given the result of a knowledgegraph lookup. If the question can't be answered with the given data clearly state so");
			_serviceProvider=serviceProvider;
			_functionCallingRegistry=functionCallingRegistry;
			_completionOptions = new ChatCompletionOptions();
			_completionOptions.Tools.AddRange(functionCallingRegistry.GetFunctionCallMetas().Select(x => x.AsChatTool()));
		}

		[HttpGet("describe")]
		[ProducesResponseType<SparqlStarResult>(StatusCodes.Status200OK)]
		public async Task<IActionResult> DescribeAsync([FromQuery] string subject)
		{
			var graph = await _graphDb.DescribeAsync(subject);

			var result = new SparqlResultSet();
			foreach (var triple in graph.Triples)
			{
				var row = new SparqlResult();
				row.SetValue("s", triple.Subject);
				row.SetValue("p", triple.Predicate);
				row.SetValue("o", triple.Object);
				result.Results.Add(row);
			}

			if (result.Count == 0)
				return NotFound();

			return Ok(result);
		}


		[HttpPost("chat")]
		[ProducesResponseType<ApiMessage[]>(StatusCodes.Status200OK)]
		public async Task<IActionResult> ChatAsync([FromBody] ApiChatRequest chat)
		{
			List<ChatMessage> messages = [ChatSystemMessage];
			messages.AddRange(chat.History.Select(x => x.AsOpenAIMessage()));
			var apiMessages = new List<ApiMessage>();

			bool requiresAction = true;
			do
			{
				var response = await _chatClient.CompleteChatAsync(messages, _completionOptions);
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

									switch (toolCall.FunctionName)
									{
										case "list_graphs":
											{
												var graphs = (IEnumerable<string>)toolResponse;
												var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, string.Join("\n", graphs));
												messages.Add(toolMessage);
												apiMessages.Add(toolMessage.AsApiMessage());
												break;
											}

										case "get_schema":
											{
												var schema = (string)toolResponse;
												var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, schema);
												messages.Add(toolMessage);
												apiMessages.Add(toolMessage.AsApiMessage());
												break;
											}

										case "query":
											{
												var graphRagRes = (SparqlResultSet)toolResponse;
												var queryString = toolParameters.RootElement.GetProperty("Query").GetString();
												var visualisation = await _graphDb.GetVisualisationResultAsync(graphRagRes, queryString);
												var csv = SparqlResultFormatter.ToCSV(graphRagRes);
												var toolMessage = ToolChatMessage.CreateToolMessage(toolCall.Id, csv);
												messages.Add(toolMessage);
												apiMessages.Add(toolMessage.AsApiMessage(visualisation));
												break;
											}
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
	}
}
