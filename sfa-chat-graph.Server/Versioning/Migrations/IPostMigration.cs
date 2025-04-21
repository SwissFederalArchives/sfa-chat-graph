namespace sfa_chat_graph.Server.Versioning.Migrations
{
	public interface IPostMigration
	{
		public Task RunPostMigrationAsync(MigrationReport report, CancellationToken token);
	}
}
