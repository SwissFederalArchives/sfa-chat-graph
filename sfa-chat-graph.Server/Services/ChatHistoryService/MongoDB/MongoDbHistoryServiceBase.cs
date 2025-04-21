using sfa_chat_graph.Server.Versioning.Migrations;
using System.Collections.Frozen;
using VDS.RDF.Query.Algebra;

namespace sfa_chat_graph.Server.Services.ChatHistoryService.MongoDB
{
	public abstract class MongoDbHistoryServiceBase : IMigrateable<IMigrateableChatHistoryService>, IMigrateableChatHistoryService
	{
		public bool CanDelete => true;

		public abstract Task DeleteAsync();
		public abstract Task<ChatHistory> GetChatHistoryAsync(Guid id);
		public abstract Task StoreAsync(ChatHistory history);
		public abstract Task<IEnumerable<Guid>> GetChatHistoryIdsAsync();

		public async virtual Task MigrateToAsync(IMigrateableChatHistoryService target, FrozenSet<string> alreadyMigrated, MigrationReportBuilder report, CancellationToken token)
		{
			var ids = await this.GetChatHistoryIdsAsync();
			foreach (var id in ids.Where(x => alreadyMigrated.Contains(x.ToString()) == false))
			{
				if(token.IsCancellationRequested)
				{
					report.ReportCancelled();
					return;
				}

				try
				{
					var history = await this.GetChatHistoryAsync(id);
					await target.StoreAsync(history);
					report.ReportMigrated(id);
				}
				catch (Exception ex)
				{
					report.ReportError(id, ex);
				}
			}
		}

	}
}
