using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SfaChatGraph.Server.FunctionCalling
{
	public interface ICallable
	{
		public Task<object> CallAsync(object context);
	}
}
