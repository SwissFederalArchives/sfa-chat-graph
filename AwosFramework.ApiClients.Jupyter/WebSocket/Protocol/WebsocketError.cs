using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Protocol
{
	public class WebsocketError
	{
		public WebsocketParserError ErrorCode { get; init; }
		public Exception? Exception { get; init; }

		public WebsocketError(WebsocketParserError errorCode, Exception? exception = null)
		{
			ErrorCode = errorCode;
			Exception = exception;
		}

		public static WebsocketError FromParserState(ref ParserState state)
		{
			if (state.IsErrorState(out var errorCode) == false)
				throw new ArgumentException("ParserState is not in error state", nameof(state));

			return new WebsocketError(errorCode.Value, state.Exception);
		}
	}
}
