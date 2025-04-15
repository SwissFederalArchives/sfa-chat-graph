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

var token = "371e778dcd2efdadab0e25fda1989e361d03d160f7aa970a";
var jupyter = new JupyterClient("http://localhost:8888", null, loggerFactory);

using (var session = await jupyter.CreateKernelSessionAsync())
{
	var csvData = """
    Category,Count
    Bananas,23
    Apples,45
    Oranges,12
    Cherries,43
    """;
	await session.UploadFileAsync("data.csv", csvData);

	var pythonCode = """
	import pandas as pd
	import matplotlib.pyplot as plt
	import seaborn as sns
	from IPython.display import display

	# Load the data
	data = pd.read_csv('data.csv')
	# Create a bar plot
	plt.figure(figsize=(10, 6))
	sns.barplot(x='Category', y='Count', data=data, palette='viridis')
	plt.title('Fruit Count')
	plt.xlabel('Fruit Category')
	plt.ylabel('Count')
	plt.show()
	display(data)
	""";

	var res = await session.ExecuteCodeAsync(pythonCode);
	var stringData = res.Results.FirstOrDefault(x => x.Data.ContainsKey("text/plain"));
	var imgData = res.Results.FirstOrDefault(x => x.Data.ContainsKey("image/png"));
	if (imgData != null)
	{
		var img = imgData.Data["image/png"];
		await File.WriteAllBytesAsync("output.png", Convert.FromBase64String((string)img));
		Console.WriteLine("Image saved as output.png");
	}
	Console.WriteLine(stringData?.Data["text/plain"]);
}
