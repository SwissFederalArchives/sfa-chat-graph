using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Terminal;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Client.Terminal
{
	public class TerminalSessionClient : IAsyncDisposable, IDisposable
	{
		private readonly TerminalModel _apiTerminal;
		private readonly TerminalWebsocketClient _terminalClient;
		private readonly TerminalSessionClientOptions _options;
		private readonly IJupyterRestClient _restClient;



		public string TerminalId => _apiTerminal.Name;
		public bool IsDisposed => _terminalClient.IsDisposed;

		public IObservable<TerminalMessage> ObservableMessages => _terminalClient.ObservableMessages;
		public Stream StdOut => _terminalClient.StdOut;
		public async Task SendAsync(string message) => await _terminalClient.SendAsync(message);

		public TerminalSessionClient(TerminalModel apiTerminal, TerminalSessionClientOptions options, IJupyterRestClient restClient)
		{
			_apiTerminal = apiTerminal;
			_options = options;
			var terminalOpts = options.DefaultWebsocketOptions with { TerminalId = apiTerminal.Name };
			_terminalClient = new TerminalWebsocketClient(terminalOpts);
			_restClient=restClient;
		}

		internal async Task InitializeAsync()
		{
			await _terminalClient.ConnectAsync();
			if (_options.CreateWorkingDirectory)
				await SendAsync($"cd {_options.StoragePath}");
		}

		public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

		public async ValueTask DisposeAsync()
		{
			if (_options.CloseTerminalOnDispose)
			{
				await _terminalClient.SendAndWaitAsync("exit");
				await _restClient.CloseTerminalAsync(_apiTerminal.Name);
			}

			await _terminalClient.DisconnectAsync();
			_terminalClient.Dispose();

			if (_options.DeleteWorkingDirectoryOnDispose && _options.CreateWorkingDirectory)
			{
				var path = _options.StoragePath;
				if (path != null)
					await _restClient.DeleteRecursivelyAsync(path);
			}

		}
	}
}
