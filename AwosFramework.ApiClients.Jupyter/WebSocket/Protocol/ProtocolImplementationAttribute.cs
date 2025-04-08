using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Protocol
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class ProtocolImplementationAttribute : Attribute
	{
		public string ProtocolName { get; init; }
		public bool IsDefault { get; init; } = false;

		public ProtocolImplementationAttribute(string name)
		{
			this.ProtocolName = name;
		}
	}
}
