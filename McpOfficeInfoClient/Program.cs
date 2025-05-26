using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net.Http.Json;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using Microsoft.Extensions.Logging;


var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

string endpoint = config["AzureOpenAI:Endpoint"]!;
string apiKey = config["AzureOpenAI:Key"]!;
string chatDeployment = config["AzureOpenAI:Deployment"]!;
string serverExePath = config["McpOfficeInfoServer:ExecutablePath"]!;

//  Builder + LLM
var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        chatDeployment,
        endpoint,
        apiKey);

//  Build kernel
var kernel = builder
    .Build();

ChatHistory chatHistory = [];
IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Create an MCP client to connect to the server (e.g., launching a local MCP server process)
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "MyMcpServer",               // Just a label for the server
        Command = serverExePath,
        Arguments = new string[] { /* any args if needed */ }
    })
);

// Retrieve the list of tools and prompts from the MCP server
var toolsList = await mcpClient.ListToolsAsync().ConfigureAwait(false);
Console.WriteLine("Available Tools:");

foreach (var toolInfo in toolsList)
{
    Console.WriteLine($"{toolInfo.Name}: {toolInfo.Description}");
}
Console.WriteLine("***********************************************************");
var promptsList = await mcpClient.ListPromptsAsync().ConfigureAwait(false);
Console.WriteLine("\nAvailable Prompts:");
foreach (var promptInfo in promptsList)
{
    Console.WriteLine($"{promptInfo.Name}: {promptInfo.Description}");
}
Console.WriteLine("***********************************************************");

// Convert MCP tools to SK functions and register them as a plugin
var kernelFunctions = toolsList.Select(tool => tool.AsKernelFunction());
kernel.Plugins.AddFromFunctions("McpToolsPlugin", kernelFunctions);

var settings = new OpenAIPromptExecutionSettings
{
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior
        .Auto(autoInvoke: true),
};


// Define the Semantic Function using Handlebars prompt template
var intentPrompt =
"""
You are a government assistant with access to external tools registered in your system.
When a citizen’s inquiry requires external data (office hours, locations, contacts, etc.), call the appropriate tool; otherwise answer directly.

Citizen Inquiry: {{$inquiry}}
""";
chatHistory.AddSystemMessage(intentPrompt);


var getIntentFunction = kernel.CreateFunctionFromPrompt(
    intentPrompt,
    executionSettings: settings);


Console.WriteLine("Welcome to the Intelligent Citizen Inquiry System!");
Console.WriteLine("Type your inquiry and press Enter (type 'exit' to quit).");

string inquiry;
while ((inquiry = Console.ReadLine())?.ToLower() != "exit")
{
    if (!string.IsNullOrWhiteSpace(inquiry))
    {
        chatHistory.AddUserMessage(inquiry);

        Console.WriteLine("Processing your inquiry...");

        // var response = await kernel.InvokeAsync(getIntentFunction, new KernelArguments { ["inquiry"] = inquiry });
        // var response = await chatService.GetChatMessageContentAsync(
        //     chatHistory,
        //     settings,
        //     kernel);
        // Get the chat message content
        ChatMessageContent results = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            kernel
        );

        chatHistory.AddAssistantMessage(results.Content!);

        string intent = results.Content!;

        if (!string.IsNullOrEmpty(intent))
        {
            Console.WriteLine($"Identified Intent: {intent}");
        }
        else
        {
            Console.WriteLine("Could not clearly identify the intent. Please rephrase.\n");
        }
    }
    else
    {
        Console.WriteLine("Please enter your inquiry.\n");
    }
}

Console.WriteLine("Exiting the system. Thank you!");
