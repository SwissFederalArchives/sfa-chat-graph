using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Json
{
	public class DebugMessageConverter : JsonConverter<DebugMessage>
	{
		public override DebugMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var content = JsonSerializer.Deserialize<JsonDocument>(ref reader, options);
			return new DebugMessage { Content = content };
		}

		public override void Write(Utf8JsonWriter writer, DebugMessage value, JsonSerializerOptions options)
		{
			JsonSerializer.Serialize(writer, value.Content, options);
		}
	}
}
