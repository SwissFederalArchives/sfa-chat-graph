using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	public abstract class ContentModel
	{
		protected readonly IJupyterRestClient _client;

		public ContentModel(ContentType type, IJupyterRestClient client)
		{
			Type = type;
			_client = client;
		}

		public bool IsFileOrDirectory([NotNullWhen(true)] out FileModel? file, [NotNullWhen(false)] out DirectoryModel? directory)
		{
			if (Type == ContentType.File)
			{
				file = AsFile;
				directory = null;
				return true;
			}
			else
			{
				file = null;
				directory = AsDirectory;
				return false;
			}
		}

		public FileModel AsFile => (FileModel)this;
		public DirectoryModel AsDirectory => (DirectoryModel)this;
		public bool IsFile => Type == ContentType.File;
		public bool IsDirectory => Type == ContentType.Directory;

		[JsonPropertyName("type")]
		public ContentType Type { get; init; }

		[JsonPropertyName("name")]
		public required string Name { get; set; }

		[JsonPropertyName("path")]
		public required string Path { get; set; }

		[JsonPropertyName("last_modified")]
		public DateTime LastModified { get; set; }

		[JsonPropertyName("created")]
		public DateTime Created { get; set; }

		[JsonPropertyName("hash")]
		[NotNullIfNotNull(nameof(HashAlgorithm))]
		public string? Hash { get; set; }

		[JsonPropertyName("hash_algorithm")]
		[NotNullIfNotNull(nameof(Hash))]
		public string? HashAlgorithm { get; set; }

		[JsonPropertyName("writable")]
		public bool Writable { get; set; }
	}
}
