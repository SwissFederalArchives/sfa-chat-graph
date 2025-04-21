using System.Collections.Frozen;

namespace sfa_chat_graph.Server.Versioning.Migrations
{
	public interface IMigrateable
	{
		public bool CanDelete { get; }
		public Task DeleteAsync();
	}

	public interface IMigrateable<T> : IMigrateable
	{
		public Task MigrateToAsync(T target, FrozenSet<string> alreadyMigrated, MigrationReportBuilder report, CancellationToken token);
	}
}
