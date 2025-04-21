namespace sfa_chat_graph.Server.Versioning
{

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class ServiceVersionAttribute : Attribute
	{
		public int Version { get; init; }
		public Type ServiceType { get; init; }
		public ServiceLifetime LifeTime { get; init; } = ServiceLifetime.Singleton;

		public ServiceVersionAttribute(Type serviceType, int version)
		{
			Version=version;
			ServiceType=serviceType;
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class ServiceVersionAttribute<T> : ServiceVersionAttribute
	{
		public ServiceVersionAttribute(int version) : base(typeof(T), version)
		{
		}
	}
}
