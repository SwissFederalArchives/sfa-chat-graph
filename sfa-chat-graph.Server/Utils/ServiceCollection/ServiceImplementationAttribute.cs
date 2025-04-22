using System.Collections.Frozen;
using System.Reflection;

namespace sfa_chat_graph.Server.Utils.ServiceCollection
{
	public record ImplementationDetail(Type ServiceType, Type ConcreteType, Type? ConfigType, ServiceLifetime Lifetime, string? Key)
	{
		public static ImplementationDetail FromAttribute(ServiceImplementationAttribute attr, Type concreteType)
		{
			if (attr.ServiceType.IsGenericType == false && concreteType.IsAssignableTo(attr.ServiceType) == false)
				throw new InvalidOperationException($"Type {concreteType.Name} does not implement {attr.ServiceType.Name}");

			if (concreteType.IsAssignableTo(typeof(IHostedService)) && attr.Lifetime != ServiceLifetime.Singleton)
				throw new InvalidOperationException($"Type {concreteType.Name} is a hosted service and must be registered as singleton");

			return new(attr.ServiceType, concreteType, attr.ConfigType, attr.Lifetime, attr.Key ?? concreteType.Name);
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class ServiceImplementationAttribute : Attribute
	{
		public static readonly FrozenDictionary<Type, ImplementationDetail[]> Registry = typeof(ServiceImplementationAttribute)
			.Assembly.GetTypes()
			.SelectMany(t => t.GetCustomAttributes<ServiceImplementationAttribute>().Select(x => ImplementationDetail.FromAttribute(x, t)))
			.GroupBy(x => x.ServiceType).ToFrozenDictionary(x => x.Key, x => x.ToArray());

		public Type ServiceType { get; init; }
		public Type? ConfigType { get; init; }
		public ServiceLifetime Lifetime { get; init; }
		public string? Key { get; init; }

		public ServiceImplementationAttribute(Type serviceType, Type configType = null, ServiceLifetime lifetime = ServiceLifetime.Scoped)
		{
			ServiceType = serviceType;
			ConfigType = configType;
			Lifetime = lifetime;
		}

		public string GetKey(TypeInfo info) => this.Key ?? info.Name;
	}

	public class ServiceImplementationAttribute<TService, TConfig> : ServiceImplementationAttribute
	{
		public ServiceImplementationAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) : base(typeof(TService), typeof(TConfig), lifetime)
		{
		}
	}
}
