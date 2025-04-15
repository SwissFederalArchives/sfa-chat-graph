namespace sfa_chat_graph.Server.Models
{
	public class ApiCodeToolData
	{
		public ApiToolData[] Data { get; set; }
		public string Error { get; set; }
		public string Code { get; set; }
		public string Language { get; set; }
		public bool Success { get; set; }
	}
}
