namespace SkDemo.Plugins;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.ComponentModel;

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