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
PREFIX ric: <https://www.ica.org/standards/RiC/ontology#>
SELECT ?year ?thema (SUM(?recordCount) AS ?count)
FROM <https://lindas.admin.ch/sfa/ais>
WHERE {
  {
    SELECT ?year ?thema (COUNT(?record) AS ?recordCount)
    WHERE {
      ?record a ric:Record ;
              ric:isAssociatedWithDate ?dateRange ;
              <https://schema.ld.admin.ch/thema> ?thema .
      ?dateRange ric:beginningDate ?beginningDate .
      BIND(YEAR(xsd:dateTime(?beginningDate)) AS ?year)
      FILTER(?year >= 1900 && ?year <= 2025)
    }
    GROUP BY ?year ?thema
  }
}
GROUP BY ?year ?thema
ORDER BY ?year
""";

var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("sfa-chat-graph-test");

var activities = new DummyActivities();

var endpoint = new StardogEndpoint(loggerFactory, "https://lindas.admin.ch/query");
var rag = new GraphRag(endpoint, loggerFactory, database);
var queryRes = await rag.QueryAsync(query);
using var csv = File.Create("output.csv");
using var writer = new StreamWriter(csv);
var data = LLMFormatter.ToCSV(queryRes);
await writer.WriteAsync(data);

var jupyter = new JupyterClient("http://localhost:8888", null, loggerFactory);
using (var session = await jupyter.CreateKernelSessionAsync())
{
  var code = """
  import pandas as pd
  import re
  from IPython.display import display, JSON

  data = pd.read_csv('call_eWGgYqgeTxQgGqeLOh6OoCuV.csv', sep=';')

  def extract_place(thema):
      if pd.isnull(thema):
          return 'None'
      thema = thema.replace('\\n', '\n')
      match = re.search(r'Orte:\s*([^\n]+)', thema)
      if match:
          places = re.split(r';|,', match.group(1))
          return places[0].strip() if places[0].strip() else 'None'
      return 'None'

  clean_data = data.dropna(subset=['year', 'thema'])
  clean_data = clean_data[clean_data['year'].apply(lambda x: str(x).isdigit())]
  clean_data['year'] = clean_data['year'].astype(int)
  clean_data['count'] = clean_data['count'].astype(int)
  clean_data['place'] = clean_data['thema'].apply(extract_place)

  grouped = clean_data.groupby(['year', 'place'])['count'].sum().reset_index()

  # Saved
  grouped.to_csv('ais_records_by_place.csv', index=False)
  grouped.to_json('ais_records_by_place.json', orient='records')

  # Display as JSON
  json_data = grouped.to_dict(orient='records')
  display(JSON(json_data))
  """;

  await session.UploadFileAsync("call_eWGgYqgeTxQgGqeLOh6OoCuV.csv", data);
	var result = await session.ExecuteCodeAsync(code);
  var files = await session.ListFilesAsync();
  files = files.Where(x => x.Name != "call_eWGgYqgeTxQgGqeLOh6OoCuV.csv").ToArray();
	foreach (var file in files)
	{
    var content = await file.GetStringContentAsync();
    Console.WriteLine(file.Name);
    Console.WriteLine(content);
	}

  var frag1 = result.Results.First();
  Console.WriteLine(frag1.Data["text/plain"]);
}

class DummyActivities : IChatActivity
{
	public Task NotifyActivityAsync(string status, string detail = null, string trace = null)
	{
		Console.WriteLine($"{status}: {detail}");
		return Task.CompletedTask;
	}
}