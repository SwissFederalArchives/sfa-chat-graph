using sfa_chat_graph.Server.Models;

namespace SfaChatGraph.Server.Models
{
	public class ApiChatRequest
	{
		public Guid Id { get; set; }
		public int MaxErrors { get; set; }
		public int Temperature { get; set; }
	}
}
