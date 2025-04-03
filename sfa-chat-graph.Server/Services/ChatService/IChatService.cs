using AwosFramework.Generators.FunctionCalling;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatService
{
	public interface IChatService
	{
		public Task<CompletionResult> CompleteAsync(ApiMessage[] history, float temperature, int maxErrors);
	}
}
