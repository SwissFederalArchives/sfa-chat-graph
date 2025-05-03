using MongoDB.Bson;

namespace SfaChatGraph.Server.Versioning.Migrations
{
	public class MigrationReportBuilder
	{
		private readonly DateTime _started = DateTime.UtcNow;
		private readonly List<MigrationError> _errors = new List<MigrationError>();
		private readonly List<string> _migrated = new List<string>();
		private readonly int _fromVersion;
		private readonly int _toVersion;
		private readonly Type _serviceType;
		private bool _wasCancelled = false;

		private MigrationReportBuilder(Type serviceType, int from, int to)
		{
			_serviceType =serviceType;
			_fromVersion=from;
			_toVersion = to;
		}

		public static MigrationReportBuilder Create(Type serviceType, int from, int to) => new MigrationReportBuilder(serviceType, from, to);

		public void ReportError<T>(T id, string message) =>
			ReportError(id.ToString(), message);

		public void ReportError<T>(T id, Exception error) =>
			ReportError(id.ToString(), error);

		public void ReportError(string id, Exception error) =>
			ReportError(id, error.ToString());

		public void ReportError(string id, string message) =>
			_errors.Add(new MigrationError(id, message));

		public void ReportMigrated<T>(T id) =>
			_migrated.Add(id.ToString());

		public void ReportMigrated(string id) =>
			_migrated.Add(id);

		public MigrationReport Build() => new MigrationReport
		{
			ServiceType = _serviceType.FullName,
			FromVersion = _fromVersion,
			ToVersion = _toVersion,
			MigrationStarted = _started,
			MigrationEnded = DateTime.UtcNow,
			Errors = _errors.ToArray(),
			Migrated = _migrated.ToArray(),
			Success = _errors.Count == 0 && _wasCancelled == false,
			WasCancelled = _wasCancelled
		};

		public void ReportCancelled()
		{
			_wasCancelled = true;
		}
	}
}
