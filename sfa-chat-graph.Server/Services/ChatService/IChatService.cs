using AwosFramework.Generators.FunctionCalling;
using Microsoft.EntityFrameworkCore.Query.Internal;
using OpenAI.Chat;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.EventService;

namespace SfaChatGraph.Server.Services.ChatService
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

		ChatContext IChatService.CreateContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history) => CreateContext(chatId, events, history);

		public Task<CompletionResult> CompleteAsync(ChatContext ctx, float temperature, int maxErrors)
		{
			if(ctx is not TContext tCtx)
				throw new InvalidOperationException($"ChatContext is not of type {typeof(TContext).Name}");

			return CompleteAsync(tCtx, temperature, maxErrors);
		}
	}

	public interface IChatService
	{
		ChatContext CreateContext(Guid chatId, IEventSink<ChatEvent> events, IEnumerable<ApiMessage> history);
		Task<CompletionResult> CompleteAsync(ChatContext ctx, float temperature, int maxErrors);
	}
}
