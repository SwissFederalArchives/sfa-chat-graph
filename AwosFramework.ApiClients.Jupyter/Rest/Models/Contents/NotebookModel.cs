using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Contents
{
	public sealed class NotebookModel : ContentModel
	{
		public NotebookModel() : base(ContentType.Notebook, null)
		{
		}

		public NotebookModel(IJupyterRestClient client) : base(ContentType.Notebook, client)
		{
		}

		[JsonPropertyName("content")]
		public required JsonDocument Content { get; set; }

		[JsonPropertyName("mimetype")]
		public string? MimeType { get; set; }

		[JsonPropertyName("size")]
		public int Size { get; set; }

		[JsonPropertyName("format")]
		public ContentFormat Format => ContentFormat.Json;

		public Task<Checkpoint[]> GetCheckpointsAsync() => _client.GetCheckpointsAsync(Path);
		public Task<Checkpoint> CreateCheckpointAsync() => _client.CreateCheckpointAsync(Path);

		public Task DeleteCheckpointAsync(Checkpoint checkpoint) => _client.DeleteCheckpointAsync(Path, checkpoint.Id);
		public Task DeleteCheckpointAsync(string checkpointId) => _client.DeleteCheckpointAsync(Path, checkpointId);

		public Task RestoreCheckpointAsync(Checkpoint checkpoint, bool fetchRestored = true) => _client.RestoreCheckpointAsync(Path, checkpoint.Id);
		public async Task RestoreCheckpointAsync(string checkpointId, bool fetchRestored = true)
		{
			await _client.RestoreCheckpointAsync(Path, checkpointId);
			if (fetchRestored)
			{
				var response = await _client.GetNotebookContentAsync(Path);
				Content = response.Content;
			}
		}

	}
}
