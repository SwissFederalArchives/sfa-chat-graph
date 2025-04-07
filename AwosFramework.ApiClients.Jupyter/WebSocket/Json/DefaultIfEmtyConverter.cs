using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Json
{
	public class DefaultIfEmtyConverter<T> : JsonConverter<T> 
	{
		private readonly JsonSerializerOptions _options;

		public DefaultIfEmtyConverter(JsonSerializerOptions options)
		{
			_options = new JsonSerializerOptions(options);
		}

		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Utf8JsonReader copy = reader;
			if(copy.TokenType == JsonTokenType.StartObject)
			{
				copy.Read();
				if (copy.TokenType == JsonTokenType.EndObject)
					return default;
			}

			return JsonSerializer.Deserialize<T>(ref reader, _options);
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			var cloned = new JsonSerializerOptions(options);
			cloned.Converters.Remove(this);
			JsonSerializer.Serialize(writer, value, _options);
		}
	}
}
