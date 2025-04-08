using AwosFramework.ApiClients.Jupyter.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket
{
	public class JupyterWebsocketOptions
	{
		public required Uri Endpoint { get; init; }
		public Guid KernelId { get; init; }
		public Guid SessionId { get; init; }
		public string? Token { get; init; }
		public ArrayPool<byte> ArrayPool { get; init; } = ArrayPool<byte>.Shared;
		public int? MaxMessages { get; init; } = 1024;
		public int? MaxReconnectTries { get; init; } = 3;
		public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(15);
		public ILoggerFactory? LoggerFactory { get; init; } = null;

		[MemberNotNullWhen(true, nameof(MaxReconnectTries))]
		public bool TryReconnect => MaxReconnectTries.HasValue;


		public bool HasToken([NotNullWhen(true)]out string? token)
		{
			token = Token;
			return token != null;
		}

		[SetsRequiredMembers]
		public JupyterWebsocketOptions(string endpoint, Guid kernelId, Guid? sessionId = null, string? token = null) : this(new Uri(endpoint), kernelId, sessionId, token)
		{

		}


		[SetsRequiredMembers]
		public JupyterWebsocketOptions(Uri endpoint, Guid kernelId, Guid? sessionId = null, string? token = null)
		{
			Endpoint = endpoint.OfComponents(UriComponents.SchemeAndServer);
			KernelId = kernelId;
			SessionId = sessionId??Guid.NewGuid();
			Token = token;
		}

		public Uri GetConnectionUri()
		{
			var uri = new UriBuilder(Endpoint);
			uri.Path = uri.Path.TrimEnd('/') + $"/api/kernels/{KernelId}/channels";
			uri.Query = $"session_id={SessionId}";
			return uri.Uri;
		}
	}
}
