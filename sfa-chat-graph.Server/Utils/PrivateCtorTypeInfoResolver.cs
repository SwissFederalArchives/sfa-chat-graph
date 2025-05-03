using OpenAI.Chat;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SfaChatGraph.Server.Utils
{
	public class PrivateCtorTypeInfoResolver : DefaultJsonTypeInfoResolver
	{
		public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
		{
			var typeInfo = base.GetTypeInfo(type, options);
			if (typeInfo.Kind == JsonTypeInfoKind.Object && typeInfo.CreateObject is null && (typeInfo.Type.IsGenericType == false || (typeInfo.Type.IsGenericType && typeInfo.Type.GetGenericTypeDefinition() != typeof(Nullable<>))))
			{
				typeInfo.CreateObject = () =>
				{
					if (typeInfo.Type == typeof(AssistantChatMessage))
						return AssistantChatMessage.CreateAssistantMessage("");

					var nonPublicCtor = type.GetConstructor(BindingFlags.NonPublic, Type.EmptyTypes);
					if (nonPublicCtor != null)
						return nonPublicCtor.Invoke(null);
					else
						return Activator.CreateInstance(type);
				};
			}

			return typeInfo;
		}


		public static T Deserialize<T>(JsonDocument document)
		{
			var options = new JsonSerializerOptions
			{	
				TypeInfoResolver = new PrivateCtorTypeInfoResolver()
			};

			return JsonSerializer.Deserialize<T>(document, options);
		}

	}
}
