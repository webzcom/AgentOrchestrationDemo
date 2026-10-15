namespace AgentDemo.Llm;

/// <summary>
/// The only thing an agent needs from a model: send instructions + input, get text back.
/// Swap implementations to go from the offline mock to a real model without touching agents.
/// </summary>
public interface ILlmClient
{
    string DisplayName { get; }

    /// <param name="role">Which agent is calling (used by the mock to pick a scripted reply).</param>
    /// <param name="systemPrompt">The agent's standing instructions — its "job description".</param>
    /// <param name="userPrompt">The specific input for this call (goal, plan, feedback...).</param>
    Task<string> CompleteAsync(string role, string systemPrompt, string userPrompt, CancellationToken ct = default);
}
