using System.Collections.Frozen;

namespace sfa_chat_graph.Server.Versioning
{
	public class VersionServiceOptions
	{
		public bool MigrateOnStart { get; set; } = true;
		public bool IgnoreFailedItems { get; set; } = false;
		public bool DeleteOldData { get; set; } = false;
		public FrozenSet<string> MigrationBlacklist { get; set; } = FrozenSet.Create<string>();
	}
}
