using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Terminal
{
	public class TerminalWebsocketClient : WebsocketClientBase<TerminalWebsocketClientOptions, ITerminalProtocol, TerminalMessage, TerminalError>
	{
		private static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();
		private readonly RecyclableMemoryStream _stdOut;
		private readonly Channel<TerminalMessage> _sendChannel;
		private ObservableSource<TerminalMessage> _receiveObservable;
		public IObservable<TerminalMessage> ObservableMessages => _receiveObservable;

		public TerminalWebsocketClient(TerminalWebsocketClientOptions options) : base(options)
		{
			_stdOut = _streamManager.GetStream();
			_receiveObservable = new ObservableSource<TerminalMessage>();
			_sendChannel = Channel.CreateUnbounded<TerminalMessage>(new UnboundedChannelOptions
			{
				SingleReader = true
			});
		}

		public Task SendAsync(TerminalMessage message) => _sendChannel.Writer.WriteAsync(message).AsTask();
		public Task SendAsync(string message) => SendAsync(new TerminalMessage(TerminalMessageType.Stdin, message));

		public Stream StdOut => _stdOut;

		protected override void DisposeImpl()
		{
			base.DisposeImpl();
			_stdOut.Dispose();
			_sendChannel.Writer.Complete();
			_receiveObservable.NotifyCompleted();
		}


		protected override async Task HandleResultAsync(TerminalMessage message)
		{
			if (message.MessageType == TerminalMessageType.Stdout && message.Content.FirstOrDefault() is string content)
				await _stdOut.WriteAsync(Encoding.UTF8.GetBytes(content));

			_receiveObservable.NotifyItem(message);
		}

		protected override Task<TerminalMessage> NextMessagAsync(CancellationToken token) => _sendChannel.Reader.ReadAsync(token).AsTask();
	}
}
