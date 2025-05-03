using AwosFramework.Generators.FunctionCalling;
using SfaChatGraph.Server.Services.ChatHistoryService;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.EventService;
using System.ComponentModel;

namespace SfaChatGraph.Server.Services.CodeExecutionService
{
	public interface ICodeExecutionService
	{
		public string Language { get; }

		public Task<CodeExecutionResult> ExecuteCodeAsync(string code, CodeExecutionData[] data, CancellationToken cancellationToken, Func<string, Task>? status = null);
	}
}
