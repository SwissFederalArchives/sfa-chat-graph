using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.IOPub;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.Client
{
	public class CodeExecutionResult
	{
		public ExecuteRequest Request { get; init; }
		public ExecuteReply Reply { get; init; }
		public DisplayDataMessage[] Results { get; init; }
	}
}
