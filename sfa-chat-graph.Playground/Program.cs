using AngleSharp.Io;
using AwosFramework.ApiClients.Jupyter.Client;
using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models;
using AwosFramework.ApiClients.Jupyter.Utils;
using AwosFramework.ApiClients.Jupyter.WebSocket;
using AwosFramework.ApiClients.Jupyter.WebSocket.Terminal.Protocol;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.DataAnnotations;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;
using sfa_chat_graph.Server.RDF;
using sfa_chat_graph.Server.RDF.Endpoints;
using sfa_chat_graph.Server.Utils;
using SfaChatGraph.Server.RDF;
using System.Buffers.Text;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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

var endpoint = new StardogEndpoint("https://lindas.admin.ch/query");
var client = new GraphRag(endpoint);
var graphs = await client.ListGraphsAsync();