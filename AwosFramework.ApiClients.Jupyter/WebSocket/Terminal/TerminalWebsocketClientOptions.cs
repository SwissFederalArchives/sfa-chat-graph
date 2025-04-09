using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Terminal
{
	public record TerminalWebsocketClientOptions : IWebsocketOptions
	{
		public required Uri Endpoint { get; init; }
		public string? Token { get; init; }
		public required string TerminalId { get; set; }
		public int? MaxReconnectTries { get; init; } = 3;
		public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(15);
		public ILoggerFactory? LoggerFactory { get; init; } = null;
		public ArrayPool<byte> ArrayPool { get; init; } = ArrayPool<byte>.Shared;

		[SetsRequiredMembers]
		public TerminalWebsocketClientOptions(Uri endpoint, string terminalId, string? token = null)
		{
			if (endpoint.Scheme.StartsWith("http"))
			{
				var builder = new UriBuilder(endpoint);
				builder.Scheme = endpoint.Scheme.EndsWith("s") ? "wss" : "ws";
				endpoint = builder.Uri;
			}

			Endpoint = endpoint.OfComponents(UriComponents.SchemeAndServer);
			TerminalId = terminalId;
			Token = token;
		}

		public Uri GetConnectionUri()
		{
			var uri = new UriBuilder(Endpoint);
			uri.Path = uri.Path.TrimEnd('/') + $"terminals/websocket/{TerminalId}";
			return uri.Uri;
		}
	}
}
