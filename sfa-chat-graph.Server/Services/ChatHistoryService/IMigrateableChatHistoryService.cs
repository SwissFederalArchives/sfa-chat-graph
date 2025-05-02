using sfa_chat_graph.Server.Versioning.Migrations;

namespace sfa_chat_graph.Server.Services.ChatHistoryService
{
	public interface IMigrateableChatHistoryService : IMigrateable<IMigrateableChatHistoryService>
	{
		public void ModifyMigrationSource(ChatHistory history);
		public Task<IEnumerable<Guid>> GetChatHistoryIdsAsync();
		public Task<ChatHistory> GetChatHistoryAsync(Guid id);
		public Task StoreAsync(ChatHistory history);
	}
}
