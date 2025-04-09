using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	public class PutContentRequest
	{
		public object? Content { get; set; }
		public ContentFormat? Format { get; set; }
		public ContentType Type { get; set; }
		public string? Name { get; set; }
		public string? Path { get; set; }

		[SetsRequiredMembers]
		private PutContentRequest(string? content, ContentFormat? format, ContentType type)
		{
			Content = content;
			Format = format;
			Type = type;
		}

		[SetsRequiredMembers]
		private PutContentRequest(JsonDocument content, ContentType type)
		{
			Content = content;
			Format = ContentFormat.Json;
			Type = type;
		}

		public static PutContentRequest CreateDirectory() => new PutContentRequest(null, null, ContentType.Directory);

		public static PutContentRequest CreateText(string content) => new PutContentRequest(content, ContentFormat.Text, ContentType.File);

		public static PutContentRequest CreateJson(JsonDocument json) => new PutContentRequest(json, ContentType.File);
		public static PutContentRequest CreateNotebook(JsonDocument notebook) => new PutContentRequest(notebook, ContentType.Notebook);

		public static PutContentRequest CreateBinary(Stream stream, string? name = null, string? path = null)
		{
			using var converter = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read);
			using var reader = new StreamReader(converter);
			var base64 = reader.ReadToEnd();
			return new PutContentRequest(base64, ContentFormat.Base64, ContentType.File) { Path = path, Name = name };
		}

		public static PutContentRequest CreateBinary(byte[] data, string? name = null, string? path = null)
		{
			var base64 = Convert.ToBase64String(data);
			return new PutContentRequest(base64, ContentFormat.Base64, ContentType.File) { Path = path, Name = name };
		}

	}
}
