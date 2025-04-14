using AwosFramework.Generators.FunctionCalling;
using System.ComponentModel;

namespace sfa_chat_graph.Server.Services.CodeExecutionService
{
	public interface ICodeExecutionService
	{
		public string Language { get; }

		public Task<CodeExecutionResult> ExecuteCodeAsync(string code, CodeExecutionData[] data, CancellationToken cancellationToken);
	}
}
