using AwosFramework.ApiClients.Jupyter.WebSocket.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Json
{
	public class BufferHolderConverter : JsonConverter<IBufferHolder>
	{
		public override IBufferHolder? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var holder = new ListBufferHolder();
			if (reader.TokenType != JsonTokenType.StartArray)
				throw new JsonException("Expected start of array");

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndArray)
					break;
				if (reader.TokenType != JsonTokenType.String)
					throw new JsonException("Expected string token");

				var buffer = reader.GetBytesFromBase64();
				holder.Add(buffer);
			}

			return holder;
		}

		public override void Write(Utf8JsonWriter writer, IBufferHolder value, JsonSerializerOptions options)
		{
			writer.WriteStartArray();
			for(int i = 0; i < value.Length; i++)
				writer.WriteBase64StringValue(value[i].Span);

			writer.WriteEndArray();
		}
	}
}
