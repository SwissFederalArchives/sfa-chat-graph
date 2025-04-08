using AngleSharp.Io;
using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket.Models.Messages.Shell;
using AwosFramework.ApiClients.Jupyter.WebSocket.Parser;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;
using sfa_chat_graph.Server.RDF;
using sfa_chat_graph.Server.RDF.Endpoints;
using sfa_chat_graph.Server.Utils;
using SfaChatGraph.Server.RDF;
using System.Text;
using System.Text.Json;
using VDS.RDF;
using VDS.RDF.Dynamic;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;

var loggerFactory = LoggerFactory.Create(builder =>
{
	builder.AddConsole();
	builder.SetMinimumLevel(LogLevel.Debug);
});

var token = "81227d216ffcfdb82faf22b1b88d7ab20fc9e8f41de22639";
var jupyter = JupyterRestClient.GetRestClient("http://localhost:8888", token);
var sessions = await jupyter.GetSessionsAsync();


if(sessions.Length > 0)
{
	var session = sessions[0];
	var options = new JupyterWebsocketOptions("ws://localhost:8888", session.Kernel.Id, Guid.NewGuid(), token)
	{
		LoggerFactory = loggerFactory,
		MaxReconnectTries = 3
	};

	var ws = new JupyterWebsocketClient(options);
	await ws.ConnectAsync();
	ws.OnReceive += (msg) =>
	{
		Console.WriteLine($"Received Message: {msg.Header.MessageType} on Channel {msg.Channel} with Id {msg.Header.Id} and Parent Id {(msg.ParentHeader?.Id ?? "None")}");
		Console.WriteLine(JsonSerializer.Serialize(msg.Content));
	};

	var executeMsg = new WebsocketMessage
	{
		Buffers = PooledBufferHolder.Empty,
		Channel = ChannelKind.Shell,
		Header = new MessageHeader
		{
			Id = Guid.NewGuid().ToString(),
			MessageType = "execute_request",
			SessionId = options.SessionId.ToString(),
			UserName = "username",
			Version = "5.3",
			SubshellId = null,
			Timestamp = DateTime.UtcNow
		},
		ParentHeader = null,
		Content = new ExecuteRequest
		{
			Code = "4 + 5",
			UserExpressions = new()
			{
				["test"] = "3*3"
			}
		},
		MetaData = null
	};

	await ws.SendAsync(executeMsg);
	Console.WriteLine("Sent msg with id: {0}", executeMsg.Header.Id);
	await ws.IOTask;
}