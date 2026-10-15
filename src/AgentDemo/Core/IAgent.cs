namespace AgentDemo.Core;

/// <summary>
/// What every agent returns: did it succeed, and a short summary
/// the orchestrator can log or act on.
/// </summary>
public sealed record AgentResult(bool Success, string Summary);

/// <summary>
/// An agent = a role + instructions + (optionally) a model + (optionally) tools.
/// It does ONE job, reads what it needs from the context, writes its result back.
/// </summary>
public interface IAgent
{
    string Name { get; }

    /// <summary>The colour used for this agent in the console trace.</summary>
    ConsoleColor Color { get; }

    Task<AgentResult> RunAsync(TaskContext context, CancellationToken ct = default);
}
