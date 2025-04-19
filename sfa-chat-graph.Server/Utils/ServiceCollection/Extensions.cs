using Microsoft.Extensions.Options;
using System.Reflection;
using ConfigEx = Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions;

namespace sfa_chat_graph.Server.Utils.ServiceCollection
{
	public static class Extensions
	{

		static readonly Type[] ConfigureSignature = [typeof(IServiceCollection), typeof(IConfiguration)];
		public static IServiceCollection AddFromConfig<TService>(this IServiceCollection collection, IConfiguration config)
		{
			var serviceType = typeof(TService);
			Type[] genericTypeArgs = null;
			if (serviceType.IsGenericType)
			{
				genericTypeArgs = serviceType.GetGenericArguments();
				serviceType = serviceType.GetGenericTypeDefinition();
			}

			if (ImplementationAttribute.Registry.TryGetValue(serviceType, out var implementations) == false)
				throw new InvalidOperationException($"No implementations found for {serviceType.Name}");
			
			var implementationKey = config.GetValue<string>("Implementation");
			var detail = implementations.First(x => x.Key.Equals(implementationKey, StringComparison.OrdinalIgnoreCase));

			var concreteType = detail.ConcreteType;
			if(concreteType.IsGenericType && genericTypeArgs != null)
				concreteType = concreteType.MakeGenericType(genericTypeArgs);
			
			if (detail.ConfigType != null)
			{
				var method = typeof(ConfigEx).GetMethod(nameof(ConfigEx.Configure), BindingFlags.Public | BindingFlags.Static, ConfigureSignature)
					.MakeGenericMethod(detail.ConfigType);
				method.Invoke(null, [collection, config]);
			}

			if (concreteType.IsAssignableTo(typeof(IHostedService)))
				collection.Add(new ServiceDescriptor(typeof(IHostedService), concreteType, detail.Lifetime));

			collection.Add(new ServiceDescriptor(typeof(TService), concreteType, detail.Lifetime));
			return collection;
		}
	}
}
