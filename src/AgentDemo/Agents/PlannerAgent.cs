using AgentDemo.Core;
using AgentDemo.Llm;

namespace AgentDemo.Agents;

/// <summary>
/// Planner: turns a vague goal into concrete steps and acceptance criteria.
/// Uses the model, has NO tools (it can't touch files) — a read-only role.
/// </summary>
public sealed class PlannerAgent(ILlmClient llm) : IAgent
{
    public string Name => "Planner";
    public ConsoleColor Color => ConsoleColor.Cyan;

    private const string SystemPrompt = """
        You are the PLANNER on a small software team.
        Break the goal into 3-6 short numbered implementation steps, then list acceptance criteria
        as concrete input -> expected output examples. Do not write code. Be brief.
        """;

    public async Task<AgentResult> RunAsync(TaskContext ctx, CancellationToken ct = default)
    {
        ctx.Plan = await llm.CompleteAsync(Name, SystemPrompt, $"GOAL:\n{ctx.Goal}", ct);
        Trace.Detail("Plan", ctx.Plan, force: true);

        var steps = ctx.Plan.Split('\n').Count(l => char.IsDigit(l.TrimStart().FirstOrDefault()));
        return new AgentResult(true, $"Produced a {steps}-step plan with acceptance criteria.");
    }
}
