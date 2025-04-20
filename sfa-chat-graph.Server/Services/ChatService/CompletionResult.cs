using OpenAI.Chat;
using sfa_chat_graph.Server.Models;

namespace sfa_chat_graph.Server.Services.ChatService
{
	public record CompletionResult(ApiMessage[] Messages, string Error, bool Success);
}
