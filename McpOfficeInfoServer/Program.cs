using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);

// (Optional) Configure console logging for the server
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace; // log to stderr for all levels
});

// Add the MCP server and configure its transport and tools
builder.Services
    .AddMcpServer()                  // Add MCP server services
    .WithStdioServerTransport()      // Use STDIO transport (for local dev)
    .WithToolsFromAssembly();        // Auto-register tools marked in this assembly

await builder.Build().RunAsync();

[McpServerToolType]  // Marks this class as containing MCP tools
public static class OfficeInfoTools
{
    [McpServerTool, Description("Returns information about state offices including office hours, locations, languages spoken, and handicap access.")]
    public static string GetOfficeInformation(string officeName)
    {
        // In a real scenario, this could save to a database or route to a department.
        Console.WriteLine($"[Server] Inquiry received. Location = {officeName}");
        return $"User asked about '{officeName}' office info. This office is open from 9 AM to 5 PM, located at 123 Main St, and speaks English and Spanish. It is handicap accessible. For more details, please visit our website.";
    }

    // (Optional) Additional tools for demo, e.g., a basic echo:
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Echo: {message}";
}
