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
[McpServerTool, Description("Returns information about state offices based on the type of request. Supported requestType values include Driver's License, Property Tax, Passport Services, Public Works, etc.")]
public static string GetOfficeInformation(string requestType)
{
    Console.WriteLine($"[Server] Inquiry received. requestType = {requestType}");

        return requestType switch
        {
            "Driver's License" => "Driver’s License Office\n" +
                               "- Hours: Mon–Fri 8:00 AM–4:30 PM\n" +
                               "- Address: 456 Elm Street, Suite 200\n" +
                               "- Phone: (555) 123-4567\n" +
                               "- Services: New licenses, renewals, written & road tests\n" +
                               "- Languages: English, Spanish\n" +
                               "- Accessibility: Wheelchair accessible",
            "Public Works" => "Department of Public Works\n" +
                               "- Hours: Mon–Fri 7:00 AM–3:00 PM\n" +
                               "- Address: 789 Oak Avenue\n" +
                               "- Phone: (555) 234-5678\n" +
                               "- Services: Road maintenance, waste collection, streetlights\n" +
                               "- Languages: English\n" +
                               "- Accessibility: Curb-cut ramps at all entrances",
            "Passport Services" => "Passport Service Hotline\n" +
                                 "- Hours: Mon–Fri 8:00 AM–5:00 PM\n" +
                                 "- Address: 123 Main Street, Room 101\n" +
                                 "- Phone: (555) 678-9012\n" +
                                 "- Services: Report potholes, track repair status\n" +
                                 "- Languages: English, Spanish\n" +
                                 "- Accessibility: Hearing assistance available",
            "Property Tax" => "County Property Tax Office\n" +
                               "- Hours: Mon–Fri 9:00 AM–5:00 PM\n" +
                               "- Address: 101 Pine Boulevard, Room 105\n" +
                               "- Phone: (555) 345-6789\n" +
                               "- Services: Tax assessments, payments, appeals\n" +
                               "- Languages: English, Spanish, Vietnamese\n" +
                               "- Accessibility: Elevator access available",
            _ => "General Information Center\n" +
                               "- Hours: Mon–Fri 9:00 AM–5:00 PM\n" +
                               "- Address: 123 Main Street, Ground Floor\n" +
                               "- Phone: (555) 000-0000\n" +
                               "- Services: All other inquiries, referrals to specific departments\n" +
                               "- Languages: English, Spanish\n" +
                               "- Accessibility: Fully accessible",
        };
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

    [McpServerPrompt, Description("Returns information about state offices based on the type of request. Supported request types include Driver's License, Property Tax, Passport Services, Public Works, etc.")]
    public static ChatMessage DetermineIntent(
            [Description("Returns information about state offices based on the type of request. Supported request types include Driver's License, Property Tax, Passport Services, Public Works, etc.")] string content)
        {
            var template = @"
        You are a government assistant.  Read the citizen’s inquiry below and respond with exactly one of these options (and nothing else):
        Driver's License
        Public Works
        Property Tax
        Passport Services
        Other

        Citizen Inquiry:
        {{$inquiry}}";
            return new(ChatRole.User, template.Replace("{{$inquiry}}", content));
        }
}
