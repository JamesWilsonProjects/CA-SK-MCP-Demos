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
var bingConnectionId    = configuration["BingConnectionId"]!;

// 2. Create the client exactly as in quickstart
PersistentAgentsClient client = new(projectEndpoint, new DefaultAzureCredential());

// 3. Define Bing grounding tool
var bingTool = new BingGroundingToolDefinition(
    new BingGroundingSearchToolParameters(
        new[]
        {
            new BingGroundingSearchConfiguration(bingConnectionId)
        }
    )
);

// 4. Provision the agent
PersistentAgent agent = client.Administration.CreateAgent(
    model:        modelDeploymentName,
    name:         "CA Services Bot",
    instructions: @"
        You are a conversational California services assistant with perfect recall.
        **Always** include the official URL for any service or form you mention *in your very first answer*.
        For every user query—even follow-ups—immediately perform a fresh
        Bing grounding search (using `site:ca.gov` plus the question), then
        return up to 3 official CA.gov links (URLs under `/departments/.../services/...` or equivalent),
        each with a one-sentence summary. Do NOT answer from memory alone.",
    tools: new ToolDefinition[]
    {
        new BingGroundingToolDefinition(
            new BingGroundingSearchToolParameters(
                new[] { new BingGroundingSearchConfiguration(bingConnectionId) }
            )
        )
    }
);


// 5. Create the thread once at startup
PersistentAgentThread thread = client.Threads.CreateThread();

Console.WriteLine($"Agent ID: {agent.Id}");
Console.WriteLine($"Thread ID: {thread.Id}");

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

// 6. Cleanup: delete agent on shutdown
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

// 7. Chat endpoint
app.MapPost("/api/chat", (ChatRequest req) =>
{
    try
    {
        Console.WriteLine($"[Chat] Received question: {req.Question}");

        // Post the user's message
        client.Messages.CreateMessage(thread.Id, MessageRole.User, req.Question);

        // Invoke the agent run
        ThreadRun run = client.Runs.CreateRun(
            thread.Id,
            agent.Id,
            additionalInstructions: "For this user message, completely ignore prior answers and immediately perform a fresh Bing grounding search using site:ca.gov plus the user's question. Answer only based on the new search results."
        );
        Console.WriteLine($"[Chat] Run started: ID={run.Id}, status={run.Status}");

        // Poll until completion or failure
        do
        {
            Thread.Sleep(500);
            run = client.Runs.GetRun(thread.Id, run.Id);
            Console.WriteLine($"[Chat] Polling: status={run.Status}");
        }
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress
            || run.Status == RunStatus.RequiresAction
        );

        // Handle failure
        if (run.Status == RunStatus.Failed)
        {
            var error = run.LastError?.Message ?? "Unknown error";
            Console.Error.WriteLine($"[Chat] Run failed: {error}");
            return Results.Json(new ChatResponse($"⚠️ Agent run failed: {error}"));
        }

        // ─── RUN DEBUG ───────────────────────────────────────────────
        Console.WriteLine("──── Run Debug ────");
        Console.WriteLine($"Instructions: {run.Instructions}");
        Console.WriteLine($"Tools: {string.Join(", ", run.Tools.Select(t => t.GetType().Name))}");
        Console.WriteLine($"RequiredActions: {string.Join(", ", run.RequiredActions.Select(a => a.GetType().Name))}");
        if (run.Usage != null)
        {
            Console.WriteLine($"Usage: PromptTokens={run.Usage.PromptTokens}, CompletionTokens={run.Usage.CompletionTokens}");
        }
        Console.WriteLine("───────────────────");


        // 7d. Fetch all messages in the thread
        var messages = client.Messages.GetMessages(thread.Id)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        // ─── FULL DETAILED HISTORY DUMP ─────────────────────────────────
        Console.WriteLine("──── Full conversation history ────");
        foreach (var msg in messages)
        {
            Console.WriteLine($"[{msg.CreatedAt:u}] [{msg.Role}]");
            foreach (var content in msg.ContentItems)
            {
                if (content is MessageTextContent txt)
                {
                    Console.WriteLine($"   • Text      : {txt.Text}");
                }
                else if (content is MessageImageFileContent img)
                {
                    Console.WriteLine($"   • ImageFile : FileId={img.FileId}");
                }
                else
                {
                    Console.WriteLine($"   • [Other content type: {content.GetType().Name}]");
                }
            }
            Console.WriteLine();
        }
        Console.WriteLine("───────────────────────────────────");

        // 7e. Extract only the last agent text response
        var agentTexts = messages
            .Where(m => m.Role == MessageRole.Agent)
            .SelectMany(m => m.ContentItems)
            .OfType<MessageTextContent>()
            .Select(t => t.Text)
            .ToList();

        string reply = agentTexts.Any()
            ? agentTexts.Last()
            : "⚠️ No response from agent.";

        Console.WriteLine($"[Chat] Sending reply: {reply}");
        return Results.Json(new ChatResponse(reply));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[Chat] ERROR: {ex}");
        return Results.Json(new ChatResponse($"⚠️ Error: {ex.Message}"));
    }
});

app.Run();

// DTOs
public record ChatRequest(string Question);
public record ChatResponse(string Reply);
