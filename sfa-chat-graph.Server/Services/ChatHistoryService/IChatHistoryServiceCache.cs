namespace SfaChatGraph.Server.Services.ChatHistoryService
{
	public interface IChatHistoryServiceCache : IChatHistoryService
	{
		public Task CacheHistoryAsync(ChatHistory history);
	}
}
