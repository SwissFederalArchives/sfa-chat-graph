using System.Diagnostics.CodeAnalysis;

namespace SfaChatGraph.Server.Services.CodeExecutionService
{
	public class CodeExecutionResult
	{
		[MemberNotNullWhen(true, nameof(Fragments))]
		[MemberNotNullWhen(false, nameof(Error))]	
		public bool Success { get; set; }
		public string Language { get; set; }
		public string? Error { get; set; }
		public CodeExecutionFragment[] Fragments { get; set; }
	}
}
