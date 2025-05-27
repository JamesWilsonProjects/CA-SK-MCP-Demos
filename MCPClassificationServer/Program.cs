using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register the MCP server: prompts from this assembly over STDIO
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
public static class ClassificationPrompts
{
    [McpServerTool, Description("Reads a citizen inquiry and returns exactly one of: Driver's License, Public Works, Property Tax, Passport Services, Other")]
    public static string DetermineCategory([Description("The raw inquiry text")] string inquiry)
    {
        // This string becomes the prompt text sent to Azure OpenAI
        return $@"
        User Inquiry: {inquiry}

        Reply with exactly one label (and nothing else):
        Driver's License
        Public Works
        Property Tax
        Passport Services
        Other
        ";
    }
}
