namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public interface IChatHistoryServiceCache : IChatHistoryService
	{
		public Task CacheHistoryAsync(ChatHistory history);
	}
}
