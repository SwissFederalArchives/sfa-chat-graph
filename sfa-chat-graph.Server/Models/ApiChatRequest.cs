using SfaChatGraph.Server.Config;

namespace SfaChatGraph.Server.Models
{
	public class ApiChatRequest
	{
		public ApiMessage Message { get; set; }
		public int MaxErrors { get; set; }
		public int Temperature { get; set; }
		public AiConfig AiConfig { get; set; }
	}
}
