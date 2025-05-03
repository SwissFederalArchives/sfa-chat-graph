using System.Runtime.CompilerServices;

namespace SfaChatGraph.Server.Versioning
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
