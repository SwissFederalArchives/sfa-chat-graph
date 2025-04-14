using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Contents;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.WebSocket;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Client.Jupyter
{
	public class KernelSessionClient : IAsyncDisposable, IDisposable
	{
		private readonly SessionModel _apiSession;
		private readonly KernelSessionClientOptions _options;
		private readonly JupyterWebsocketClient _websocketClient;
		private readonly IJupyterRestClient _restClient;

		public JupyterWebsocketClient WebSocketClient => _websocketClient;
		public Guid SessionId => _apiSession.Id;
		public KernelModel Kernel => _apiSession.Kernel;
		public WebsocketState State => _websocketClient.State;
		public bool IsDisposed { get; private set; } = false;

		private void ThrowIfDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(KernelSessionClient));
		}

		public KernelSessionClient(SessionModel apiSession, KernelSessionClientOptions options, IJupyterRestClient restClient, CookieContainer cookies)
		{
			_apiSession = apiSession;
			_options = options;
			_restClient = restClient;
			var opts = options.DefaultWebsocketOptions with { SessionId = apiSession.Id, KernelId = apiSession.Kernel.Id!.Value };
			_websocketClient = new JupyterWebsocketClient(opts, cookies);
		}

		public async Task<CodeExecutionResult> ExecuteCodeAsync(string code, CancellationToken token = default)
		{
			var executeRequest = new ExecuteRequest { Code = code, StopOnError = true };
			var observable = await _websocketClient.SendAndObserveAsync(executeRequest);
			var items = await observable.ToAsyncEnumerable().Select(x => x.Content).ToArrayAsync(token);
			var result = items.OfType<ExecuteReply>().FirstOrDefault();
			var data = items.OfType<DisplayDataMessage>();
			return new CodeExecutionResult { Request = executeRequest, Reply = result, Results = data.ToArray() };
		}


		private void FileIOCheck()
		{
			if (_options.CreateWorkingDirectory == false)
				throw new InvalidOperationException("FileIO is disabled");
		}

		public async Task UploadFileAsync(PutContentRequest request)
		{
			FileIOCheck();
			ThrowIfDisposed();
			await _restClient.PutContentAsync($"{_options.StoragePath}/{request.Name}", request);
		}

		public async Task UploadFileAsync(string name, string data, Encoding? encoding = null)
		{
			encoding ??= Encoding.UTF8;
			await UploadFileAsync(name, encoding.GetBytes(data));
		}

		public async Task UploadFileAsync(string name, Stream data)
		{
			FileIOCheck();
			ThrowIfDisposed();
			var request = PutContentRequest.CreateBinary(data, name, _options.StoragePath);
			await _restClient.PutContentAsync($"{_options.StoragePath}/{name}", request);
		}

		public async Task UploadFileAsync(string name, byte[] data)
		{
			FileIOCheck();
			ThrowIfDisposed();
			var request = PutContentRequest.CreateBinary(data, name, _options.StoragePath);
			await _restClient.PutContentAsync($"{_options.StoragePath}/{name}", request);
		}

		internal async Task InitializeAsync()
		{
			ThrowIfDisposed();
			await _websocketClient.ConnectAsync();
		}

		private async Task TryKillKernelAsync()
		{
			try
			{
				if (_options.KillKernelOnDispose)
					await _restClient.ShutdownKernelAsync(_apiSession.Kernel.Id!.Value);
			}
			catch (ApiException)
			{

			}
		}


		public async ValueTask DisposeAsync()
		{
			if (IsDisposed == false)
			{
				await _websocketClient.DisconnectAsync();
				_websocketClient.Dispose();
				await _restClient.DeleteSessionAsync(_apiSession.Id);
				if (_options.CreateWorkingDirectory && _options.DeleteWorkingDirectoryOnDispose)
					await _restClient.DeleteRecursivelyAsync(_options.StoragePath);

				await TryKillKernelAsync();
				IsDisposed = true;
			}
		}

		public void Dispose()
		{
			DisposeAsync().GetAwaiter().GetResult();
		}
	}
}
