using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Rest.Models.Session
{
	public class StartSessionRequest
	{
		public SessionType Type { get; set; }
		public string? Path { get; set; }
		public string? Name { get; set; }
		public required KernelIdentification Kernel { get; set; }

		[SetsRequiredMembers]
		private StartSessionRequest(SessionType type, string? path, string? name, KernelIdentification kernel)
		{
			Type = type;
			Path = path;
			Name = name;
			Kernel = kernel;
		}

		public static StartSessionRequest CreateNotebook(KernelIdentification kernel, string path, string? name = null) => new StartSessionRequest(SessionType.Notebook, path, name, kernel);
		public static StartSessionRequest CreateConsole(KernelIdentification kernel, string? name = null) => new StartSessionRequest(SessionType.Console, null, name, kernel);
	}
}
