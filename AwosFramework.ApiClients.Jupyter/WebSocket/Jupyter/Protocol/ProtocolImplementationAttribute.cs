using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Protocol
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class ProtocolImplementationAttribute : Attribute
	{
		public string ProtocolName { get; init; }
		public bool IsDefault { get; init; } = false;

		public ProtocolImplementationAttribute(string name)
		{
			ProtocolName = name;
		}
	}
}
