using sfa_chat_graph.Server.Models;
using SfaChatGraph.Server.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sfa_chat_graph.Server.Utils
{
	public class ApiMessageConverter : JsonConverter<ApiMessage>
	{
		public override ApiMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var newOptions = new JsonSerializerOptions(options);
			newOptions.Converters.Remove(this); // prevent recursive calls to this converter

			Utf8JsonReader readerCopy = reader;
			while (readerCopy.Read())
			{
				if (readerCopy.TokenType == JsonTokenType.PropertyName && readerCopy.GetString().Equals("role", StringComparison.OrdinalIgnoreCase))
				{
					readerCopy.Read();
					ChatRole role = JsonSerializer.Deserialize<ChatRole>(ref readerCopy, newOptions);
					switch (role)
					{
						case ChatRole.User:
							return JsonSerializer.Deserialize<ApiMessage>(ref reader, newOptions);
						case ChatRole.ToolCall:
							return JsonSerializer.Deserialize<ApiToolCallMessage>(ref reader, newOptions);
						case ChatRole.ToolResponse:
							return JsonSerializer.Deserialize<ApiToolResponseMessage>(ref reader, newOptions);
						case ChatRole.Assistant:
							return JsonSerializer.Deserialize<ApiAssistantMessage>(ref reader, newOptions);
					}
				}
			}

			throw new JsonException("Unknown role");
		}

		public override void Write(Utf8JsonWriter writer, ApiMessage value, JsonSerializerOptions options)
		{
			JsonSerializer.Serialize(writer, value, value.GetType(), options);
		}
	}
}
