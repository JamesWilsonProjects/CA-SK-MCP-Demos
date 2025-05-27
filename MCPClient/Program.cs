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
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;



var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

string endpoint = config["AzureOpenAI:Endpoint"]!;
string apiKey = config["AzureOpenAI:Key"]!;
string chatDeployment = config["AzureOpenAI:Deployment"]!;
string classificationServer = config["ClassificationServer:Path"]!;
string infoServer = config["InfoServer:Path"]!;


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
var classificationClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "ClassificationServer",               // Just a label for the server
        Command = classificationServer,
        Arguments = new string[] { /* any args if needed */ }
    })
);
var infoClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Name = "InfoServer",               // Just a label for the server
        Command = infoServer,
        Arguments = new string[] { /* any args if needed */ }
    })
);

// Retrieve the list of tools from the MCP server
var classificationToolsList = await classificationClient.ListToolsAsync().ConfigureAwait(false);
var infoToolsList = await infoClient.ListToolsAsync().ConfigureAwait(false);
var allToolsList = classificationToolsList.Concat(infoToolsList).ToList();
Console.WriteLine("Available Tools:");

foreach (var toolInfo in allToolsList)
{
    Console.WriteLine($"{toolInfo.Name}: {toolInfo.Description}");
}


// Convert MCP tools to SK functions and register them as a plugin
var kernelFunctions = allToolsList.Select(tool => tool.AsKernelFunction());
kernel.Plugins.AddFromFunctions("McpToolsPlugin", kernelFunctions);

var settings = new OpenAIPromptExecutionSettings
{
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior
        .Auto(autoInvoke: true),
};


// Define the Semantic Function using Handlebars prompt template
var servicePrompt = """
{{#useFunctions}}
You are an AI government assistant. You have access to external tools for office hours, locations, contacts, and other civic data.
When a citizen’s inquiry requires external data, call the appropriate tool; otherwise answer directly.
{{/useFunctions}}

Citizen Inquiry:
{{inquiry}}
""";

chatHistory.AddSystemMessage(servicePrompt);


var handlebarsFactory = new HandlebarsPromptTemplateFactory();

// 2) Create your prompt‐function with explicit format + factory
var getServiceFunction = kernel.CreateFunctionFromPrompt(
    promptTemplate:    servicePrompt,
    executionSettings: new[] { settings },
    functionName:      "HandleCitizenInquiry",
    description:       "Routes a citizen inquiry via MCP tools or answers directly",
    templateFormat:    "handlebars",          // <— must match the engine name
    promptTemplateFactory: handlebarsFactory // <— your factory instance
);


Console.WriteLine("Welcome to the Intelligent Citizen Inquiry System!");
Console.WriteLine("Type your inquiry and press Enter (type 'exit' to quit).");

string inquiry;
while ((inquiry = Console.ReadLine())?.ToLower() != "exit")
{
    if (!string.IsNullOrWhiteSpace(inquiry))
    {
        chatHistory.AddUserMessage(inquiry);

        Console.WriteLine("Processing your inquiry...");

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