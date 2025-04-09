using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Protocol
{
	public sealed class ProtocolResult<TRes, TError>
	{
		private readonly TRes? _message;
		private readonly TError? _error;
		public int CountRead { get; init; }

		internal ProtocolResult(int countRead, TRes? message, TError? error)
		{
			CountRead = countRead;
			_message = message;
			_error = error;
		}

		public bool IsCompleted([NotNullWhen(true)] out TRes? message)
		{
			message = _message;
			return _message != null;
		}

		public bool IsError([NotNullWhen(true)] out TError? error)
		{
			error = _error;
			return _error != null;
		}
	}

	public static class ProtocolResult
	{
		public static ProtocolResult<TRes, TError> CompletedResult<TRes, TError>(TRes message, int countRead) => new ProtocolResult<TRes, TError>(countRead, message, default);
		public static ProtocolResult<TRes, TError> ErrorResult<TRes, TError>(TError error, int countRead) => new ProtocolResult<TRes, TError>(countRead, default, error);
		public static ProtocolResult<TRes, TError> OkResult<TRes, TError>(int countRead) => new ProtocolResult<TRes, TError>(countRead, default, default);
	
		public static ProtocolResult<WebsocketMessage, JupyterWebsocketError> CompletedResult(WebsocketMessage message, int countRead) => new ProtocolResult<WebsocketMessage, JupyterWebsocketError>(countRead, message, default);
		public static ProtocolResult<WebsocketMessage, JupyterWebsocketError> ErrorResult(JupyterWebsocketError error, int countRead) => new ProtocolResult<WebsocketMessage, JupyterWebsocketError>(countRead, default, error);
		public static ProtocolResult<WebsocketMessage, JupyterWebsocketError> PartialResult(int countRead) => new ProtocolResult<WebsocketMessage, JupyterWebsocketError>(countRead, default, default);


	}
}