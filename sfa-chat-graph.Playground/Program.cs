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
using MessagePack;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Endpoints;
using SfaChatGraph.Server.Services.ChatService;
using SfaChatGraph.Server.Utils;
using SfaChatGraph.Server.Utils.Json;
using SfaChatGraph.Server.Utils.MessagePack;
using System.Buffers.Text;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;

var loggerFactory = LoggerFactory.Create(builder =>
{
	builder.AddConsole();
	builder.SetMinimumLevel(LogLevel.Debug);
});



const string query = """
 SELECT ?obs ?station ?waterLevel WHERE {
  GRAPH <https://lindas.admin.ch/foen/hydro> {
    ?obs a <https://cube.link/Observation> ;
  	<https://environment.ld.admin.ch/foen/hydro/dimension/station> ?station ;
   	<https://environment.ld.admin.ch/foen/hydro/dimension/waterLevel> ?waterLevel .
  }
  FILTER(isNumeric(?waterLevel))
}
ORDER BY DESC(xsd:double(?waterLevel))
LIMIT 1
""";

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("sfa-chat-graph-test");

var activities = new DummyActivities();

var endpoint = new StardogEndpoint("https://lindas.admin.ch/query");
var rag = new GraphRag(endpoint, loggerFactory, database);
var queryRes = await rag.QueryAsync(query);
Console.WriteLine(LLMFormatter.ToCSV(queryRes));
var visRes = await rag.GetVisualisationResultAsync(queryRes, query);
Console.WriteLine(LLMFormatter.ToCSV(visRes));
client.DropDatabase("sfa-chat-graph-test");


class DummyActivities : IChatActivity
{
	public Task NotifyActivityAsync(string status, string detail = null, string trace = null)
	{
		Console.WriteLine($"{status}: {detail}");
		return Task.CompletedTask;
	}
}