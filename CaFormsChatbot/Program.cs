using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Linq;

// 1. Build host & load console-snippet config
var builder = WebApplication.CreateBuilder(args);
IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var projectEndpoint     = configuration["ProjectEndpoint"]!;
var modelDeploymentName = configuration["ModelDeploymentName"]!;

// 2. Create the client exactly as in quickstart
PersistentAgentsClient client = new(projectEndpoint, new DefaultAzureCredential());

// 3. Provision the agent (console snippet verbatim)
PersistentAgent agent = client.Administration.CreateAgent(
    model:        modelDeploymentName,
    name:         "My Test Agent",
    instructions: "You politely help with math questions. Use the code interpreter tool when asked to visualize numbers.",
    tools:        new[] { new CodeInterpreterToolDefinition() }
);

// 4. *** Create the thread once at startup ***
PersistentAgentThread thread = client.Threads.CreateThread();

Console.WriteLine($"Agent ID: {agent.Id}");
Console.WriteLine($"Thread ID: {thread.Id}");

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

// 5. Cleanup: delete agent on shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("🗑 Deleting agent...");
    try
    {
        client.Administration.DeleteAgent(agent.Id);
        Console.WriteLine("✅ Agent deleted");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"⚠️ Failed to delete agent: {ex.Message}");
    }
});

// 6. Chat endpoint — only the question is passed in
app.MapPost("/api/chat", (ChatRequest req) =>
{
    // 6a. Post the user’s question
    client.Messages.CreateMessage(
        thread.Id,
        MessageRole.User,
        req.Question
    );

    // 6b. Invoke the agent (verbatim from snippet)
    ThreadRun run = client.Runs.CreateRun(
        thread.Id,
        agent.Id,
        additionalInstructions: "Please address the user as Jane Doe. The user has a premium account."
    );

    // 6c. Poll for completion
    do
    {
        Thread.Sleep(TimeSpan.FromMilliseconds(500));
        run = client.Runs.GetRun(thread.Id, run.Id);
    }
    while (run.Status == RunStatus.Queued
        || run.Status == RunStatus.InProgress
        || run.Status == RunStatus.RequiresAction
    );

    // 6d. Fetch all messages (text & images) exactly as snippet does
    Pageable<PersistentThreadMessage> messages = client.Messages.GetMessages(
        threadId: thread.Id,
        order:    ListSortOrder.Ascending
    );

    // 6e. Collect only the text bits into one string for the web UI
    string reply = string.Join("\n", messages
        .SelectMany(m => m.ContentItems)
        .OfType<MessageTextContent>()
        .Select(t => t.Text)
    );

    return Results.Json(new ChatResponse(reply));
});

app.Run();

// ─── DTO ─────────────────────────────────────────────────────────

public record ChatRequest(string Question);
public record ChatResponse(string Reply);
