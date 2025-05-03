using Microsoft.AspNetCore.Mvc;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.EventService.WebSockets;
using System.Net.WebSockets;

namespace SfaChatGraph.Server.Controllers
{
	[ApiController]
	[Route("/api/v1/events")]
	public class EventController : ControllerBase
	{
		private readonly ChatServiceEventService _eventService;
		private readonly ILoggerFactory _loggerFactory;

		public EventController(ChatServiceEventService eventService, ILoggerFactory loggerFactory)
		{
			_eventService=eventService;
			_loggerFactory=loggerFactory;
		}

		[HttpGet("subscribe/{channelId}")]
		public async Task SubscribeEventsAsync(Guid channelId)
		{
			if (HttpContext.WebSockets.IsWebSocketRequest == false)
			{
				HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
				await HttpContext.Response.WriteAsync("Expected websocket request");
				return;
			}

			var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
			var target = new WebsocketTarget(webSocket, _loggerFactory, WebSocketMessageType.Text);
			_eventService.RegisterTarget(channelId, target);
			await target.WaitForCloseAsync(HttpContext.RequestAborted);
			await target.DisposeAsync();
		}
	}
}
