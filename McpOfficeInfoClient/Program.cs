// Program.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

class Program
{
    static async Task Main()
    {
        // 1) Load each server’s EXE path from local config
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
            .Build();

        string classifyPath = config["ClassificationServer:Path"]!;
        string infoPath     = config["InfoServer:Path"]!;

        // 2) Spin up the classification server (prompts only)
        var classifyClient = await McpClientFactory.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions {
                    Name      = "Classifier",
                    Command   = classifyPath,
                    Arguments = Array.Empty<string>()
                }
            )
        );

        // 3) Spin up the info server (tools only)
        var infoClient = await McpClientFactory.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions {
                    Name      = "InfoSvc",
                    Command   = infoPath,
                    Arguments = Array.Empty<string>()
                }
            )
        );

        // 4) Build and configure the SK kernel with Azure OpenAI
        var aoai = config.GetSection("AzureOpenAI");
        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                aoai["Deployment"]!,
                aoai["Endpoint"]!,
                aoai["Key"]!
            )
            .Build();

        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // 5) Discover *and register* server‐side prompts & tools as Kernel functions
        var promptFuncs = (await classifyClient.ListPromptsAsync())
            .Select(p => p.AsKernelFunction());
        var toolFuncs   = (await infoClient.ListToolsAsync())
            .Select(t => t.AsKernelFunction());
        kernel.Plugins.AddFromFunctions(
            "McpFunctions",
            promptFuncs.Concat(toolFuncs)
        );

        // 6) Configure auto‐invoke behavior
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature            = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.AutoInvoke
        };

        // 7) Prime the chat as an “agent” that knows about its functions
        var systemPrompt = @"
You are a citizen–government assistant with access to registered functions.
When given a user inquiry, you should:
  1) Call the classification function to get back a single department label.
  2) If that label is not 'Other', call the office‐info function with that label.
Return *only* the final office information text or a brief apology if no department applies.";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        // 8) REPL: ask, process, and print
        Console.WriteLine("Ask your question (or 'exit' to quit):");
        while (true)
        {
            Console.Write("> ");
            var inquiry = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(inquiry) ||
                inquiry.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            chatHistory.AddUserMessage(inquiry);

            // ONE call: the LLM inspects its available functions, invokes them in order,
            // and returns the final office information text.
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                settings,
                kernel
            );

            chatHistory.AddAssistantMessage(response.Content!);
            Console.WriteLine("\n" + response.Content! + "\n");
        }

        Console.WriteLine("Goodbye!");
    }
}
