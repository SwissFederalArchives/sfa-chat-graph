using SfaChatGraph.Server.Utils.ServiceCollection;

namespace SfaChatGraph.Server.Config
{
	public class AiConfig : IServiceConfig
	{
		public string Implementation { get; set; } = "OpenAI";
		public string Model { get; set; } = "gpt-4o";
		public string ApiKey { get; set; } = string.Empty;
		public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
	}
}
