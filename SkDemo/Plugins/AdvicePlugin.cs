namespace SkDemo.Plugins;

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

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