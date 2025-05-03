namespace SfaChatGraph.Server.Services.CodeExecutionService
{
	public class CodeExecutionFragment
	{
		public Guid Id { get; init; }
		public string Description { get; init; }
		public Dictionary<string, string> BinaryData { get; init; }
		public Dictionary<string, Guid> BinaryIDs { get; init; }
	}
}
