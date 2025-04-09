using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Json
{
	public class HistoryEntryConverter : JsonConverter<HistoryEntry>
	{
		public override HistoryEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			int session, lineNumber;
			string input, output = null;
			reader.Read();
			session = reader.GetInt32();
			reader.Read();
			lineNumber = reader.GetInt32();
			reader.Read();
			input = reader.GetString() ?? string.Empty;
			reader.Read();
			if(reader.TokenType == JsonTokenType.String)
			{
				output = reader.GetString();
				reader.Read();
			}

			if (reader.TokenType != JsonTokenType.EndArray)
				throw new JsonException("Expected end of array");

			return new HistoryEntry { Session = session, LineNumber = lineNumber, Input = input, Output = output };
		}

		public override void Write(Utf8JsonWriter writer, HistoryEntry value, JsonSerializerOptions options)
		{
			writer.WriteStartArray();
			writer.WriteNumberValue(value.Session);
			writer.WriteNumberValue(value.LineNumber);
			writer.WriteStringValue(value.Input);
			if (string.IsNullOrEmpty(value.Output) == false)
				writer.WriteStringValue(value.Output);
			writer.WriteEndArray();
		}
	}
}
