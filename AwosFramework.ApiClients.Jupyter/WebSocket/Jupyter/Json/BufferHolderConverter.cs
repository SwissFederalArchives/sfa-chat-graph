using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Json
{
	public class BufferHolderConverter : JsonConverter<ITransferableBufferHolder>
	{
		private void ReadBytes(ref Utf8JsonReader reader, BufferHolderBuilder builder)
		{
			var b64Span = reader.ValueSpan;
			var count = Base64.GetMaxDecodedFromUtf8Length(b64Span.Length);
			Span<byte> buffer = stackalloc byte[count];
			Base64.DecodeFromUtf8(b64Span, buffer, out var bytesWritten, out var bytesConsumed);
			buffer = buffer.Slice(0, bytesWritten);
			builder.Add(buffer);
		}

		public override ITransferableBufferHolder? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
				throw new JsonException("Expected start of array");

			var builder = BufferHolderBuilder.Create();
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndArray)
					break;
				
				if (reader.TokenType != JsonTokenType.String)
					throw new JsonException("Expected string token");

				ReadBytes(ref reader, builder);
			}

			return builder.Build();
		}

		public override void Write(Utf8JsonWriter writer, ITransferableBufferHolder buffers, JsonSerializerOptions options)
		{
			writer.WriteStartArray();
			foreach(var buffer in buffers)
				writer.WriteBase64StringValue(buffer.Span);

			writer.WriteEndArray();
		}
	}
}
