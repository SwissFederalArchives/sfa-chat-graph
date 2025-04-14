using AwosFramework.ApiClients.Jupyter.Client.Jupyter;
using AwosFramework.ApiClients.Jupyter.Client.Terminal;
using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Terminal;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AwosFramework.ApiClients.Jupyter.Client
{
	public class JupyterClient : IDisposable
	{
		private readonly IJupyterRestClient _restClient;
		private readonly List<KernelSessionClient> _kernelSessions = new();
		private readonly List<TerminalSessionClient> _terminalSessions = new();

		private readonly JupyterWebsocketOptions _defaultJupyterWebsocketOptions;
		private readonly TerminalWebsocketClientOptions _defaultTerminalWebsocketOptions;
		private readonly ILogger? _logger;
		private readonly CookieContainer _cookieContainer = new();

		public IJupyterRestClient RestClient => _restClient;

		public JupyterClient(string endpoint, string? token, ILoggerFactory? loggerFactory) : this(new Uri(endpoint), token, loggerFactory)
		{
		}

		public JupyterClient(Uri endpoint, string? token, ILoggerFactory? loggerFactory)
		{
			_restClient = JupyterRestClient.GetRestClient(_cookieContainer, endpoint, token);
			_defaultJupyterWebsocketOptions = new JupyterWebsocketOptions(endpoint, Guid.Empty) { Token = token, LoggerFactory = loggerFactory };
			_defaultTerminalWebsocketOptions = new TerminalWebsocketClientOptions(endpoint, string.Empty) { Token = token, LoggerFactory = loggerFactory };
			_logger = loggerFactory?.CreateLogger<JupyterClient>();
		}

		public void RemoveDisposedSessions()
		{
			var terminalCount = _terminalSessions.RemoveAll(x => x.IsDisposed);
			var kernelCount = _kernelSessions.RemoveAll(x => x.IsDisposed);
			_logger?.LogDebug("Removed disposed sessions [Kernel: {KernelCount}, Terminal: {TerminalCount}]", kernelCount, terminalCount);
		}

		public async Task<TerminalSessionClient> CreateTerminalSessionAsync(TerminalSessionClientOptions options)
		{
			TerminalModel terminal;
			if (string.IsNullOrEmpty(options.TerminalId))
			{
				terminal = await _restClient.OpenTerminalAsync();
			}
			else
			{
				terminal = await _restClient.GetTerminalAsync(options.TerminalId);
				ArgumentNullException.ThrowIfNull(terminal, nameof(terminal));
			}

			if(options.CreateWorkingDirectory)
				await _restClient.CreateDirectoriesAsync(options.StoragePath);

			var client = new TerminalSessionClient(terminal, options, _restClient, _cookieContainer);
			await client.InitializeAsync();
			return client;
		}

		public Task<TerminalSessionClient> CreateTerminalSessionAsync(Action<TerminalSessionClientOptions>? configure = null)
		{
			var options = new TerminalSessionClientOptions() { DefaultWebsocketOptions = _defaultTerminalWebsocketOptions };
			configure?.Invoke(options);
			return CreateTerminalSessionAsync(options);
		}

		public async Task<KernelSessionClient> CreateKernelSessionAsync(KernelSessionClientOptions options)
		{
			KernelIdentification kernelId = new KernelIdentification { Id = options.KernelId, SpecName = options.KernelSpecName };
			if (string.IsNullOrEmpty(options.KernelSpecName) && options.KernelId.HasValue == false)
			{
				var kernelSpecs = await _restClient.GetKernelSpecsAsync();
				kernelId.SpecName = kernelSpecs.Default;
			}

			if (options.CreateWorkingDirectory)
				await _restClient.CreateDirectoriesAsync(options.StoragePath);

			var createSession = StartSessionRequest.CreateConsole(kernelId, options.StoragePath ?? string.Empty);
			var session = await _restClient.StartSessionAsync(createSession);
			var sessionClient = new KernelSessionClient(session, options, _restClient, _cookieContainer);
			_kernelSessions.Add(sessionClient);
			await sessionClient.InitializeAsync();
			_logger?.LogInformation("Started kernel session {SessionId} with kernel {KernelName}[{KernelId}]", session.Id, session.Kernel.SpecName, session.Kernel.Id);
			return sessionClient;
		}

		public Task<KernelSessionClient> CreateKernelSessionAsync(Action<KernelSessionClientOptions>? configure = null)
		{
			var options = new KernelSessionClientOptions() { DefaultWebsocketOptions = _defaultJupyterWebsocketOptions };
			configure?.Invoke(options);
			return CreateKernelSessionAsync(options);
		}

		public void Dispose()
		{
			foreach (var session in _kernelSessions)
				session.Dispose();

			foreach (var session in _terminalSessions)
				session.Dispose();
		}
	}
}
