using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Memory;
using System.Net.Http.Json;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5002");
var app = builder.Build();

// Define a simple route
app.MapGet("/", () => "Welcome to the SkDemo Web App!");

// Serve static files if needed
app.UseDefaultFiles();
app.UseStaticFiles();

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

string endpoint = config["AzureOpenAI:Endpoint"]!;
string apiKey = config["AzureOpenAI:Key"]!;
string chatDeployment = config["AzureOpenAI:Deployment"]!;


// 1. Builder + LLM
var kernelBuilder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        chatDeployment,
        endpoint,
        apiKey);

// 2. Build kernel
var kernel = kernelBuilder.Build();

// 3. Register plugins
kernel.Plugins.AddFromType<AdvicePlugin>();
kernel.Plugins.AddFromType<TodoPlugin>();
kernel.Plugins.AddFromType<HolidayPlugin>();

//  Semantic prompt template with tool list marker
const string systemPrompt = """
You are a helpful personal assistant. Use the available functions
to answer. If the user asks weather, call GetWeather; if holiday,
call IsPublicHoliday; if a todo, call AddTask.
""";


// 4. Chat loop
app.MapPost("/ask", async (ChatRequest request) =>
{
    var input = request.Question;
    if (string.IsNullOrWhiteSpace(input))
        return Results.BadRequest("Question cannot be empty.");

    var reply = await kernel.InvokePromptAsync(
        $"{systemPrompt}\nUser: {input}",
        new(new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        }));

    var replyText = reply.GetValue<string>();
    Console.WriteLine($"[Debug] Reply from kernel: {replyText}");

    // return a JSON object with reply (and, if you want, a threadId)
    return Results.Ok(new
    {
      reply = replyText,
      threadId = Guid.NewGuid().ToString()  // or null, or your own conversation ID
    });
});

// Run the web app
app.Run();

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

    [KernelFunction, Description("Get all tasks from the in-memory list")]
    public string GetTasks()
    {
        if (_tasks.Count == 0)
            return "📝 No tasks found.";
            
        var tasks = string.Join("\n", _tasks.Select(t => $"• {t}"));
        return $"📝 Tasks:\n{tasks}";
    }

    [KernelFunction, Description("Remove a task from the in-memory list")]
    public string RemoveTask(
        [Description("Task text to remove, e.g., renew tabs")] string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "❌ Task text cannot be empty.";

        // Try exact match first
        if (_tasks.Remove(text))
            return $"🗑️ Removed: {text}";

        // Try partial match (case-insensitive)
        var matchingTask = _tasks.FirstOrDefault(t => 
            t.Contains(text, StringComparison.OrdinalIgnoreCase));
        
        if (matchingTask != null)
        {
            _tasks.Remove(matchingTask);
            return $"🗑️ Removed: {matchingTask}";
        }

        return $"❌ Task not found: {text}";
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

public class ChatRequest
{
    public string Question { get; set; } = string.Empty;
}
