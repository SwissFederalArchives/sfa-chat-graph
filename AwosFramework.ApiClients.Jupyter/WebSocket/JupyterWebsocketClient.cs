using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models.Session;
using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket.Base;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.IOPub;
using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using AwosFramework.ApiClients.Jupyter.WebSocket.Protocol;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AwosFramework.ApiClients.Jupyter.WebSocket
{
	public class JupyterWebsocketClient : WebsocketClientBase<JupyterWebsocketOptions, IWebsocketProtocol, WebsocketMessage, JupyterWebsocketError>
	{
		
		private readonly Channel<WebsocketMessage> _receiveChannel;
		private readonly Channel<WebsocketMessage> _sendChannel;
		private readonly Dictionary<string, ObservableSource<WebsocketMessage>> _observableMessages = new();

		public ChannelReader<WebsocketMessage> MessageReader => _receiveChannel.Reader;
	
		public JupyterWebsocketClient(JupyterWebsocketOptions options) : base(options)
		{	
			var sendOptions = new UnboundedChannelOptions { SingleReader = true };
			_sendChannel = Channel.CreateUnbounded<WebsocketMessage>(sendOptions);
			if (options.MaxMessages.HasValue)
			{
				var channelOptions = new BoundedChannelOptions(options.MaxMessages.Value) { SingleWriter = true, FullMode = BoundedChannelFullMode.DropOldest };
				_receiveChannel = Channel.CreateBounded<WebsocketMessage>(channelOptions);
			}
			else
			{
				_receiveChannel = Channel.CreateUnbounded<WebsocketMessage>();
			}
		}

		public JupyterWebsocketClient(Uri endpoint, Guid kernelId, Guid? sessionId = null, string? token = null) : this(new JupyterWebsocketOptions(endpoint, kernelId, sessionId, token))
		{


		}

	
		protected override void DisposeImpl()
		{
			base.DisposeImpl();
			while (_receiveChannel.Reader.TryRead(out var msg))
				msg.Dispose();

			while (_sendChannel.Reader.TryRead(out var msg))
				msg.Dispose();
		}


		protected override Task<WebsocketMessage> NextMessagAsync(CancellationToken token) => _sendChannel.Reader.ReadAsync(token).AsTask();
		protected async override Task HandleResultAsync(WebsocketMessage message)
		{
			_logger?.LogDebug("Received message {MessageType} on channel {Channel}", message.Header.MessageType, message.Channel);
			if (message.Content is KernelStatusMessage status && status.ExecutionState == ExecutionState.Idle && message.ParentHeader != null && _observableMessages.TryGetValue(message.ParentHeader.Id, out var observable))
			{
				_observableMessages.Remove(message.ParentHeader.Id);
				observable.NotifyCompleted();
			}

			if(message.ParentHeader != null && message.Content is not KernelStatusMessage && _observableMessages.TryGetValue(message.ParentHeader.Id, out var ovservable))
			{
				ovservable.NotifyItem(message);
			}
			else
			{
				await _receiveChannel.Writer.WriteAsync(message);
			}
		}


		private WebsocketMessage CreateMessageFromPayload(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, WebsocketMessage? parent = null)
		{
			ArgumentNullException.ThrowIfNull(payload, nameof(payload));
			var attrs = payload.GetType().GetCustomAttributes<MessageTypeAttribute>().ToArray();
			var attr = attrs.Length > 1 ? attrs.FirstOrDefault(x => x.MessageType.Contains("request")) : attrs.FirstOrDefault();
			if (attr == null)
				throw new ArgumentException($"Type {payload.GetType()} does not have a MessageTypeAttribute");

			var message = new WebsocketMessage
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

		public async Task<IObservable<WebsocketMessage>> SendAndObserveAsync(WebsocketMessage message)
		{
			var observable = new ObservableSource<WebsocketMessage>();
			_observableMessages[message.Header!.Id] = observable;
			await SendAsync(message);
			return observable;
		}

		public async Task<IObservable<WebsocketMessage>> SendAndObserveAsync(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, WebsocketMessage? parent = null)
		{
			var message = CreateMessageFromPayload(payload, buffers, metaData, parent);
			var observable = new ObservableSource<WebsocketMessage>();
			_observableMessages[message.Header!.Id] = observable;
			await SendAsync(message);
			return observable;
		}


		public async Task<WebsocketMessage> SendAsync(object payload, ITransferableBufferHolder? buffers = null, JsonDocument? metaData = null, WebsocketMessage? parent = null)
		{
			var message = CreateMessageFromPayload(payload, buffers, metaData, parent);
			await SendAsync(message);
			return message;
		}

		public async Task SendAsync(WebsocketMessage message)
		{
			await _sendChannel.Writer.WriteAsync(message, CancellationToken);
		}


	}
}
