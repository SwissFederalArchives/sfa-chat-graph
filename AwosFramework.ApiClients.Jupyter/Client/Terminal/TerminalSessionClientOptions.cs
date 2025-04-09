using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Client.Terminal
{
	public class TerminalSessionClientOptions
	{

		[MemberNotNullWhen(true, nameof(StoragePath))]
		public bool CreateWorkingDirectory { get; set; } = false;
		public bool DeleteWorkingDirectoryOnDispose { get; set; } = true;
		public Guid StorageId { get; set; } = Guid.NewGuid();
		public string StoragePathFormat { get; set; } = "terminal/{0}";
		public string? StoragePath => CreateWorkingDirectory ? string.Format(StoragePathFormat, StorageId) : null;

		public string? TerminalId { get; set; }
		public bool CloseTerminalOnDispose { get; set; } = true;
		public required TerminalWebsocketClientOptions DefaultWebsocketOptions { get; init; }
	}
}
