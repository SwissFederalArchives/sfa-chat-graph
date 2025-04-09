using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Base
{
	public interface IWebsocketOptions
	{
		public string? Token { get; }

		[MemberNotNullWhen(true, nameof(MaxReconnectTries))]
		public bool TryReconnect => MaxReconnectTries.HasValue;
		public ArrayPool<byte> ArrayPool { get;  }
		public int? MaxReconnectTries { get; }
		public TimeSpan ReconnectDelay { get; }
		public ILoggerFactory? LoggerFactory { get; }
		public Uri GetConnectionUri();

		public bool HasToken([NotNullWhen(true)] out string? token)
		{
			token = Token;
			return token != null;
		}

	}
}
