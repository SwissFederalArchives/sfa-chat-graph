using System.Text.Json;

namespace sfa_chat_graph.Server.Utils
{
	public static class JsonExtensions
	{
		public static JsonTokenType ReadAndCheck(this ref Utf8JsonReader reader, params JsonTokenType[] types)
		{
			if(reader.Read() == false || types.Contains(reader.TokenType) == false)
				throw new JsonException($"Expected one of {string.Join(", ", types)} but got {reader.TokenType}");

			return reader.TokenType;
		}

		public static string ReadNamedObject(this ref Utf8JsonReader reader)
		{
			if(reader.TokenType != JsonTokenType.PropertyName)
				throw new JsonException($"Expected PropertyName but got {reader.TokenType}");

			var name = reader.GetString();
			reader.ReadAndCheck(JsonTokenType.StartObject);
			return name;
		}
	}
}
