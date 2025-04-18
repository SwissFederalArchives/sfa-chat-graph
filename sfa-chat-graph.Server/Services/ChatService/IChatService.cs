using AwosFramework.Generators.FunctionCalling;
using Microsoft.EntityFrameworkCore.Query.Internal;
using OpenAI.Chat;
using sfa_chat_graph.Server.Models;
using sfa_chat_graph.Server.Services.ChatService.Events;
using sfa_chat_graph.Server.Services.EventService;

namespace sfa_chat_graph.Server.Services.ChatService
{
	public abstract class ChatServiceBase<TContext> : IChatService where TContext : ChatContext
	{
		public abstract Task<CompletionResult> CompleteAsync(TContext context, float temperature, int maxErrors);
		public abstract TContext CreateContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history);

		public async Task<CompletionResult> CompleteAsync(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history, float temperature, int maxErrors)
		{
			var context = CreateContext(chatId, events, history);
			var result = await CompleteAsync(context, temperature, maxErrors);
			return result;
		}
	}

	public interface IChatService
	{
		Task<CompletionResult> CompleteAsync(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history, float temperature, int maxErrors);
	}
}
