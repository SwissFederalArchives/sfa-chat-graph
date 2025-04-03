using AwosFramework.Generators.FunctionCalling;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.RDF.Models;
using sfa_chat_graph.Server.Services.ChatService;
using sfa_chat_graph.Server.Utils;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Models;
using SfaChatGraph.Server.Utils;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Writing.Formatting;

namespace SfaChatGraph.Server.Controllers
{
	[ApiController]
	[Route("/api/v1/rdf")]
	public class RdfController : ControllerBase
	{
		private readonly IChatService _chatService;
		private readonly IGraphRag _graphDb;
		private ILogger _logger;

		public RdfController(IGraphRag graphDb, IChatService chatService, ILoggerFactory loggerFactory)
		{
			_graphDb=graphDb;
			_logger=loggerFactory.CreateLogger<RdfController>();
			_chatService = chatService;
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
			var response = await _chatService.CompleteAsync(chat.History, chat.Temperature, chat.MaxErrors);
			if (response.Success == false)
				return StatusCode(500, "Max errors exceeded");

			return Ok(response.Messages);
		}
	}
}
