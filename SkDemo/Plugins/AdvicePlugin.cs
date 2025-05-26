namespace SkDemo.Plugins;

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

public class AdvicePlugin
{
    private readonly HttpClient _http = new();

    [KernelFunction, Description("Provide advice based on input")]
    public Task<string> GetAdviceAsync(string input)
    {
        return Task.FromResult($"Advice for {input}: Stay positive and keep learning!");
    }
}