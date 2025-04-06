using OpenAI;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Models;
using System.ClientModel;
using System.Collections.Frozen;
using VDS.RDF.Storage.Management;
using AwosFramework.Generators.FunctionCalling;
using sfa_chat_graph.Server.Utils;
using Json.Schema.Generation.DataAnnotations;
using sfa_chat_graph.Server.RDF;
using sfa_chat_graph.Server.RDF.Endpoints;

DotNetEnv.Env.Load();
DataAnnotationsSupport.AddDataAnnotations();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var client = new OpenAIClient(System.Environment.GetEnvironmentVariable("OPENAI_KEY"));
builder.Services.AddSingleton<OpenAIClient>(client);
builder.Services.AddScoped(x => x.GetRequiredService<OpenAIClient>().GetChatClient("gpt-4o"));
builder.Services.AddScoped(x => x.AsIParentResolver());
builder.Services.AddScoped<FunctionCallRegistry>();

builder.Services.AddSingleton<ISparqlEndpoint>(new StardogEndpoint("https://lindas.admin.ch/query"));
builder.Services.AddSingleton<IGraphRag, GraphRag>();

builder.Services.AddControllers()
	.AddJsonOptions(opts =>
	{
		opts.JsonSerializerOptions.Converters.Add(new SparqlStarConverter());
		opts.JsonSerializerOptions.Converters.Add(new SparqlResultSetConverter());
		opts.JsonSerializerOptions.Converters.Add(new ApiMessageConverter());
		opts.JsonSerializerOptions.Converters.Add(new GraphConverter());
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
