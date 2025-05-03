namespace SfaChatGraph.Server.Services.Cache
{
	public class AppendableCacheOptions
	{
		public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
	}
}
