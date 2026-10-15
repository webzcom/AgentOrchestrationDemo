namespace AgentDemo.Core;

/// <summary>
/// The shared "blackboard" every agent reads from and writes to.
///
/// Agents never talk to each other directly. They only see this object,
/// and the orchestrator decides who runs next. This is the single most
/// important idea in agent orchestration: agents are isolated workers,
/// and all coordination lives in one place (the orchestrator).
/// </summary>
public sealed class TaskContext
{
    public TaskContext(string goal, string workspaceDir)
    {
        Goal = goal;
        WorkspaceDir = workspaceDir;
    }

    /// <summary>What the user asked for.</summary>
    public string Goal { get; }

    /// <summary>Folder the agents are allowed to write files into.</summary>
    public string WorkspaceDir { get; }

    /// <summary>Written by the Planner.</summary>
    public string? Plan { get; set; }

    /// <summary>Written by the Coder (also saved to disk as Solution.cs).</summary>
    public string? Code { get; set; }

    /// <summary>Written by the Tester.</summary>
    public string? TestReport { get; set; }

    /// <summary>Written by the Reviewer.</summary>
    public string? Review { get; set; }

    /// <summary>
    /// Feedback the orchestrator passes back to the Coder on a retry
    /// (e.g. the failing test output). This is how the loop "learns".
    /// </summary>
    public string? Feedback { get; set; }

    /// <summary>Which attempt the Coder is on (1-based).</summary>
    public int Attempt { get; set; }

    /// <summary>A simple trace of everything that happened, in order.</summary>
    public List<string> History { get; } = new();
}
