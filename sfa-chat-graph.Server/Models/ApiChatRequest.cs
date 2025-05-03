using sfa_chat_graph.Server.Config;

namespace sfa_chat_graph.Server.Models
{
	public class ApiChatRequest
	{
		public ApiMessage Message { get; set; }
		public int MaxErrors { get; set; }
		public int Temperature { get; set; }
		public AiConfig AiConfig { get; set; }
	}
}
