using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages
{
	public enum StatusType
	{
		Ok,
		Error,
		Aborted,
		Complete,
		Incomplete,
		Invalid,
		Unknown
	}
}
