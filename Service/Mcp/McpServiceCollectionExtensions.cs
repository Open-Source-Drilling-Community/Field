using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace NORCE.Drilling.Field.Service.Mcp;

internal static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddLegacyMcpTool<TTool>(this IServiceCollection services)
        where TTool : class, IMcpTool
    {
        services.AddSingleton<TTool>();
        services.AddSingleton<IMcpTool>(sp => sp.GetRequiredService<TTool>());
        services.AddSingleton<McpServerTool>(sp =>
        {
            var tool = sp.GetRequiredService<TTool>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new LegacyMcpServerToolAdapter(tool, loggerFactory);
        });

        return services;
    }

    public static IServiceCollection AddLegacyMcpTool(
        this IServiceCollection services,
        string name,
        string description,
        JsonNode? inputSchema,
        Func<IServiceProvider, JsonObject?, CancellationToken, Task<JsonNode?>> invokeAsync)
    {
        services.AddSingleton<IMcpTool>(sp => new DelegateMcpTool(name, description, inputSchema, arguments => invokeAsync(sp, arguments.Arguments, arguments.CancellationToken)));
        services.AddSingleton<McpServerTool>(sp =>
        {
            var tools = sp.GetServices<IMcpTool>();
            IMcpTool tool = null!;
            foreach (var candidate in tools)
            {
                if (candidate.Name == name)
                {
                    tool = candidate;
                }
            }
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new LegacyMcpServerToolAdapter(tool, loggerFactory);
        });

        return services;
    }

    private sealed class DelegateMcpTool : IMcpTool
    {
        private readonly Func<(JsonObject? Arguments, CancellationToken CancellationToken), Task<JsonNode?>> _invokeAsync;

        public DelegateMcpTool(
            string name,
            string description,
            JsonNode? inputSchema,
            Func<(JsonObject? Arguments, CancellationToken CancellationToken), Task<JsonNode?>> invokeAsync)
        {
            Name = name;
            Description = description;
            InputSchema = inputSchema;
            _invokeAsync = invokeAsync;
        }

        public string Name { get; }

        public string Description { get; }

        public JsonNode? InputSchema { get; }

        public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken)
        {
            return _invokeAsync((arguments, cancellationToken));
        }
    }
}
