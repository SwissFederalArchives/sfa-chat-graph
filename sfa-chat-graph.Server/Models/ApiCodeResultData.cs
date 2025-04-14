namespace sfa_chat_graph.Server.Models
{
	public class ApiCodeResultData
	{
		public Guid Id { get; set; }
		public bool IsBase64Content { get; set; }
		public string Description { get; set; }
		public string MimeType { get; set; }
		public string Content { get; set; }
	}
}
