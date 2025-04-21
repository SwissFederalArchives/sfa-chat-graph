using System.Runtime.CompilerServices;

namespace sfa_chat_graph.Server.Versioning
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddVersioning(this IServiceCollection collection, Action<VersionServiceOptions> configure = null)
		{
			configure ??= options => { };
			collection.Configure<VersionServiceOptions>(configure);
			collection.AddSingleton<IHostedService, VersioningService>();
			return collection;
		}
	}
}
