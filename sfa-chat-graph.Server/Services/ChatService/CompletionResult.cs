using OpenAI.Chat;
using SfaChatGraph.Server.Models;

namespace SfaChatGraph.Server.Services.ChatService
{
	public record CompletionResult(ApiMessage[] Messages, string Error, bool Success);
}
