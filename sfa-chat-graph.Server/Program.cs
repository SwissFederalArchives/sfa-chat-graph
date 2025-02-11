using OpenAI;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Models;
using System.ClientModel;
using System.Collections.Frozen;
using VDS.RDF.Storage.Management;
using AwosFramework.Generators.FunctionCalling;
using sfa_chat_graph.Server.Utils;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var server = new OntotextStorageServer("http://localhost:7200");
var store = (OntotextStorage)await server.GetStoreAsync("Movies");
await store.ChangeGraphAsync("http://neo4j.org/movies/");

builder.Services.AddSingleton<IAsyncStorageServer>(server);
builder.Services.AddSingleton<OntotextStorage>(store);
builder.Services.AddSingleton<IGraphRag>(store);
var client = new OpenAIClient(System.Environment.GetEnvironmentVariable("OPENAI_KEY"));
builder.Services.AddSingleton<OpenAIClient>(client);
builder.Services.AddScoped(x => x.GetRequiredService<OpenAIClient>().GetChatClient("gpt-4o"));
builder.Services.AddScoped(x => x.AsIParentResolver());
builder.Services.AddScoped<FunctionCallRegistry>();

builder.Services.AddControllers()
	.AddJsonOptions(opts =>
	{
		opts.JsonSerializerOptions.Converters.Add(new SparqlStarConverter());
		opts.JsonSerializerOptions.Converters.Add(new ApiMessageConverter());
	});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapSwagger();
app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
