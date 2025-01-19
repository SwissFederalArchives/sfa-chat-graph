using System;
using System.Collections.Generic;
using System.Text;

namespace AwosFramework.Generators.FunctionCalling
{
	public static class Constants
	{

		public const string NameSpace = "AwosFramework.Generators.FunctionCalling";
		public const string ModelNameSpace = $"{NameSpace}.Models";
		public const string ModelClassNameFormat = "{0}FunctionCallModel";
		public const string RegistryClassName = "FunctionCallRegistry";
		public const string RegistryHandlerDictFieldName = "_handlerDict";
		public const string RegistryResolverFieldName = "_resolver";


		public const string SchemaFieldName = "Schema";
		public const string ResolveAndHandleFunctionName = "ResolveAndHandleAsync";


		public const string ServiceProviderParentResolverName = "ServiceProviderParentResolver";
		public const string ServiceProviderParentResolverFullName = $"{NameSpace}.{ServiceProviderParentResolverName}";
		public const string ServiceProviderParentResolver = $$"""
		using System;

		namespace {{NameSpace}}
		{
			public class {{ServiceProviderParentResolverName}} : {{ParentResolverInterfaceName}}
			{
				private readonly IServiceProvider _serviceProvider;
				public {{ServiceProviderParentResolverName}}(IServiceProvider serviceProvider)
				{
					_serviceProvider = serviceProvider;
				}

				public T ResolveParent<T>()
				{
					return (T)_serviceProvider.GetService(typeof(T));
				}
			}
		}

		""";

		public const string ExtensionsClassName = "Extensions";
		public const string ExtensionsClassFullName = $"{NameSpace}.{ExtensionsClassName}";
		public const string ExtensionsClass = $$"""
		using System;

		namespace {{NameSpace}}
		{
			public static class {{ExtensionsClassName}}
			{
				public static {{ParentResolverInterfaceName}} As{{ParentResolverInterfaceName}}(this IServiceProvider serviceProvider)
				{
					return new {{ServiceProviderParentResolverName}}(serviceProvider);
				}
			}
		}
		""";

		public const string FunctionCallMetadataInterfaceName = "IFunctionCallMetadata";
		public const string FunctionCallMetadataInterfaceFullName = $"{NameSpace}.{FunctionCallMetadataInterfaceName}";
		public const string FunctionCallMetadataInterface = $$"""
		using System;
		using Json.Schema;

		namespace {{NameSpace}}
		{
			public interface {{FunctionCallMetadataInterfaceName}}
			{
				string Id { get; }
				string Description { get; }
				JsonSchema Schema { get; }
			}
		}
		""";

		public const string FunctionCallMetadataName = "FunctionCallMetadata";
		public const string FunctionCallMetadataFullName = $"{NameSpace}.{FunctionCallMetadataName}";
		public const string FunctionCallMetadata = $$"""
		using System;
		using System.Text.Json;
		using Json.Schema;

		namespace {{NameSpace}}
		{
			internal record {{FunctionCallMetadataName}}(string Id, string Description, JsonSchema Schema, Func<JsonDocument, {{ParentResolverInterfaceName}}, Task<object>> Handler) : {{FunctionCallMetadataInterfaceName}}
			{
			}
		}
		""";




		public const string MarkerAttributeName = "FunctionCallAttribute";
		public const string MarkerAttributeFullName = $"{NameSpace}.{MarkerAttributeName}";
		public const string MarkerAttribute = $$"""
		namespace {{NameSpace}}
		{
			[System.AttributeUsage(AttributeTargets.Method)]
			public class {{MarkerAttributeName}} : System.Attribute
			{
					public string FunctionCallId { get; }

					public FunctionCallAttribute(string id)
					{
						this.FunctionCallId = id;
					}
			}
		}
		""";

		public const string CallableInterfaceName = "ICallableFunction";
		public const string CallableInterfaceFullName = $"{NameSpace}.{CallableInterfaceName}";
		public const string CallableInterface = $$"""
		namespace {{NameSpace}} 
		{
			public interface {{CallableInterfaceName}}
			{
				public Task<object> InvokeAsync();
			}


		}
		""";

		public const string ParentResolverInterfaceName = "IParentResolver";
		public const string ParentResolverInterfaceFullName = $"{NameSpace}.{ParentResolverInterfaceName}";
		public const string ParentResolverInterface = $$"""
		namespace {{NameSpace}} 
		{
			public interface {{ParentResolverInterfaceName}}
			{
				public T ResolveParent<T>();
			}
		}
		""";

	}
}
