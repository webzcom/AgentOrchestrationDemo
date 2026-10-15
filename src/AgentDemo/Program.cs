using AgentDemo.Agents;
using AgentDemo.Core;
using AgentDemo.Llm;
using AgentDemo.Tools;

// ─────────────────────────────────────────────────────────────────────────────
// Agent Orchestration Demo
//
//   dotnet run                 → offline, scripted model (no API key needed)
//   dotnet run -- --verbose    → also show the prompts sent to the model
//   dotnet run -- --real       → use a real model (needs ANTHROPIC_API_KEY)
// ─────────────────────────────────────────────────────────────────────────────

Console.OutputEncoding = System.Text.Encoding.UTF8;
Trace.Verbose = args.Contains("--verbose");

ILlmClient llm;
if (args.Contains("--real"))
{
    var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    if (string.IsNullOrWhiteSpace(key))
    {
        Console.Error.WriteLine("Set ANTHROPIC_API_KEY to use --real (or run without it for the offline demo).");
        return 2;
    }
    llm = new AnthropicLlmClient(key, Environment.GetEnvironmentVariable("ANTHROPIC_MODEL"));
}
else
{
    llm = new MockLlmClient();
}

// The agents write into ./workspace at the repo root, so you can open the files in the IDE.
var workspace = Path.Combine(FindRepoRoot(), "workspace");
var tools = new WorkspaceTools(workspace);

Console.WriteLine($"Model:     {llm.DisplayName}");
Console.WriteLine($"Workspace: {workspace}");

// Wire up the team. Each agent gets only what it needs (model and/or tools).
var orchestrator = new Orchestrator(
    planner:  new PlannerAgent(llm),
    coder:    new CoderAgent(llm, tools),
    tester:   new TesterAgent(tools),
    reviewer: new ReviewerAgent(llm),
    maxAttempts: 3);

var context = new TaskContext(
    goal: "Write a C# function IsPalindrome(string) that ignores case, spaces and punctuation.",
    workspaceDir: workspace);

var ok = await orchestrator.RunAsync(context);

Console.WriteLine();
Console.WriteLine("History (what the orchestrator recorded):");
for (var i = 0; i < context.History.Count; i++)
    Console.WriteLine($"  {i + 1}. {context.History[i]}");

return ok ? 0 : 1;

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AgentOrchestrationDemo.sln")))
        dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
