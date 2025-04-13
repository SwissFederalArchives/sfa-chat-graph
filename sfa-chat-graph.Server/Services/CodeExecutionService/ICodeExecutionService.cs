using AwosFramework.Generators.FunctionCalling;

namespace sfa_chat_graph.Server.Services.CodeExecutionService
{
	public interface ICodeExecutionService
	{
		public string Language { get; }

		[FunctionCall("execute_code")]
		public Task<CodeExecutionResult> ExecuteCodeAsync(string code, CodeExecutionData[] data, CancellationToken cancellationToken);
	}
}
