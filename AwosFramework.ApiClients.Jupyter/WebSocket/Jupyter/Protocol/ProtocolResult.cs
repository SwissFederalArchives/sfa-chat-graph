using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Protocol
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
	
		public static ProtocolResult<TerminalMessage, TerminalError> ErrorResult(TerminalError error, int countRead) => new ProtocolResult<TerminalMessage, TerminalError>(countRead, default, error);
		public static ProtocolResult<TerminalMessage, TerminalError> CompletedResult(TerminalMessage message, int countRead) => new ProtocolResult<TerminalMessage, TerminalError>(countRead, message, default);
		public static ProtocolResult<TerminalMessage, TerminalError> TerminalPartialResult(int countRead) => new ProtocolResult<TerminalMessage, TerminalError>(countRead, default, default);


		public static ProtocolResult<JupyterMessage, JupyterError> CompletedResult(JupyterMessage message, int countRead) => new ProtocolResult<JupyterMessage, JupyterError>(countRead, message, default);
		public static ProtocolResult<JupyterMessage, JupyterError> ErrorResult(JupyterError error, int countRead) => new ProtocolResult<JupyterMessage, JupyterError>(countRead, default, error);
		public static ProtocolResult<JupyterMessage, JupyterError> PartialResult(int countRead) => new ProtocolResult<JupyterMessage, JupyterError>(countRead, default, default);


	}
}