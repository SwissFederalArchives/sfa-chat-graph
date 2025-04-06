using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	public sealed class DirectoryModel : ContentModel
	{

		public DirectoryModel() : base(ContentType.Directory, null)
		{

		}

		public DirectoryModel(IJupyterRestClient client) : base(ContentType.Directory, client)
		{
		}

		[JsonPropertyName("content")]
		public required ContentModel[] ContentRaw { get; set; }

		[JsonIgnore]
		public ContentModel[] Content
		{
			get
			{
				return ContentRaw ?? LoadContentAsync().Result;
			}
		}

		private async Task<ContentModel[]> LoadContentAsync()
		{
			var content = await _client.GetDirectoryContentAsync(Path);
			ContentRaw = content.Content ?? Array.Empty<ContentModel>();
			return ContentRaw;
		}

		public async Task<ContentModel[]> GetContentAsync(bool reload = false)
		{
			if (ContentRaw == null || reload)
				await LoadContentAsync();

			return ContentRaw!;
		}

	}
}
