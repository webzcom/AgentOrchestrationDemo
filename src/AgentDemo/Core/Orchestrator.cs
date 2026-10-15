namespace AgentDemo.Core;

/// <summary>
/// The orchestrator is ordinary code. It is NOT an LLM.
///
/// It decides the order agents run in, what happens on failure, and when to stop.
/// Here the workflow is:
///
///   Planner ──► Coder ──► Tester ──┬─(pass)──► Reviewer ──► done
///                 ▲                │
///                 └──(fail: feed ──┘
///                     test output back)
///
/// Put a breakpoint in RunAsync and step through it to watch the hand-offs.
/// </summary>
public sealed class Orchestrator
{
    private readonly IAgent _planner;
    private readonly IAgent _coder;
    private readonly IAgent _tester;
    private readonly IAgent _reviewer;
    private readonly int _maxAttempts;

    public Orchestrator(IAgent planner, IAgent coder, IAgent tester, IAgent reviewer, int maxAttempts = 3)
    {
        _planner = planner;
        _coder = coder;
        _tester = tester;
        _reviewer = reviewer;
        _maxAttempts = maxAttempts;
    }

    public async Task<bool> RunAsync(TaskContext ctx, CancellationToken ct = default)
    {
        Trace.Banner($"GOAL: {ctx.Goal}");

        // 1. PLAN — runs once.
        if (!await Step(_planner, ctx, ct)) return false;

        // 2. BUILD/TEST LOOP — the Coder retries until tests pass or we give up.
        var testsPassed = false;
        for (ctx.Attempt = 1; ctx.Attempt <= _maxAttempts; ctx.Attempt++)
        {
            Trace.Section($"Attempt {ctx.Attempt} of {_maxAttempts}");

            if (!await Step(_coder, ctx, ct)) return false;

            testsPassed = await Step(_tester, ctx, ct);
            if (testsPassed) break;

            // The key move: the failure output becomes the next prompt's input.
            ctx.Feedback = ctx.TestReport;
            Trace.Orchestrator("Tests failed → sending the failures back to the Coder.");
        }

        if (!testsPassed)
        {
            Trace.Orchestrator($"Gave up after {_maxAttempts} attempts. A human needs to look at this.");
            return false;
        }

        // 3. REVIEW — a final quality gate.
        var approved = await Step(_reviewer, ctx, ct);
        Trace.Banner(approved ? "DONE ✔  Code written, tested and approved." : "STOPPED ✖  Reviewer did not approve.");
        return approved;
    }

    private static async Task<bool> Step(IAgent agent, TaskContext ctx, CancellationToken ct)
    {
        Trace.Orchestrator($"→ handing off to {agent.Name}");
        var result = await agent.RunAsync(ctx, ct);
        ctx.History.Add($"{agent.Name}: {(result.Success ? "OK" : "FAIL")} — {result.Summary}");
        Trace.Agent(agent, result);
        return result.Success;
    }
}
