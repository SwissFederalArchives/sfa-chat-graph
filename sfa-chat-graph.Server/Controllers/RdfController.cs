using AwosFramework.Generators.FunctionCalling;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.RDF.Models;
using sfa_chat_graph.Server.Services.ChatHistoryService;
using sfa_chat_graph.Server.Services.ChatService;
using sfa_chat_graph.Server.Services.ChatService.Events;
using sfa_chat_graph.Server.Services.EventService;
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
		private readonly IChatHistoryService _chatHistoryService;
		private readonly IChatService _chatService;
		private readonly IGraphRag _graphDb;
		private readonly ChatServiceEventService _eventService;
		private ILogger _logger;

		public RdfController(IGraphRag graphDb, IChatService chatService, ILoggerFactory loggerFactory, IChatHistoryService chatHistoryService, ChatServiceEventService eventService)
		{
			_graphDb=graphDb;
			_logger=loggerFactory.CreateLogger<RdfController>();
			_chatService = chatService;
			_chatHistoryService=chatHistoryService;
			_eventService=eventService;
		}

		[HttpGet("describe")]
		[ProducesResponseType<SparqlStarResult>(StatusCodes.Status200OK)]
		public async Task<IActionResult> DescribeAsync([FromQuery] string subject)
		{
			var graph = await _graphDb.DescribeAsync(subject);	
			return Ok(graph);
		}

		[HttpGet("history/{id}")]
		[ProducesResponseType<ApiMessage[]>(StatusCodes.Status200OK)]
		public async Task<IActionResult> GetHistoryAsync(Guid id, [FromQuery]bool loadBlobs = false)
		{
			var history = await _chatHistoryService.GetChatHistoryAsync(id, loadBlobs);
			return Ok(history.Messages);
		}

		[HttpGet("tool-data/{id}")]
		[ProducesResponseType<FileResult>(StatusCodes.Status200OK)]
		public async Task<IActionResult> GetToolDataAsync(Guid id)
		{
			if (_chatHistoryService.SupportsToolData == false)
				return NotFound();
			
			var data = await _chatHistoryService.GetToolDataAsync(id);
			if (data == null)
				return NotFound();

			return data;
		}

		[HttpPost("chat/{id}")]
		[ProducesResponseType<ApiMessage[]>(StatusCodes.Status200OK)]
		public async Task<IActionResult> ChatAsync([FromBody] ApiChatRequest chat, Guid id, [FromQuery]Guid? eventChannel)
		{
			IEventSink<ChatEvent> sink = null;
			if(eventChannel.HasValue)
				sink = _eventService.GetChannel(eventChannel.Value);

			await sink?.PushAsync(ChatEvent.CActivity(id, "Loading chat history"));
			var history = await _chatHistoryService.GetChatHistoryAsync(id);
			var messages = history.Messages.Append(chat.Message);
			var response = await _chatService.CompleteAsync(history.Id, sink, messages, chat.Temperature, chat.MaxErrors);
			if (response.Success == false)
				return StatusCode(500, response.Error);

			await _chatHistoryService.AppendAsync(id, response.Messages.Prepend(chat.Message));
			return Ok(response.Messages);
		}
	}
}
