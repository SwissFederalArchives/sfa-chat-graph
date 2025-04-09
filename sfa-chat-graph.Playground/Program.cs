using AngleSharp.Io;
using AwosFramework.ApiClients.Jupyter.Client;
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

var token = "371e778dcd2efdadab0e25fda1989e361d03d160f7aa970a";
var jupyter = new JupyterClient("http://localhost:8888", token, loggerFactory);

using(var session = await jupyter.StartKernelSessionAsync())
{
	var jsonContent = $$"""
	{
		"message": "Hello World from Kernel Session {{session.SessionId}}"
	}
	""";
	await session.UploadFileAsync("msg.json", jsonContent);

	var pythonCode = """		
	import json

	message = ""
	with open("msg.json", "r") as f:
		data = json.load(f)
		message = data["message"]

	message
	""";

	var res = await session.ExecuteCodeAsync(pythonCode);
	var stringData = res.Results.FirstOrDefault(x => x.Data.ContainsKey("text/plain"));
	Console.WriteLine(stringData?.Data["text/plain"]);
}

