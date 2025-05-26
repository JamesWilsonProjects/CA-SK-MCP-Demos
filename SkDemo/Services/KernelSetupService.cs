using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SkDemo.Plugins;
using SkDemo;
using Microsoft.Extensions.Logging;


public class KernelSetupService
{
    public Kernel Kernel { get; }

    private readonly ILogger<KernelSetupService> _logger;

    public KernelSetupService(IConfiguration configuration, ILogger<KernelSetupService> logger)
    {
        _logger = logger;
        _logger.LogInformation("Initializing KernelSetupService...");

        // Load Azure OpenAI settings from configuration
        string endpoint = configuration["AzureOpenAI:Endpoint"]!;
        string apiKey = configuration["AzureOpenAI:Key"]!;
        string chatDeployment = configuration["AzureOpenAI:Deployment"]!;

        // 1. Builder + LLM
        var builder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                chatDeployment,
                endpoint,
                apiKey);

        // 2. Build kernel
        var kernel = builder.Build();

        // 3. Register plugins using KernelExtensions
        // Ensure plugins are added only once
        if (!kernel.Plugins.Any(p => p.GetType() == typeof(AdvicePlugin)))
        {
            _logger.LogInformation("Registering AdvicePlugin...");
            kernel.Plugins.AddFromType<AdvicePlugin>();
        }
        if (!kernel.Plugins.Any(p => p.GetType() == typeof(TodoPlugin)))
        {
            _logger.LogInformation("Registering TodoPlugin...");
            kernel.Plugins.AddFromType<TodoPlugin>();
        }
        if (!kernel.Plugins.Any(p => p.GetType() == typeof(HolidayPlugin)))
        {
            _logger.LogInformation("Registering HolidayPlugin...");
            kernel.Plugins.AddFromType<HolidayPlugin>();
        }

        // 4. Register semantic functions
        var promptFile = Path.Combine(
            Directory.GetCurrentDirectory(),
            "SemanticFunctions",
            "summarize.txt");
        var promptTemplate = File.ReadAllText(promptFile);

        var summarizeFunc = kernel.CreateFunctionFromPrompt(
            promptTemplate,
            executionSettings: new OpenAIPromptExecutionSettings
            {
                MaxTokens             = 200,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            },
            functionName:    "summarize",
            description:     "Summarize the given text into a concise paragraph"
        );

        var summarizePlugin = KernelPluginFactory.CreateFromFunctions("SummarizePlugin", new[] { summarizeFunc });
        kernel.Plugins.Add(summarizePlugin);


        this.Kernel = kernel;
    }
}