using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	public class CreateContentRequest
	{
		[JsonPropertyName("copy_from")]
		public string? CopyFrom { get; set; }

		[JsonPropertyName("ext")]
		public string? Extension { get; set; }

		[JsonPropertyName("type")]
		public ContentType Type { get; set; }

		internal CreateContentRequest(string? copyFrom, string? extension, ContentType type)
		{
			CopyFrom=copyFrom;
			Extension=extension;
			Type=type;
		}

		public static CreateContentRequest CreateCopy(string path) => new CreateContentRequest(path, null, ContentType.File);
		public static CreateContentRequest CreateDirectory(string path) => new CreateContentRequest(path, null, ContentType.Directory);
		public static CreateContentRequest CreateFile(string path, string extension) => new CreateContentRequest(path, extension, ContentType.File);
	}
}
