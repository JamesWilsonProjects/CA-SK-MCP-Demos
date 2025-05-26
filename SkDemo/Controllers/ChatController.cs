using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Memory;
using System.Net.Http.Json;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.AspNetCore.Mvc;
using SkDemo.Plugins;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly KernelSetupService _kernelService;

    public ChatController(KernelSetupService kernelService)
    {
        _kernelService = kernelService;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");

        // Use the Kernel property from KernelSetupService
        var kernel = _kernelService.Kernel;
        
        // Define systemPrompt and input variables
        string systemPrompt = "Provide a helpful response to the user query.";
        string input = request.Question;

        var reply = await kernel.InvokePromptAsync(
        $"{systemPrompt}\nUser: {input}",
        new(new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        }));

        // Convert FunctionResult to string
        string replyText = reply.ToString();
        // Cast IResult to IActionResult
        return new JsonResult(new ChatResponse(replyText));
    }
}