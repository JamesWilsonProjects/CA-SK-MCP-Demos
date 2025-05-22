using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Memory;
using System.Net.Http.Json;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

string endpoint = config["AzureOpenAI:Endpoint"]!;
string apiKey = config["AzureOpenAI:Key"]!;
string chatDeployment = config["AzureOpenAI:Deployment"]!;

//  Builder + LLM
var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        chatDeployment,
        endpoint,
        apiKey);

//  Build kernel
var kernel = builder.Build();

// Define the Semantic Function using Handlebars prompt template
string promptTemplate = @"Determine the primary intent of the following citizen inquiry and categorize it into one of the following categories: Driver's License, Public Works, Property Tax, Other.

Citizen Inquiry: {{$inquiry}}

Intent:";

// This is our semantic function
var getIntentFunction = kernel.CreateFunctionFromPrompt(
    promptTemplate, 
    executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 100 });


Console.WriteLine("Welcome to the Intelligent Citizen Inquiry System!");
Console.WriteLine("Type your inquiry and press Enter (type 'exit' to quit).");

string inquiry;
while ((inquiry = Console.ReadLine())?.ToLower() != "exit")
{
    if (!string.IsNullOrWhiteSpace(inquiry))
    {
        Console.WriteLine("Processing your inquiry...");
        var result = await kernel.InvokeAsync(getIntentFunction, new KernelArguments { ["inquiry"] = inquiry });
        string intent = result.GetValue<string>()?.Trim();

        if (!string.IsNullOrEmpty(intent))
        {
            Console.WriteLine($"Identified Intent: {intent}");
            Console.WriteLine($"Suggestion: Routing to the {intent} department.\n");
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
