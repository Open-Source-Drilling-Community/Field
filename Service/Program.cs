using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using OSDC.Drilling.Field.Service;
using OSDC.Drilling.Field.Service.Managers;
using OSDC.Drilling.Field.Service.Mcp;
using OSDC.Drilling.Field.Service.Mcp.Tools;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

string externalConfigPath = builder.Configuration["FIELD_EXTERNAL_CONFIG"]
    ?? Path.Combine(SqlConnectionManager.HOME_DIRECTORY, "Field.Service.json");
builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);

// registering the manager of SQLite connections through dependency injection
builder.Services.AddSingleton(sp =>
    new SqlConnectionManager(
        $"Data Source={SqlConnectionManager.HOME_DIRECTORY}{SqlConnectionManager.DATABASE_FILENAME}",
        sp.GetRequiredService<ILogger<SqlConnectionManager>>()));


// serialization settings (using System.Json)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        JsonSettings.ApplyTo(options.JsonSerializerOptions);
    });

// serialize using short name rather than full names
builder.Services.AddSwaggerGen(config =>
{
    config.CustomSchemaIds(type => type.FullName);
});

builder.Services.Configure<McpHubOptions>(builder.Configuration.GetSection(McpHubOptions.SectionName));
builder.Services.AddHttpClient(nameof(McpHubRegistrationService));
builder.Services.AddHostedService<McpHubRegistrationService>();
builder.Services.AddHttpClient<IEarthCartographicProjectionClient, EarthCartographicProjectionClient>();
builder.Services.AddHttpClient<IEarthGeodesyClient, EarthGeodesyClient>();
builder.Services.AddScoped<FieldCoordinateConversionService>();

// MCP server registrations
var serverVersion = typeof(SqlConnectionManager).Assembly.GetName().Version?.ToString() ?? "1.0.0";

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "FieldService",
        Version = serverVersion
    };
    options.Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability()
    };
}).WithHttpTransport();

builder.Services.AddLegacyMcpTool<PingMcpTool>();
builder.Services.AddFieldRestMcpTools();

// end MCP server

var app = builder.Build();

// Resolve the database manager before the web host starts accepting requests so
// an unsupported schema or malformed database fails startup.
_ = app.Services.GetRequiredService<SqlConnectionManager>();
app.Logger.LogInformation("Field database initialization and validation completed.");

var basePath = "/field/api";
app.UsePathBase(basePath);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    string path = context.Request.Path.Value ?? string.Empty;
    if (path.Contains("/.well-known/oauth-protected-resource", System.StringComparison.OrdinalIgnoreCase) ||
        path.Contains("/.well-known/oauth-authorization-server", System.StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        string body = "{\"error\":\"oauth_not_configured\",\"error_description\":\"This MCP server does not require OAuth. Connect directly to the MCP endpoint.\",\"authentication\":\"none\"}";
        await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(body));
        return;
    }

    await next();
});

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

string relativeSwaggerPath = "/swagger/merged/swagger.json";
string fullSwaggerPath = $"{basePath}{relativeSwaggerPath}";
string customVersion = "Merged API Version 1";

var mergedDoc = SwaggerMiddlewareExtensions.ReadOpenApiDocument("wwwroot/json-schema/FieldMergedModel.json");
app.UseCustomSwagger(mergedDoc, relativeSwaggerPath);
app.UseSwaggerUI(c =>
{
    //c.SwaggerEndpoint("v1/swagger.json", "API Version 1");
    c.SwaggerEndpoint(fullSwaggerPath, customVersion);
});

app.UseCors(cors => cors
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .SetIsOriginAllowed(origin => true)
                        .AllowCredentials()
           );

app.MapMcp("/mcp");
app.MapMcpWebSocket("/mcp/ws");
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
