using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

using System.ComponentModel;
using Microsoft.Extensions.AI;

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
    .WithPromptsFromAssembly()      // Auto-register prompts marked in this assembly
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

    [McpServerTool, Description("Returns information about state offices including office hours, locations, languages spoken, and handicap access.")]
    public static string GetOfficeManagerByLocation(string officeLocation)
    {
        // In a real scenario, this could save to a database or route to a department.
        Console.WriteLine($"[Server] Inquiry received. Location = {officeLocation}");
        return $"User asked about '{officeLocation}' office manager. This office is managed by John Doe, who is available from 9 AM to 5 PM and can be reached at (555) 123-4567. For more details, please visit our website.";
    }

    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Echo: {message}";
}

[McpServerPromptType]
public static class PromptTools
{
    [McpServerPrompt, Description("Creates a prompt to summarize the provided message.")]
    public static ChatMessage Summarize([Description("The content to summarize")] string content) =>
        new(ChatRole.User, $"Please summarize this content into a single sentence: {content}");

    [McpServerPrompt, Description("Creates a prompt to summarize the provided message.")]
    public static ChatMessage DetermineIntent([Description("Determines the inquiry intent and categorizes it to the correct category")] string content)
    {
        string promptTemplate = @"Determine the primary intent of the following citizen inquiry and categorize it into one of the following categories: Driver's License, Public Works, Property Tax, Other.

        Citizen Inquiry: {{$inquiry}}

        Intent:";
        return new ChatMessage(ChatRole.User, promptTemplate.Replace("{{$inquiry}}", content));
    }
}
