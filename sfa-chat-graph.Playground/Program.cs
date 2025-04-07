using AngleSharp.Io;
using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models;
using AwosFramework.ApiClients.Jupyter.WebSocket;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
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


var token = "befecb7e902f2b5fa0d180ed2d017d6a0a7a7ef48fff6279";
var jupyter = JupyterRestClient.GetRestClient("http://localhost:8888", token);
var sessions = await jupyter.GetSessionsAsync();
if(sessions.Length > 0)
{
	var session = sessions[0];
	var ws = new JupyterWebsocketClient(new Uri("ws://localhost:8888"), session.Kernel.Id, session.Id, token);
	await ws.ConnectAsync();
	ws.OnReceive += (msg) =>
	{
		Console.WriteLine($"Received Message: {msg.Header.MessageType} on Channel {msg.Channel}");
		Console.WriteLine(JsonSerializer.Serialize(msg.Content));
	};
	await ws.ReadTask;
}