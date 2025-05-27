using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        // Register MCP tool server over STDIO, auto-discover tools in this assembly
        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();

[McpServerToolType]
public static class OfficeInfoTools
{
    [McpServerTool, Description(
      "Given a requestType, returns dummy office details. " +
      "Supported types: Driver's License, Public Works, Property Tax, Passport Services"
    )]
    public static string GetOfficeInformation(string requestType) => requestType switch
    {
        "Driver's License"   => "Driver’s License Office: Mon–Fri 8:00–16:30, 456 Elm St, (555)123-4567",
        "Public Works"       => "Public Works Dept: Mon–Fri 7:00–15:00, 789 Oak Ave, (555)234-5678",
        "Property Tax"       => "Property Tax Office: Mon–Fri 9:00–17:00, 101 Pine Blvd Rm 105, (555)345-6789",
        "Passport Services"  => "Passport Services: Mon–Fri 8:00–17:00, 123 Main St Rm 101, (555)678-9012",
        _                    => "General Information Center: Mon–Fri 9:00–17:00, 123 Main St Gnd, (555)000-0000"
    };

    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Echo: {message}";
}
