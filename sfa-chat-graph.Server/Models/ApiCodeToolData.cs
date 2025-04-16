using MessagePack;

namespace sfa_chat_graph.Server.Models
{
	[MessagePackObject]
	public class ApiCodeToolData
	{
		[Key(0)]
		public ApiToolData[] Data { get; set; }

		[Key(1)]
		public string Error { get; set; }

		[Key(2)]
		public string Code { get; set; }

		[Key(3)]
		public string Language { get; set; }

		[Key(4)]
		public bool Success { get; set; }
	}
}
