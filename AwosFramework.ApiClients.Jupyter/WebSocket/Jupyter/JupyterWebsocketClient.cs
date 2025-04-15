using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.IOPub;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Models.Messages.Shell;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Parser;
using AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter.Protocol;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AwosFramework.ApiClients.Jupyter.WebSocket.Jupyter
{
	public class JupyterWebsocketClient : WebsocketClientBase<JupyterWebsocketOptions, IJupyterProtocol, JupyterMessage, JupyterError>
	{

		private readonly Channel<JupyterMessage> _receiveChannel;
		private readonly Channel<JupyterMessage> _sendChannel;
		private readonly Dictionary<string, ObservableWrapper> _observableMessages = new();

		public ChannelReader<JupyterMessage> MessageReader => _receiveChannel.Reader;

		public JupyterWebsocketClient(JupyterWebsocketOptions options, CookieContainer cookies) : base(options, cookies)
		{
			var sendOptions = new UnboundedChannelOptions { SingleReader = true };
			_sendChannel = Channel.CreateUnbounded<JupyterMessage>(sendOptions);
			if (options.MaxMessages.HasValue)
			{
				var channelOptions = new BoundedChannelOptions(options.MaxMessages.Value) { SingleWriter = true, FullMode = BoundedChannelFullMode.DropOldest };
				_receiveChannel = Channel.CreateBounded<JupyterMessage>(channelOptions);
			}
			else
			{
				_receiveChannel = Channel.CreateUnbounded<JupyterMessage>();
			}
		}


		record ObservableWrapper(string Id, ObservableSource<JupyterMessage> Observable)
		{
			public bool IODone { get; set; } = false;
			public bool ExecuteDone { get; set; } = false;
			public bool Done => IODone && ExecuteDone;
		}

		protected override void DisposeImpl()
		{
			base.DisposeImpl();
			_sendChannel.Writer.Complete();
			_receiveChannel.Writer.Complete();
			while (_receiveChannel.Reader.TryRead(out var msg))
				msg.Dispose();

			while (_sendChannel.Reader.TryRead(out var msg))
				msg.Dispose();
		}


		protected override Task<JupyterMessage> NextMessagAsync(CancellationToken token) => _sendChannel.Reader.ReadAsync(token).AsTask();
		protected async override Task HandleResultAsync(JupyterMessage message)
		{
			_logger?.LogDebug("Received message {MessageType} on channel {Channel}", message.Header.MessageType, message.Channel);
			if (message.Content is KernelStatusMessage status && status.ExecutionState == ExecutionState.Idle && message.ParentHeader != null && _observableMessages.TryGetValue(message.ParentHeader.Id, out var wrapper))
			{
				wrapper.IODone = true;
				if (wrapper.Done)
				{
					_observableMessages.Remove(message.ParentHeader.Id);
					wrapper.Observable.NotifyCompleted();
				}
			}

			if (message.ParentHeader != null && message.Content is not KernelStatusMessage && _observableMessages.TryGetValue(message.ParentHeader.Id, out wrapper))
			{
				wrapper.Observable.NotifyItem(message);
				if (message.Content is ExecuteReply)
				{
					wrapper.ExecuteDone = true;
					if (wrapper.Done)
					{
						_observableMessages.Remove(message.ParentHeader.Id);
						wrapper.Observable.NotifyCompleted();
					}
				}
			}
			else
			{
				await _receiveChannel.Writer.WriteAsync(message);
			}
		}


		private JupyterMessage CreateMessageFromPayload(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, JupyterMessage? parent = null)
		{
			ArgumentNullException.ThrowIfNull(payload, nameof(payload));
			var attrs = payload.GetType().GetCustomAttributes<MessageTypeAttribute>().ToArray();
			var attr = attrs.Length > 1 ? attrs.FirstOrDefault(x => x.MessageType.Contains("request")) : attrs.FirstOrDefault();
			if (attr == null)
				throw new ArgumentException($"Type {payload.GetType()} does not have a MessageTypeAttribute");

			var message = new JupyterMessage
			{
				TransferableBuffers = buffers,
				Channel = attr.Channel,
				Content = payload,
				ParentHeader = parent?.Header,
				MetaData = metaData,
				Header = new Models.MessageHeader
				{
					Id = Guid.NewGuid().ToString(),
					MessageType = attr.MessageType,
					SessionId = Options.SessionId.ToString(),
					UserName = Options.UserName,
					Version = attr.Version,
					SubshellId = null,
					Timestamp = DateTime.UtcNow
				}
			};

			return message;
		}

		public async Task<IObservable<JupyterMessage>> SendAndObserveAsync(JupyterMessage message)
		{
			var observable = new ObservableSource<JupyterMessage>();
			_observableMessages[message.Header!.Id] = new ObservableWrapper(message.Header.Id, observable);
			await SendAsync(message);
			return observable;
		}

		public async Task<IObservable<JupyterMessage>> SendAndObserveAsync(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, JupyterMessage? parent = null)
		{
			var message = CreateMessageFromPayload(payload, buffers, metaData, parent);
			var observable = new ObservableSource<JupyterMessage>();
			_observableMessages[message.Header!.Id] = new ObservableWrapper(message.Header.Id, observable);
			await SendAsync(message);
			return observable;
		}


		public async Task<JupyterMessage> SendAsync(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, JupyterMessage? parent = null)
		{
			var message = CreateMessageFromPayload(payload, buffers, metaData, parent);
			await SendAsync(message);
			return message;
		}

		public async Task SendAsync(JupyterMessage message)
		{
			await _sendChannel.Writer.WriteAsync(message, CancellationToken);
		}


	}
}
