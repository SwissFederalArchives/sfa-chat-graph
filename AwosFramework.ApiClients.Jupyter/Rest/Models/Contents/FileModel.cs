using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	public sealed class FileModel : ContentModel
	{

		public FileModel() : base(ContentType.File, null)
		{

		}

		public FileModel(IJupyterRestClient client) : base(ContentType.File, client)
		{
		}


		[JsonPropertyName("mimetype")]
		public string? MimeType { get; set; }

		[JsonPropertyName("size")]
		public int Size { get; set; }

		[JsonPropertyName("format")]
		public ContentFormat? Format { get; set; }

		[JsonPropertyName("content")]
		public string? RawContent { get; set; }

		public Task<Checkpoint[]> GetCheckpointsAsync() => _client.GetCheckpointsAsync(Path);
		public Task<Checkpoint> CreateCheckpointAsync() => _client.CreateCheckpointAsync(Path);

		public Task DeleteCheckpointAsync(Checkpoint checkpoint) => _client.DeleteCheckpointAsync(Path, checkpoint.Id);
		public Task DeleteCheckpointAsync(string checkpointId) => _client.DeleteCheckpointAsync(Path, checkpointId);

		public Task RestoreCheckpointAsync(Checkpoint checkpoint) => _client.RestoreCheckpointAsync(Path, checkpoint.Id);
		public Task RestoreCheckpointAsync(string checkpointId) => _client.RestoreCheckpointAsync(Path, checkpointId);



		public async Task<byte[]> GetBinaryContentAsync(bool forceReload = false)
		{
			if (Format.HasValue == false || forceReload)
				await LoadContentAsync();

			return Format switch
			{
				ContentFormat.Text => Encoding.UTF8.GetBytes(RawContent ?? string.Empty),
				ContentFormat.Base64 => Convert.FromBase64String(RawContent ?? string.Empty),
				_ => throw new NotSupportedException($"Content format {Format} is not supported."),
			};
		}

		public async Task<string> GetStringContentAsync(bool forceReload = false)
		{
			if (Format.HasValue == false || forceReload)
				await LoadContentAsync();

			return Format switch
			{
				ContentFormat.Text => RawContent ?? string.Empty,
				ContentFormat.Base64 => Encoding.UTF8.GetString(Convert.FromBase64String(RawContent ?? string.Empty)),
				_ => throw new NotSupportedException($"Content format {Format} is not supported."),
			};
		}

		public async Task<string> GetRawContentAsync(bool forceReload = false)
		{
			if (Format.HasValue == false || forceReload)
				await LoadContentAsync();
			
			return RawContent ?? string.Empty;
		}

		private bool IsTextMimeType()
		{
			if (string.IsNullOrEmpty(MimeType))
				return false;

			if (MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
				return true;

			if (MimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}

		private async Task LoadContentAsync()
		{
			var isText = IsTextMimeType();
			var response = await _client.GetFileContentAsync(Path, isText ? ContentFormat.Text : ContentFormat.Base64);
			if (response.Content == null)
				throw new InvalidOperationException("Content is null.");

			RawContent = response.Content;
			Format = response.Format;
		}
	}
}
