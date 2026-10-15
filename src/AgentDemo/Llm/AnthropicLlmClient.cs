using System.Net.Http.Json;
using System.Text.Json;

namespace AgentDemo.Llm;

/// <summary>
/// A real model, called over plain HTTP (no SDK) so you can see exactly what goes over the wire.
/// Set ANTHROPIC_API_KEY and run with --real. Optionally set ANTHROPIC_MODEL.
///
/// Any provider works the same way: write another ILlmClient (Azure OpenAI, Ollama, etc.)
/// and the agents and orchestrator don't change.
/// </summary>
public sealed class AnthropicLlmClient : ILlmClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };
    private readonly string _apiKey;
    private readonly string _model;

    public AnthropicLlmClient(string apiKey, string? model = null)
    {
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? "claude-sonnet-5-5" : model;
    }

    public string DisplayName => $"Anthropic API ({_model})";

    public async Task<string> CompleteAsync(string role, string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(new
        {
            model = _model,
            max_tokens = 2000,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } },
        });

        using var response = await Http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Model call failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        return string.Concat(doc.RootElement.GetProperty("content").EnumerateArray()
            .Where(b => b.GetProperty("type").GetString() == "text")
            .Select(b => b.GetProperty("text").GetString()));
    }
}
