using System.Text.RegularExpressions;
using AgentDemo.Core;
using AgentDemo.Llm;
using AgentDemo.Tools;

namespace AgentDemo.Agents;

/// <summary>
/// Coder: writes the implementation. Uses the model AND a tool (write a file).
/// On a retry it also receives the orchestrator's feedback (the failing tests).
/// </summary>
public sealed partial class CoderAgent(ILlmClient llm, WorkspaceTools tools) : IAgent
{
    public string Name => "Coder";
    public ConsoleColor Color => ConsoleColor.Yellow;

    private const string SystemPrompt = """
        You are the CODER on a small software team.
        Implement the plan in C# (.NET 8) as `public static class Solution` with
        `public static bool IsPalindrome(string? input)`.
        Reply with ONLY one ```csharp code block. No Main method, no tests, no explanation.
        If FEEDBACK FROM TESTS is present, fix exactly what failed.
        """;

    public async Task<AgentResult> RunAsync(TaskContext ctx, CancellationToken ct = default)
    {
        // Build the prompt from the shared context. Context in → text out → context updated.
        var prompt = $"GOAL:\n{ctx.Goal}\n\nPLAN:\n{ctx.Plan}";
        if (ctx.Feedback is not null)
            prompt += $"\n\nFEEDBACK FROM TESTS (your previous attempt failed):\n{ctx.Feedback}\n\nPREVIOUS CODE:\n{ctx.Code}";

        Trace.Detail("Prompt sent to model", prompt);

        var reply = await llm.CompleteAsync(Name, SystemPrompt, prompt, ct);
        var match = CodeBlock().Match(reply);
        if (!match.Success)
            return new AgentResult(false, "Model did not return a ```csharp code block.");

        ctx.Code = match.Groups[1].Value.Trim();
        var path = tools.WriteFile("Solution.cs", ctx.Code + "\n");   // ← tool call
        Trace.Detail($"Code (attempt {ctx.Attempt})", ctx.Code, force: true);

        var what = ctx.Feedback is null ? "Wrote first version" : "Rewrote code using test feedback";
        return new AgentResult(true, $"{what} → {Path.GetFileName(path)} ({ctx.Code.Split('\n').Length} lines).");
    }

    [GeneratedRegex(@"```(?:csharp|cs|c#)?\s*\n(.*?)```", RegexOptions.Singleline)]
    private static partial Regex CodeBlock();
}
