using J2N.Text;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.RDF;
using System.Text;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;

namespace sfa_chat_graph.Server.Controllers
{
	[ApiController]
	[Route("/api/v1/rdf")]
	public class RdfController : ControllerBase
	{
		private readonly IGraphRag _graphDb;
		private readonly ChatClient _chatClient;
		private SystemChatMessage _schemaSysMessage;
		private SystemChatMessage _answerSysMessage;
		private Task _initTask;
		private ILogger _logger;

		public RdfController(IGraphRag graphDb, ChatClient chatClient, ILoggerFactory loggerFactory)
		{
			_graphDb=graphDb;
			_logger=loggerFactory.CreateLogger<RdfController>();
			_chatClient=chatClient;
			_initTask = _graphDb.InitAsync().ContinueWith(x =>
			{
				_schemaSysMessage=new SystemChatMessage($"You're an assistant generating SPARQL Code to answer the users questions. Only answer in code blocks like \n```sparql\nselect * where \n{{ ?s <http://example.com> ?o .\n}}\n```\nYou are allowed to use Prefixes. Remember that when using prefixes you can't have slashs after a prefixed value. Make sure to produce valid SPARQL syntax where Uri are formatted like <uri>\n If the schema doesn't cover the needed information reply with `<INCOMPATIBLE_SCHEMA>`. The current database has the following schema: \n\n{graphDb.Schema}");
			});

			_answerSysMessage=new SystemChatMessage("Your job is to answer the user's question given the result of a knowledgegraph lookup. If the question can't be answered with the given data clearly state so");
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
