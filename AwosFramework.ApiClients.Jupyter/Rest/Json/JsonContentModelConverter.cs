using AwosFramework.ApiClients.Jupyter.Rest.Models.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Json
{
	public class JsonContentModelConverter : JsonConverter<ContentModel>
	{
		private Type FindModelType(Utf8JsonReader readerCopy)
		{
			int depth = 0;

			while (readerCopy.Read())
			{
				if (readerCopy.TokenType == JsonTokenType.EndObject)
					depth--;

				if(readerCopy.TokenType == JsonTokenType.StartObject)
					depth++;

				if (readerCopy.TokenType == JsonTokenType.PropertyName && depth == 0)
				{
					var propertyName = readerCopy.GetString();
					if (propertyName == "type")
					{
						readerCopy.Read();
						var type = readerCopy.GetString();
						if (Enum.TryParse<ContentType>(type, true, out var contentType) == false)
							throw new JsonException($"Unknown content type {type}");

						return contentType switch
						{
							ContentType.Directory => typeof(DirectoryModel),
							ContentType.File => typeof(FileModel),
							ContentType.Notebook => typeof(NotebookModel),
							_ => throw new JsonException($"Unknown content type {type}"),
						};
					}
				}
			}

			throw new JsonException("Could not find type property in JSON.");
		}

		public override ContentModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var type = FindModelType(reader);
			return (ContentModel)JsonSerializer.Deserialize(ref reader, type, options);
		}

		public override void Write(Utf8JsonWriter writer, ContentModel value, JsonSerializerOptions options)
		{
			JsonSerializer.Serialize(writer, value, value.GetType(), options);
		}
	}
}
