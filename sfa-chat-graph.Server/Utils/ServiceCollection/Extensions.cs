using Lucene.Net.Analysis.Standard;
using Microsoft.Extensions.Options;
using System.Reflection;
using ConfigEx = Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions;

namespace sfa_chat_graph.Server.Utils.ServiceCollection
{
	public static class Extensions
	{

		static readonly Type[] ConfigureSignature = [typeof(IServiceCollection), typeof(IConfiguration)];

		public static TService GetFromConfig<TService, TConfig>(this IServiceProvider provider, TConfig config) where TConfig : class, IServiceConfig
		{
			var serviceType = typeof(TService);
			Type[] genericTypeArgs = null;
			if (serviceType.IsGenericType)
			{
				genericTypeArgs = serviceType.GetGenericArguments();
				serviceType = serviceType.GetGenericTypeDefinition();
			}

			if (ServiceImplementationAttribute.Registry.TryGetValue(serviceType, out var implementations) == false)
				throw new InvalidOperationException($"No implementations found for {serviceType.Name}");

			var detail = implementations.First(x => x.Key.Equals(config.Implementation, StringComparison.OrdinalIgnoreCase));

			var concreteType = detail.ConcreteType;
			if (concreteType.IsGenericType && genericTypeArgs != null)
				concreteType = concreteType.MakeGenericType(genericTypeArgs);

			IOptions<TConfig> serviceOptions = Options.Create(config);
			return (TService)ActivatorUtilities.CreateInstance(provider, concreteType, serviceOptions);
		}

		public static TService GetFromConfig<TService>(this IServiceProvider provider, IConfiguration config)
		{
			var serviceType = typeof(TService);
			Type[] genericTypeArgs = null;
			if (serviceType.IsGenericType)
			{
				genericTypeArgs = serviceType.GetGenericArguments();
				serviceType = serviceType.GetGenericTypeDefinition();
			}

			if (ServiceImplementationAttribute.Registry.TryGetValue(serviceType, out var implementations) == false)
				throw new InvalidOperationException($"No implementations found for {serviceType.Name}");

			var implementationKey = config.GetValue<string>("Implementation");
			var detail = implementations.First(x => x.Key.Equals(implementationKey, StringComparison.OrdinalIgnoreCase));

			var concreteType = detail.ConcreteType;
			if (concreteType.IsGenericType && genericTypeArgs != null)
				concreteType = concreteType.MakeGenericType(genericTypeArgs);

			object serviceOptions = null;
			if (detail.ConfigType != null)
			{
				var configObject = ActivatorUtilities.CreateInstance(provider, detail.ConfigType);
				config.Bind(configObject);
				var method = typeof(Options).GetMethod(nameof(Options.Create), BindingFlags.Public | BindingFlags.Static)
					.MakeGenericMethod(detail.ConfigType);
				serviceOptions = method.Invoke(null, [configObject]);
			}	

			return (TService)ActivatorUtilities.CreateInstance(provider, concreteType, serviceOptions);
		}

		public static IServiceCollection AddFromConfig<TService>(this IServiceCollection collection, IConfiguration config)
		{
			var serviceType = typeof(TService);
			Type[] genericTypeArgs = null;
			if (serviceType.IsGenericType)
			{
				genericTypeArgs = serviceType.GetGenericArguments();
				serviceType = serviceType.GetGenericTypeDefinition();
			}

			if (ServiceImplementationAttribute.Registry.TryGetValue(serviceType, out var implementations) == false)
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
