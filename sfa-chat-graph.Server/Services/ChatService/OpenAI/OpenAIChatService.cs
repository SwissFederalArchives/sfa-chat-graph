using AwosFramework.Generators.FunctionCalling;
using Lucene.Net.Util;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Writers;
using OpenAI.Chat;
using SfaChatGraph.Server.Config;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.Services.ChatService.Abstract;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.EventService;
using SfaChatGraph.Server.Utils.ServiceCollection;

namespace SfaChatGraph.Server.Services.ChatService.OpenAI
{
	[ServiceImplementation<IChatService, AiConfig>(Key = "OpenAI", Lifetime = ServiceLifetime.Scoped)]
	public class OpenAIChatService : AbstractChatService<ChatMessage, OpenAIChatContext, ChatCompletionOptions>
	{
		private readonly ChatClient _client;
		private readonly ChatTool[] _tools;

		private readonly AiConfig _config;

		private const string CHAT_SYS_PROMPT = $"""
		You are an helpfull assistant which answers questions with the help of generating sparql queries for the current database. Use your tool calls to query the database with sparql.
		To include IRI's try to also select intermediate values as response as long as they don't mess with the query, for example if you get a list of names, get a list of names and the respective iris of the subjects.
		If you encounter any query issues, try fixing them yourselve by using the provided exception message and calling the tool again.	
		Format your answers in markdown. Use tables or codeblocks where you see fit.
		If the conversation switches to a specific graph and you've obtained it's iri with list_graphs tool call get_schema next to get a ontology of the graph
		Inside a Schema "LITERAL" means the predicate points to a literal and FIXED means it points to one or more fixed iris outside the database

		The following tools are available:
		- list_graphs: Use this tool to get a list of all graphs in the database
		- get_schema: Use this tool to get the schema of a graph use this as well if the user asks for a schema
		- query: Use this tool to query the database with sparql. When querying the graph database, try to include the IRI's in the query response as well even if not directly needed. This is important to know which part of the graph was used for the answer.
		- execute_code: Use this tool to write python code to visualize data or fully analyze large datasets, the code execution state is not stored, so variables from another call won't be accessible in the next call. Don't hardcode data into your code snippet if not necessary. Reference it instead as explained in the tool call description
		""";

		private static readonly SystemChatMessage ChatSystemMessage = new SystemChatMessage(CHAT_SYS_PROMPT);

		public OpenAIChatService(IOptions<AiConfig> config, ILoggerFactory loggerFactory, FunctionCallRegistry functionCalls, IGraphRag graphRag) : base(loggerFactory.CreateLogger<OpenAIChatService>(), graphRag, functionCalls)
		{
			_config = config.Value;
			_client = new ChatClient(_config.Model, _config.ApiKey);
			_tools = _functionCalls.GetFunctionCallMetas().Select(x => x.AsChatTool()).ToArray();
		}

		public override OpenAIChatContext CreateContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history)
		{
			return new OpenAIChatContext(chatId, events, history);
		}

		protected override async Task<Message<ChatMessage>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
		{
			var message = await _client.CompleteChatAsync(messages.Prepend(ChatSystemMessage), options);
			var assistant = ChatMessage.CreateAssistantMessage(message);
			return new Message<ChatMessage>(assistant, assistant.AsApiMessage());
		}

		protected override ChatMessage CreateToolResponse(string id, string content)
		{
			return ChatMessage.CreateToolMessage(id, content);
		}

		protected override ChatCompletionOptions GetOptionsWithSettings(float? temperature = null, string functionName = null, bool includeTools = false)
		{
			var options = new ChatCompletionOptions();
			if (temperature != null)
				options.Temperature = temperature.Value;

			if (functionName != null)
				options.ToolChoice = ChatToolChoice.CreateFunctionChoice(functionName);

			if (includeTools)
				options.Tools.AddRange(_tools);

			return options;
		}


	}
}
