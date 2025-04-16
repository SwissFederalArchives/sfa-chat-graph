using AwosFramework.Generators.FunctionCalling;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatService
{
	public interface IChatService
	{
		public Task<CompletionResult> CompleteAsync(IEnumerable<ApiMessage> message, float temperature, int maxErrors);
	}
}
