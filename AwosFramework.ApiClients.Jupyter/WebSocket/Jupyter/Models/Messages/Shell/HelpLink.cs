using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell
{
	public class HelpLink
	{
		public required string Text { get; set; }
		public required string Url { get; set; }
	}
}
