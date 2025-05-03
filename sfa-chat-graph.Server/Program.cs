using OpenAI;
using System.ClientModel;
using System.Collections.Frozen;
using VDS.RDF.Storage.Management;
using AwosFramework.Generators.FunctionCalling;
using SfaChatGraph.Server.Utils;
using Json.Schema.Generation.DataAnnotations;
using SfaChatGraph.Server.RDF;
using SfaChatGraph.Server.RDF.Endpoints;
using SfaChatGraph.Server.Services.CodeExecutionService;
using SfaChatGraph.Server.Services.CodeExecutionService.Jupyter;
using SfaChatGraph.Server.Services.ChatService;
using SfaChatGraph.Server.Services.ChatService.OpenAI;
using VDS.RDF;
using StackExchange.Redis;
using MongoDB.Driver;
using SfaChatGraph.Server.Utils.Json;
using SfaChatGraph.Server.Services.ChatHistoryService;
using SfaChatGraph.Server.Services.ChatService.Events;
using SfaChatGraph.Server.Services.Cache;
using SfaChatGraph.Server.Models;
using SfaChatGraph.Server.Utils.ServiceCollection;
using MongoDB.Driver.GridFS;
using SfaChatGraph.Server.Services.ChatHistoryService.Cached;
using SfaChatGraph.Server.Services.ChatHistoryService.MongoDB.V2;
using SfaChatGraph.Server.Versioning;
using HarmonyLib;

var harmony = new Harmony("sfa-chat-graph");
harmony.PatchAll(typeof(Program).Assembly);

DotNetEnv.Env.Load();
DataAnnotationsSupport.AddDataAnnotations();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped(x => x.AsIParentResolver());
builder.Services.AddScoped<FunctionCallRegistry>();
builder.Services.Configure<JupyterCodeExecutionServiceOptions>(builder.Configuration.GetSection("JupyterOptions"));
builder.Services.AddSingleton<ICodeExecutionService, JupyterCodeExecutionService>();
builder.Services.AddSingleton<ISparqlEndpoint>(new StardogEndpoint("https://lindas.admin.ch/query"));
builder.Services.AddScoped<IGraphRag, GraphRag>();
builder.Services.AddSingleton<ChatCodeService>();
builder.Services.AddSingleton<ChatServiceEventService>();
builder.Services.AddFromConfig<IChatService>(builder.Configuration.GetSection("AiConfig"));
builder.Services.AddVersioning();

var redis = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrEmpty(redis) == false)
{
	builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redis));
	builder.Services.AddScoped<IDatabaseAsync>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
}

builder.Services.AddScoped<IMongoClient>(x => new MongoClient(x.GetRequiredService<IConfiguration>().GetConnectionString("Mongo")));
builder.Services.AddScoped<IMongoDatabase>(x =>
{
	var client = x.GetRequiredService<IMongoClient>();
	var config = x.GetRequiredService<IConfiguration>();
	var url = new MongoUrl(config.GetConnectionString("Mongo"));
	return client.GetDatabase(url.DatabaseName);
});

builder.Services.AddFromConfig<IAppendableCache<Guid, IApiMessage>>(builder.Configuration.GetSection("Cache"));
builder.Services.AddSingleton<IChatHistoryServiceCache, AppendableCacheChatHistoryServiceCache>();
builder.Services.AddKeyedScoped<IChatHistoryService>("Storage", (sp, _) => VersioningService.GetLatestVersion<IChatHistoryService>(sp));
builder.Services.AddScoped<IChatHistoryService, CachedChatHistoryService>();

builder.Services.AddControllers()
	.AddJsonOptions(opts =>
	{
		opts.JsonSerializerOptions.Converters.Add(new SparqlResultSetConverter());
		opts.JsonSerializerOptions.Converters.Add(new ApiMessageConverter());
		opts.JsonSerializerOptions.Converters.Add(new GraphConverter<Graph>());
		opts.JsonSerializerOptions.Converters.Add(new GraphConverter<IGraph>(() => new Graph()));
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
app.UseWebSockets();

app.MapSwagger();
app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
