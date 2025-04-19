namespace sfa_chat_graph.Server.Services.Cache
{
	public class AppendableCacheOptions
	{
		public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
	}
}
