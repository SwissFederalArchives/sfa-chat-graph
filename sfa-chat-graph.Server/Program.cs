using OpenAI;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Models;
using System.ClientModel;
using VDS.RDF.Storage.Management;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var server = new OntotextStorageServer("http://localhost:7200");
var store = (OntotextStorage)await server.GetStoreAsync("TestDB");
await store.ChangeGraphAsync("http://ld.admin.ch/stapfer-ai");

builder.Services.AddSingleton<IAsyncStorageServer>(server);
builder.Services.AddSingleton<OntotextStorage>(store);
builder.Services.AddSingleton<IGraphRag>(store);
var client = new OpenAIClient(System.Environment.GetEnvironmentVariable("OPENAI_KEY"));
builder.Services.AddSingleton<OpenAIClient>(client);
builder.Services.AddScoped(x => x.GetRequiredService<OpenAIClient>().GetChatClient("gpt-4o"));

builder.Services.AddControllers()
	.AddJsonOptions(opts =>
	{
		opts.JsonSerializerOptions.Converters.Add(new SparqlStarConverter());
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
