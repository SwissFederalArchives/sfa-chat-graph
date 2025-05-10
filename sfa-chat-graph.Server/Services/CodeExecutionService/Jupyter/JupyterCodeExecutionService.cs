using AwosFramework.ApiClients.Jupyter.Client;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Contents;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SfaChatGraph.Server.Services.ChatHistoryService;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.EventService;
using SfaChatGraph.Server.Utils;
using System.Reactive.Linq;

namespace SfaChatGraph.Server.Services.CodeExecutionService.Jupyter
{
	public class JupyterCodeExecutionService : IDisposable, ICodeExecutionService
	{
		private readonly AsyncLazy<JupyterClient> _jupyterClient;
		private readonly JupyterCodeExecutionServiceOptions _options;
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger<JupyterCodeExecutionService> _logger;
		private KernelSpecModel _kernelSpec;

		private async Task<JupyterClient> InitClientAsync()
		{
			var client = new JupyterClient(_options.Endpoint, _options.Token, _loggerFactory);
			var kernels = await client.RestClient.GetKernelSpecsAsync();
			_kernelSpec = kernels.KernelSpecs[_options.Kernel ?? kernels.Default];
			ArgumentNullException.ThrowIfNull(_kernelSpec, nameof(_kernelSpec));
			_logger.LogInformation("Jupyter client initialized with kernel: {Kernel}", _kernelSpec.Name);
			if (string.IsNullOrEmpty(_options.SetupScript) == false)
			{
				using var terminal = await client.CreateTerminalSessionAsync();
				await terminal.SendAsync(_options.SetupScript);
				try
				{
					await terminal.ObservableMessages.Timeout(TimeSpan.FromSeconds(15))
						.Catch(Observable.Empty<TerminalMessage>())
						.LastOrDefaultAsync();
				}
				catch (TimeoutException)
				{

				}
			}

			return client;
		}

		public JupyterCodeExecutionService(IOptions<JupyterCodeExecutionServiceOptions> options, ILoggerFactory loggerFactory)
		{
			_jupyterClient = new AsyncLazy<JupyterClient>(InitClientAsync);
			_options = options.Value;
			_loggerFactory = loggerFactory;
			_logger = loggerFactory.CreateLogger<JupyterCodeExecutionService>();
		}

		public string Language => _kernelSpec.Spec.Language;

		public void Dispose()
		{

			_jupyterClient?.Dispose();
		}

		private PutContentRequest AsContentRequest(CodeExecutionData data) =>
			data.IsBinary ?
			PutContentRequest.CreateBinary(data.Data, data.Name) :
			PutContentRequest.CreateText(data.Data, data.Name);

		private CodeExecutionFragment AsFragment(DisplayDataMessage message)
		{
			var description = (string)message.Data.GetValueOrDefault("text/plain", null);
			message.Data.Remove("text/plain");
			return new CodeExecutionFragment
			{
				Id = Guid.NewGuid(),
				Description = description,
				BinaryData = message.Data,
				BinaryIDs = message.Data.Keys.ToDictionary(x => x, x => Guid.NewGuid())
			};
		}

		public async Task<CodeExecutionResult> ExecuteCodeAsync(string code, CodeExecutionData[] data, CancellationToken cancellationToken, Func<string, Task>? statusAsync = null)
		{
			await statusAsync?.Invoke("Starting jupyter client");
			var client = await _jupyterClient.ValueAsync();
			await statusAsync?.Invoke("Creating jupyter session");
			using var session = await client.CreateKernelSessionAsync(opts =>
			{
				opts.KernelSpecName = _kernelSpec.Name;
				opts.CreateWorkingDirectory = data.Length > 0;
				opts.DeleteWorkingDirectoryOnDispose = true;
			});

			if (data.Length > 0)
			{
				await statusAsync?.Invoke("Uploading data");
				var tasks = data.Select(x => session.UploadFileAsync(AsContentRequest(x)));
				await Task.WhenAll(tasks);
			}

			await statusAsync?.Invoke("Executing code");
			var result = await session.ExecuteCodeAsync(code, cancellationToken);
			var reply = result.Reply;
			if (reply.Status == StatusType.Error)
			{
				var error = $"{reply.ExceptionName}: {reply.ExceptionValue}\n\nStacktrace:\n{string.Join("\n", reply.StackTrace.Select(AnsiCleaner.CleanAnsiString))}";
				return new CodeExecutionResult { Success = false, Language = _kernelSpec.Spec.Language, Fragments = null, Error = error };
			}
			else
			{
				var fragments = result.Results.Select(AsFragment).ToArray();
				return new CodeExecutionResult
				{
					Success = true,
					Fragments = fragments,
					Error = null,
					Language = _kernelSpec.Spec.Language
				};
			}
		}
	}
}
