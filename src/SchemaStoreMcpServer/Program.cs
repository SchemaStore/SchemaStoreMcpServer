using SchemaStoreMcpServer.BackgroundServices;
using SchemaStoreMcpServer.Services;

var builder = WebApplication.CreateBuilder(args);

// HTTP client for fetching schemas
builder.Services.AddHttpClient("SchemaStore", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SchemaStoreMcpServer/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Schema catalog service (singleton — holds in-memory catalog)
builder.Services.AddSingleton<ISchemaCatalogService, SchemaCatalogService>();

// Background service to refresh catalog periodically
builder.Services.AddHostedService<CatalogRefreshService>();

// MCP server with HTTP/SSE transport
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SchemaStoreMcpServer.Tools.SchemaTools>();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// MCP endpoint (SSE + JSON-RPC over HTTP)
app.MapMcp();

// Health check endpoint
app.MapHealthChecks("/health");

app.Run();
