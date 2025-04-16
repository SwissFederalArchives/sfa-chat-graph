using sfa_chat_graph.Server.Models;

namespace SfaChatGraph.Server.Models
{
	public class ApiChatRequest
	{
		public ApiMessage Message { get; set; }
		public int MaxErrors { get; set; }
		public int Temperature { get; set; }
	}
}
