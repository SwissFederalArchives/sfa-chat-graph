using AngleSharp.Io;
using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket;
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
		Console.WriteLine($"Received Message: {msg.Header.MessageType} on Channel {msg.Channel}");
		Console.WriteLine(JsonSerializer.Serialize(msg.Content));
	};
	await ws.IOTask;
}