using AwosFramework.ApiClients.Jupyter.Rest.Models;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Contents;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Identity;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Terminal;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("sfa-chat-graph.Playground")]
namespace AwosFramework.ApiClients.Jupyter.Rest
{
	public interface IJupyterRestClient
	{
		[Get("/")]
		public Task<VersionResponse> GetVersionAsync();

		[Get("/contents/{path}")]
		public Task<ContentModel> GetContentsAsync(string path, [AliasAs("type")] ContentType? type = null, [AliasAs("format")] ContentFormat? format = null, [AliasAs("content")] bool includeContent = true, [AliasAs("hash")] bool includeHash = false);

		public async Task<DirectoryModel> GetDirectoryAsync(string path)
		{
			var dir = await GetContentsAsync(path, ContentType.Directory, null, true);
			return dir.AsDirectory;
		}

		public Task<ContentModel> CreateContentCopyAsync(string path, string copy_from) => CreateContentAsync(path, CreateContentRequest.CreateCopy(copy_from));
		public Task<ContentModel> CreateContentFileAsync(string path, string extension) => CreateContentAsync(path, CreateContentRequest.CreateFile(path, extension));
		public Task<ContentModel> CreateContentDirectoryAsync(string path) => PutContentAsync(path, PutContentRequest.CreateDirectory());

		public async Task<ContentModel?> CreateDirectoriesAsync(string path)
		{
			ContentModel dir = null;
			var parts = path.Split('/');
			for(int i = 0; i < parts.Length; i++)
				dir = await CreateContentDirectoryAsync(string.Join('/', parts.Take(i+1)));

			return dir;
		}

		public async Task DeleteRecursivelyAsync(ContentModel model)
		{
			if (model.IsDirectory)
			{
				var children = await model.AsDirectory.GetContentAsync(true);
				await Task.WhenAll(children.Select(c => DeleteRecursivelyAsync(c)));
			}

			await DeleteContentAsync(model.Path);
		}

		public async Task DeleteRecursivelyAsync(string path)
		{
			var contents = await GetContentsAsync(path);
			await DeleteRecursivelyAsync(contents);
		}

		[Post("/contents/{path}")]
		public Task<ContentModel> CreateContentAsync(string path, [Body] CreateContentRequest request);

		[Patch("/contents/{oldPath}")]
		public Task<ContentModel> RenameContentAsync(string oldPath, [AliasAs("path")] string newPath);

		[Put("/contents/{path}")]
		public Task<ContentModel> PutContentAsync(string path, [Body] PutContentRequest content);

		[Delete("/contents/{path}")]
		public Task DeleteContentAsync(string path);

		[Get("/contents/{path}/checkpoints")]
		public Task<Checkpoint[]> GetCheckpointsAsync(string path);

		[Post("/contents/{path}/checkpoints")]
		public Task<Checkpoint> CreateCheckpointAsync(string path);

		[Post("/contents/{path}/checkpoints/{checkpoint_id}")]
		public Task RestoreCheckpointAsync(string path, string checkpoint_id);

		[Delete("/contents/{path}/checkpoints/{checkpoint_id}")]
		public Task DeleteCheckpointAsync(string path, string checkpoint_id);

		[Get("/sessions")]
		public Task<SessionModel[]> GetSessionsAsync();

		[Get("/sessions/{id}")]
		public Task<SessionModel> GetSessionAsync(Guid id);

		[Delete("/sessions/{id}")]
		public Task DeleteSessionAsync(Guid id);

		[Post("/sessions")]
		public Task<SessionModel> StartSessionAsync([Body] StartSessionRequest req);

		[Get("/kernels")]
		public Task<KernelModel[]> GetKernelsAsync();

		[Get("/kernels/{id}")]
		public Task<KernelModel> GetKernelAsync(Guid id);

		[Delete("/kernels/{id}")]
		public Task ShutdownKernelAsync(Guid id);
		public Task ShutdownKernelAsync(KernelModel kernel) => ShutdownKernelAsync(kernel.Id.Value);

		[Post("/kernels")]
		public Task<KernelModel> StartKernelAsync([Body] StartKernelRequest req);
		public Task<KernelModel> StartKernelAsync(string specName, string? path = null) => StartKernelAsync(new StartKernelRequest { SpecName = specName, Path = path });

		[Post("/kernels/{id}/interrupt")]
		public Task InterruptKernelAsync(Guid id);
		public Task InterruptKernelAsync(KernelModel kernel) => InterruptKernelAsync(kernel.Id.Value);

		[Post("/kernels/{id}/restart")]
		public Task RestartKernelAsync(Guid id);
		public Task RestartKernelAsync(KernelModel kernel) => RestartKernelAsync(kernel.Id.Value);

		[Get("/kernelspecs")]
		public Task<KernelSpecsResponse> GetKernelSpecsAsync();

		[Get("/config/{section}")]
		public Task<JsonDocument> GetConfigSectionAsync(string section);

		[Patch("/config/{section}")]
		public Task UpdateConfigSectionAsync(string section, [Body] JsonDocument data);

		[Get("/api/terminals")]
		public Task<TerminalModel[]> GetTerminalsAsync();

		[Get("/api/terminals/{id}")]
		public Task<TerminalModel> GetTerminalAsync(string id);
		public Task<TerminalModel> GetTerminalAsync(TerminalModel terminal) => GetTerminalAsync(terminal.Name);

		[Delete("/api/terminals/{id}")]
		public Task CloseTerminalAsync(string id);
		public Task CloseTerminalAsync(TerminalModel terminal) => CloseTerminalAsync(terminal.Name);

		[Get("/me")]
		public Task<IdentityResponse> GetCurrentUserAsync([AliasAs("permissions")] string permissionFilter = null);
		public Task<IdentityResponse> GetCurrentUserAsync(Dictionary<string, string[]> permissionsFilter) => GetCurrentUserAsync(JsonSerializer.Serialize(permissionsFilter));

		[Get("/status")]
		public Task<StatusModel> GetStatusAsync();

		[Get("/contents/{path}?type=file&content=1")]
		internal Task<FileContentResponse> GetFileContentAsync(string path, [AliasAs("format")] ContentFormat format);

		[Get("/contents/{path}?type=notebook&content=1")]
		internal Task<NotebookContentResponse> GetNotebookContentAsync(string path);

		[Get("/contents/{path}?type=directory&content=1")]
		internal Task<DirectoryContentResponse> GetDirectoryContentAsync(string path);
	}
}
