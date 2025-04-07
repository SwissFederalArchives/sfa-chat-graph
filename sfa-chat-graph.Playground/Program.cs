using AngleSharp.Io;
using AwosFramework.ApiClients.Jupyter.Rest;
using AwosFramework.ApiClients.Jupyter.Rest.Models;
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



var jupyter = JupyterRestClient.GetRestClient("http://localhost:8888", "31b6ae941596a6cb1b11c6edb6344719e0c966e4e8ae73ff");
var dir = await jupyter.GetDirectoryAsync("");
foreach (var entry in dir.Content)
	Console.WriteLine($"{entry.Name}: {entry.Type}");