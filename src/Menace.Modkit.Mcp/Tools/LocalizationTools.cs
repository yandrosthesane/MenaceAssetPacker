using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Menace.Modkit.Mcp.Tools;

/// <summary>
/// MCP tools for multilingual localization support.
/// These are HTTP proxies that call the game's MCP server.
/// </summary>
[McpServerToolType]
public static class LocalizationTools
{
    private const string GameServerUrl = "http://127.0.0.1:7655";
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [McpServerTool(Name = "get_all_translations", Destructive = false)]
    [Description("Get translations for a localization key in ALL languages. Requires game to be running.")]
    public static async Task<string> GetAllTranslations(
        [Description("Localization category (e.g., 'weapons', 'skills', 'perks')")] string category,
        [Description("Localization key (e.g., 'weapons.assault_rifle.name')")] string key)
    {
        return await PostToGame("/localization/all", new { category, key });
    }

    [McpServerTool(Name = "get_translation", Destructive = false)]
    [Description("Get translation for a specific language. Requires game to be running.")]
    public static async Task<string> GetTranslation(
        [Description("Language (English, German, French, Russian, ChineseSimplified, ChineseTraditional, Japanese, Korean, Polish, Turkish)")] string language,
        [Description("Localization category")] string category,
        [Description("Localization key")] string key)
    {
        return await PostToGame("/localization/get", new { language, category, key });
    }

    [McpServerTool(Name = "list_localization_languages", Destructive = false)]
    [Description("List all available localization languages. Requires game to be running.")]
    public static async Task<string> ListLanguages()
    {
        return await FetchFromGame("/localization/languages");
    }

    [McpServerTool(Name = "list_localization_categories", Destructive = false)]
    [Description("List all localization categories for a language. Requires game to be running.")]
    public static async Task<string> ListCategories(
        [Description("Language to query")] string language)
    {
        return await PostToGame("/localization/categories", new { language });
    }

    [McpServerTool(Name = "list_localization_keys", Destructive = false)]
    [Description("List all localization keys in a category. Requires game to be running.")]
    public static async Task<string> ListKeys(
        [Description("Language to query")] string language,
        [Description("Category to list keys from")] string category,
        [Description("Optional filter substring")] string? filter = null,
        [Description("Maximum keys to return (default 100)")] int limit = 100)
    {
        return await PostToGame("/localization/keys", new { language, category, filter, limit });
    }

    [McpServerTool(Name = "localization_statistics", Destructive = false)]
    [Description("Get statistics about loaded localization data. Requires game to be running.")]
    public static async Task<string> GetStatistics()
    {
        return await FetchFromGame("/localization/statistics");
    }

    private static async Task<string> FetchFromGame(string endpoint)
    {
        try
        {
            var response = await HttpClient.GetAsync($"{GameServerUrl}{endpoint}");
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Game not running or MCP server not started",
                details = ex.Message
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            }, JsonOptions);
        }
    }

    private static async Task<string> PostToGame(string endpoint, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync($"{GameServerUrl}{endpoint}", content);
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = "Game not running or MCP server not started",
                details = ex.Message
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                error = ex.Message
            }, JsonOptions);
        }
    }
}
