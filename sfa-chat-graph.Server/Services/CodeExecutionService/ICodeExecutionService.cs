using AwosFramework.Generators.FunctionCalling;
using sfa_chat_graph.Server.Services.ChatHistoryService;
using sfa_chat_graph.Server.Services.ChatService.Events;
using sfa_chat_graph.Server.Services.EventService;
using System.ComponentModel;

namespace sfa_chat_graph.Server.Services.CodeExecutionService
{
	public interface ICodeExecutionService
	{
		public string Language { get; }

		public Task<CodeExecutionResult> ExecuteCodeAsync(string code, CodeExecutionData[] data, CancellationToken cancellationToken, Func<string, Task>? status = null);
	}
}
