using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Client.Jupyter
{
	public class KernelSessionClientOptions
	{
		public string? KernelSpecName { get; set; }
		public Guid? KernelId { get; set; }

		[MemberNotNullWhen(true, nameof(StoragePath))]
		public bool CreateWorkingDirectory { get; set; } = true;
		public bool DeleteWorkingDirectoryOnDispose { get; set; } = true;
		public bool KillKernelOnDispose { get; set; } = false;
		public Guid StorageId { get; set; } = Guid.NewGuid();
		public string StoragePathFormat { get; set; } = "sessions/{0}";

		public string? StoragePath => CreateWorkingDirectory ? string.Format(StoragePathFormat, StorageId) : null;

		public required JupyterWebsocketOptions DefaultWebsocketOptions { get; init; }
	}
}
