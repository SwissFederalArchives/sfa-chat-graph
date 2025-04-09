using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Terminal
{
	public class TerminalWebsocketClient
	{
		private ClientWebSocket _socket;
		private CancellationTokenSource? _stopSocket;
		private readonly ILogger? _logger;


		public WebsocketState State { get; private set; } = WebsocketState.Disconnected;

		[MemberNotNullWhen(true, nameof(IOTask))]
		public bool IsConnected => State == WebsocketState.Connected;
		public bool IsDisconnected => State == WebsocketState.Disconnected;
		public bool IsDisposed => State == WebsocketState.Disposed || State == WebsocketState.Errored;

		public event Action<WebsocketState>? StateChanged;

		public Task? IOTask { get; private set; }
		public Exception? Exception { get; private set; }

		private void ThrowIfDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(JupyterWebsocketClient));
		}

		private void SetState(WebsocketState state)
		{
			ThrowIfDisposed();
			if (state != State)
			{
				State = state;
				StateChanged?.Invoke(state);
				_logger?.LogInformation("Websocket state changed to {State}", state);
			}
		}
	}
}
