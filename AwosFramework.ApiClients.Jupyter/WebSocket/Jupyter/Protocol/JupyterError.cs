using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Protocol
{
	public class JupyterError : IError
	{
		public WebsocketParserError ErrorCode { get; init; }
		public Exception? Exception { get; init; }
		object? IError.ErrorCode => ErrorCode;

		public JupyterError(WebsocketParserError errorCode, Exception? exception = null)
		{
			ErrorCode = errorCode;
			Exception = exception;
		}

		public static JupyterError FromParserState(ref ParserState state)
		{
			if (state.IsErrorState(out var errorCode) == false)
				throw new ArgumentException("ParserState is not in error state", nameof(state));

			return new JupyterError(errorCode.Value, state.Exception);
		}
	}
}
