namespace sfa_chat_graph.Server.Services.ChatService
{
	public interface IChatActivity 
	{
		public Task NotifyActivityAsync(string status, string detail = null, string trace = null);
	}
}
