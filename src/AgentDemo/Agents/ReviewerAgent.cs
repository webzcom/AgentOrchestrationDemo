using AgentDemo.Core;
using AgentDemo.Llm;

namespace AgentDemo.Agents;

/// <summary>
/// Reviewer: a second opinion on code that already passes tests.
/// Uses the model, no tools (read-only). Its verdict is the final gate.
/// </summary>
public sealed class ReviewerAgent(ILlmClient llm) : IAgent
{
    public string Name => "Reviewer";
    public ConsoleColor Color => ConsoleColor.Green;

    private const string SystemPrompt = """
        You are the CODE REVIEWER on a small software team. The code already passes its tests.
        Start your reply with exactly APPROVE or REQUEST_CHANGES on the first line,
        then up to 3 short bullet points.
        """;

    public async Task<AgentResult> RunAsync(TaskContext ctx, CancellationToken ct = default)
    {
        var prompt = $"PLAN:\n{ctx.Plan}\n\nCODE:\n{ctx.Code}\n\nTEST OUTPUT:\n{ctx.TestReport}";
        ctx.Review = await llm.CompleteAsync(Name, SystemPrompt, prompt, ct);
        Trace.Detail("Review", ctx.Review, force: true);

        var approved = ctx.Review.TrimStart().StartsWith("APPROVE", StringComparison.OrdinalIgnoreCase);
        return new AgentResult(approved, approved ? "Approved." : "Requested changes.");
    }
}
