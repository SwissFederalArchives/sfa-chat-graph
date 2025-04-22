
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using sfa_chat_graph.Server.Services.ChatHistoryService;
using sfa_chat_graph.Server.Utils.ServiceCollection;
using sfa_chat_graph.Server.Versioning.Migrations;
using SfaChatGraph.Server.Utils;
using System.Collections.Frozen;
using System.Reflection;
using VDS.RDF.Query.Algebra;

namespace sfa_chat_graph.Server.Versioning
{
	public class VersioningService : IHostedService
	{
		private record VersionData(int Version, Type ServiceType, Type ImplementationType, ServiceLifetime LifeTime)
		{
			public static VersionData FromType(Type type)
			{
				var attribute = type.GetCustomAttribute<ServiceVersionAttribute>();
				if (attribute == null) return null;
				return new VersionData(attribute.Version, attribute.ServiceType, type, attribute.LifeTime);
			}
		}

		private static readonly FrozenDictionary<Type, VersionData[]> VersionRegistry = FindVersions(typeof(VersioningService).Assembly);
		private static FrozenDictionary<Type, VersionData[]> FindVersions(Assembly assembly)
		{
			return assembly.GetTypes()
				.Select(VersionData.FromType)
				.Where(x => x != null)
				.GroupBy(x => x.ServiceType)
				.ToFrozenDictionary(x => x.Key, x => x.ToArray());
		}

		public static IEnumerable<Type> GetServiceTypes() => VersionRegistry.Keys;

		public static object GetLatestVersion(IServiceProvider serviceProvider, Type serviceType)
		{
			if (VersionRegistry.TryGetValue(serviceType, out var versions) == false)
				throw new InvalidOperationException($"No versions found for {serviceType.Name}");

			var latest = versions
				.OrderByDescending(x => x.Version)
				.FirstOrDefault();

			if (latest == null)
				throw new InvalidOperationException($"No versions found for {serviceType.Name}");

			return ActivatorUtilities.CreateInstance(serviceProvider, latest.ImplementationType);
		}

		public static T GetLatestVersion<T>(IServiceProvider serviceProvider)
		{
			if (VersionRegistry.TryGetValue(typeof(T), out var versions) == false)
				throw new InvalidOperationException($"No versions found for {typeof(T).Name}");

			var latest = versions
				.OrderByDescending(x => x.Version)
				.FirstOrDefault();

			if (latest == null)
				throw new InvalidOperationException($"No versions found for {typeof(T).Name}");

			if (latest.ImplementationType.IsAssignableTo(latest.ServiceType) == false)
				throw new InvalidOperationException($"Implementation type {latest.ImplementationType.Name} is not assignable to service type {latest.ServiceType.Name}");

			return (T)ActivatorUtilities.CreateInstance(serviceProvider, latest.ImplementationType);
		}


		private readonly IServiceProvider _serviceProvider;
		private readonly VersionServiceOptions _options;
		private readonly ILogger _logger;

		public VersioningService(IOptions<VersionServiceOptions> options, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		{
			_options=options.Value;
			_serviceProvider=serviceProvider;
			_logger = loggerFactory.CreateLogger<VersioningService>();
		}

		private async Task MigrateAsync(VersionData from, VersionData to, IMongoCollection<MigrationReport> reportsCollection, CancellationToken token)
		{
			if (from.ServiceType != to.ServiceType)
				throw new InvalidOperationException($"Cannot migrate from {from.ServiceType.Name} to {to.ServiceType.Name}");

			var reports = await reportsCollection
				.Find(x => x.ServiceType == from.ServiceType.FullName && x.FromVersion == from.Version)
				.ToListAsync();

			if (reports.Any(x => x.Success))
				return;

			var migratedIds = reports.SelectMany(x => x.Migrated);
			if (_options.IgnoreFailedItems)
				migratedIds = migratedIds.Concat(reports.SelectMany(x => x.Errors.Select(x => x.ItemId)));

			var frozenIds = migratedIds.ToFrozenSet();

			var fromTypes = from.ImplementationType.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IMigrateable<>))
				.Select(x => x.GetGenericArguments()[0])
				.ToArray();

			var toTypes = to.ImplementationType.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IMigrateable<>))
				.Select(x => x.GetGenericArguments()[0])
				.ToArray();

			var firstMatching = fromTypes
				.FirstOrDefault(x => toTypes.Any(y => y.IsAssignableFrom(x)));

			if (firstMatching == null)
				throw new InvalidOperationException($"No matching types found for migration from {from.ImplementationType.Name} to {to.ImplementationType.Name}");

			var migrateableType = typeof(IMigrateable<>).MakeGenericType(firstMatching);
			var migrationFunction = from.ImplementationType.GetMethod(nameof(IMigrateable<object>.MigrateToAsync), BindingFlags.Public | BindingFlags.Instance);

			using (var scope = _serviceProvider.CreateScope())
			{
				var fromMigrateable = ActivatorUtilities.CreateInstance(scope.ServiceProvider, from.ImplementationType);
				var toMigrateable = ActivatorUtilities.CreateInstance(scope.ServiceProvider, to.ImplementationType);
				var report = MigrationReportBuilder.Create(from.ServiceType, from.Version, to.Version);
				_logger.LogInformation("Migrating {ServiceName} from {FromVersion} to {ToVersion}", from.ServiceType.FullName, from.Version, to.Version);
				var migrationTask = (Task)migrationFunction.Invoke(fromMigrateable, [toMigrateable, frozenIds, report, token]);
				await migrationTask;

				var finished = report.Build();
				_logger.LogInformation("Finished migrating {ServiceName} from Version {FromVersion} to Version {ToVersion}, took {Seconds}s, {ErrorCount} error, {ConvertedCount} migrated", from.ServiceType.FullName, from.Version, to.Version, finished.Duration.TotalSeconds, finished.Errors.Length, finished.Migrated.Length);
				if (_options.DeleteOldData && finished.Success && fromMigrateable is IMigrateable migrateable)
				{
					_logger.LogInformation("Deleting old data for {ServiceName} Version {FromVersion}", from.ServiceType.FullName, from.Version);
					await migrateable.DeleteAsync();
				}

				await reportsCollection.InsertOneAsync(finished);
				if(fromMigrateable is IPostMigration fromPostMigration)
				{
					await fromPostMigration.RunPostMigrationAsync(finished, token);
					_logger.LogInformation("Running post-migration for {ServiceName} Version {FromVersion}", from.ServiceType.FullName, from.Version);
				}

				if(toMigrateable is IPostMigration toPostMigration)
				{
					await toPostMigration.RunPostMigrationAsync(finished, token);
					_logger.LogInformation("Running post-migration for {ServiceName} Version {ToVersion}", to.ServiceType.FullName, to.Version);
				}
			}
		}

		private async Task HandleMigrationAsync(Type serviceType, CancellationToken token, IMongoCollection<MigrationReport> reports)
		{
			var latest = VersionRegistry[serviceType].MaxBy(x => x.Version);
			_logger.LogInformation("Latest Version for {ServiceName} is {Version}", serviceType.FullName, latest);
			var migratedVersions = await reports.Aggregate()
				.Match(x => x.ServiceType == serviceType.FullName && x.Success)
				.Group(x => x.FromVersion, x => x.Key)
				.ToListAsync();

			var missingVersions = VersionRegistry[serviceType]
				.Where(x => x.Version < latest.Version && migratedVersions.Contains(x.Version) == false)
				.OrderBy(x => x.Version)
				.ToArray();

			_logger.LogInformation("Missing versions for {ServiceName} are {Versions}", serviceType.FullName, string.Join(", ", missingVersions.Select(x => x.Version)));
			foreach (var missing in missingVersions)
				await MigrateAsync(missing, latest, reports, token);
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (_options.MigrateOnStart)
			{
				_logger.LogInformation("Starting versioning service");
				using (var scope = _serviceProvider.CreateScope())
				{
					var reports = scope.ServiceProvider.GetRequiredService<IMongoDatabase>().GetCollection<MigrationReport>("migrations");
					var tasks = VersionRegistry.Keys.Select(HandleMigrationAsync, cancellationToken, reports);
					await Task.WhenAll(tasks);
				}
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
