namespace SfaChatGraph.Server.Models
{
	public class ApiChatRequest
	{
		public ApiChatMessage[] History { get; set; }
		public int MaxErrors { get; set; }
		public int Temperature { get; set; }
	}
}
