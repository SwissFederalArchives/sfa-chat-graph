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


var endpoint = new StardogEndpoint("https://lindas.admin.ch/query");
var graph = new GraphRag(endpoint);
var options = new JsonSerializerOptions
{
	WriteIndented = true
};

options.Converters.Add(new SparqlResultSetConverter());

string[] iris = ["https://health.ld.admin.ch/foph/covid19/ageGroup/total_population", "https://health.ld.admin.ch/foph/covid19/dimension/ageGroup"];
string[] preds = ["https://health.ld.admin.ch/foph/covid19/dimension/ageGroup"];

var res = await graph.QueryAsync(Queries.RelatedTriplesQuery(iris, preds));
Console.WriteLine(JsonSerializer.Serialize(res, options));