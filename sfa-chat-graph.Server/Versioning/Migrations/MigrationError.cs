namespace SfaChatGraph.Server.Versioning.Migrations
{
	public class MigrationError
	{
		public string ItemId { get; set; }
		public string Message { get; set; }

		public MigrationError(string itemId, string message)
		{
			ItemId = itemId;
			Message = message;
		}
	}
}
