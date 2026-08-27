using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace NORCE.Drilling.Field.Service.Mcp;

public static class McpServiceCollectionExtensions
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
        services.AddSingleton<IMcpTool>(sp => new DelegateMcpTool(name, description, inputSchema ?? EmptyInputSchema(), DefaultOutputSchema(), InferBehavior(name), arguments => invokeAsync(sp, arguments.Arguments, arguments.CancellationToken)));
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
            JsonNode inputSchema,
            JsonNode outputSchema,
            McpToolBehavior behavior,
            Func<(JsonObject? Arguments, CancellationToken CancellationToken), Task<JsonNode?>> invokeAsync)
        {
            Name = name;
            Description = description;
            InputSchema = inputSchema;
            OutputSchema = outputSchema;
            Behavior = behavior;
            _invokeAsync = invokeAsync;
        }

        public string Name { get; }

        public string Description { get; }

        public JsonNode InputSchema { get; }

        public JsonNode OutputSchema { get; }

        public McpToolBehavior Behavior { get; }

        public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken)
        {
            return _invokeAsync((arguments, cancellationToken));
        }
    }

    private static JsonNode EmptyInputSchema() => JsonNode.Parse("""{"type":"object","additionalProperties":false}""")!;

    private static JsonNode DefaultOutputSchema() => JsonNode.Parse("""
    {"type":"object","properties":{"status":{"type":"integer","minimum":200,"maximum":299},"data":{}},"required":["status"],"additionalProperties":false}
    """)!;

    private static McpToolBehavior InferBehavior(string name)
    {
        string title = string.Join(' ', name.Split('_').Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        bool delete = name.Contains("delete", StringComparison.Ordinal);
        bool update = name.Contains("update", StringComparison.Ordinal);
        bool create = name.Contains("create", StringComparison.Ordinal);
        bool openWorld = name.Contains("convert_coordinates", StringComparison.Ordinal);
        return new McpToolBehavior(title, !(delete || update || create), delete || update, !(create), openWorld);
    }
}
