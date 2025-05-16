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


// 1️⃣  Builder + LLM
var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        chatDeployment,
        endpoint,
        apiKey);

// 2️⃣  Build kernel
var kernel = builder.Build();

// 3️⃣  Register plugins (pass config values as needed)
kernel.Plugins.AddFromType<AdvicePlugin>();
kernel.Plugins.AddFromType<TodoPlugin>();
kernel.Plugins.AddFromType<HolidayPlugin>();

//  Semantic prompt template with tool list marker
const string systemPrompt = """
You are a helpful personal assistant. Use the available functions
to answer. If the user asks weather, call GetWeather; if holiday,
call IsPublicHoliday; if a todo, call AddTask.
""";


// 4️⃣  Chat loop
while (true)
{
    Console.Write("You: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var reply = await kernel.InvokePromptAsync(
        $"{systemPrompt}\nUser: {input}",
        new(new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        }));
    Console.WriteLine("Assistant: " + reply.GetValue<string>());
}

// -------------------- Plugins --------------------

public sealed class AdvicePlugin
{
    private readonly HttpClient _http = new();

    [KernelFunction, Description("Get a short piece of random advice")]
    public async Task<string> GetAdvice()
    {
        var dto = await _http.GetFromJsonAsync<AdviceDto>(
                      "https://api.adviceslip.com/advice")
                  ?? throw new Exception("No data");

        return $"🗣️ Advice: {dto.slip.advice}";
    }

    private sealed record AdviceDto(Slip slip);
    private sealed record Slip(string advice);
}

public class TodoPlugin
{
    private readonly List<string> _tasks = new();

    [KernelFunction, Description("Add a task to the in-memory list")]
    public string AddTask(
        [Description("Task text, e.g., renew tabs")] string text)
    {
        _tasks.Add(text);
        return $"✅ Added: {text}";
    }
}

public class HolidayPlugin
{
    private readonly HttpClient _http = new();

    [KernelFunction, Description("Check if a date (yyyy-MM-dd) is a US public holiday")]
    public async Task<string> IsPublicHoliday(string date)
    {
        if (!DateTime.TryParse(date, out var dt))
            return "❌ Invalid date (yyyy-MM-dd).";

        var list = await _http.GetFromJsonAsync<List<NagerDto>>(
            $"https://date.nager.at/api/v3/PublicHolidays/{dt.Year}/US") ?? new();

        var hit = list.FirstOrDefault(h => h.Date == dt.ToString("yyyy-MM-dd"));

        return hit is null
            ? $"ℹ️ {date} is not a federal holiday."
            : $"🎉 {date} is {hit.LocalName} ({hit.Name}).";
    }
    private record NagerDto(string Date, string LocalName, string Name);
}
