using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter;
using Microsoft.Extensions.Logging;

namespace AwosFramework.ApiClients.Jupyter.Client
{
	public class JupyterClient : IDisposable
	{
		private readonly IJupyterRestClient _restClient;
		private readonly Dictionary<Guid, ClientKernelSession> _sessions = new();
		private readonly JupyterWebsocketOptions _defaultWebsocketOptions;
		private readonly ILogger? _logger;

		public IJupyterRestClient RestClient => _restClient;

		public JupyterClient(string endpoint, string? token, ILoggerFactory? loggerFactory) : this(new Uri(endpoint), token, loggerFactory)
		{
		}

		public JupyterClient(Uri endpoint, string? token, ILoggerFactory? loggerFactory)
		{
			_restClient = JupyterRestClient.GetRestClient(endpoint, token);
			_defaultWebsocketOptions = new JupyterWebsocketOptions(endpoint, Guid.Empty) { Token = token, LoggerFactory = loggerFactory }; 
			_logger = loggerFactory?.CreateLogger<JupyterClient>();
		}

		public void RemoveDisposedKernels()
		{
			var disposed = _sessions.Where(x => x.Value.IsDisposed).Select(x => x.Key).ToArray();
			foreach (var id in disposed)
			{
				_sessions.Remove(id);
				_logger?.LogInformation("Removed disposed kernel session {SessionId}", id);
			}
		}

		public async Task<ClientKernelSession> StartKernelAsync(ClientKernelSessionOptions options)
		{
			KernelIdentification kernelId = new KernelIdentification { Id = options.KernelId, SpecName = options.KernelSpecName };
			if(string.IsNullOrEmpty(options.KernelSpecName) && options.KernelId.HasValue == false)
			{
				var kernelSpecs = await _restClient.GetKernelSpecsAsync();
				kernelId.SpecName = kernelSpecs.Default;
			}

			if (options.CreateWorkingDirectory)
				await _restClient.CreateDirectoriesAsync(options.StoragePath);
			
			var createSession = StartSessionRequest.CreateConsole(kernelId, options.StoragePath ?? string.Empty);
			var session = await _restClient.StartSessionAsync(createSession);
			var sessionClient = new ClientKernelSession(session, options, _restClient);
			_sessions.Add(session.Id, sessionClient);
			await sessionClient.InitializeAsync();
			_logger?.LogInformation("Started kernel session {SessionId} with kernel {KernelName}[{KernelId}]", session.Id, session.Kernel.SpecName, session.Kernel.Id);
			return sessionClient;
		}

		public Task<ClientKernelSession> StartKernelSessionAsync(Action<ClientKernelSessionOptions>? configure = null)
		{
			var options = new ClientKernelSessionOptions() { DefaultWebsocketOptions = _defaultWebsocketOptions };
			configure?.Invoke(options);
			return StartKernelAsync(options);
		}

		public void Dispose()
		{
			foreach (var session in _sessions.Values)
				session.Dispose();
		}
	}
}	
