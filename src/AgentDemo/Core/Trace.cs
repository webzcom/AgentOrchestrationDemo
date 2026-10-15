namespace AgentDemo.Core;

/// <summary>Pretty console output so you can follow the hand-offs.</summary>
public static class Trace
{
    public static bool Verbose { get; set; }

    public static void Banner(string text)
    {
        Console.WriteLine();
        Write(ConsoleColor.White, new string('═', 64));
        Write(ConsoleColor.White, "  " + text);
        Write(ConsoleColor.White, new string('═', 64));
    }

    public static void Section(string text)
    {
        Console.WriteLine();
        Write(ConsoleColor.DarkGray, $"── {text} " + new string('─', Math.Max(0, 60 - text.Length)));
    }

    public static void Orchestrator(string text) => Write(ConsoleColor.Gray, $"[Orchestrator] {text}");

    public static void Agent(IAgent agent, AgentResult result)
    {
        var mark = result.Success ? "✔" : "✖";
        Write(agent.Color, $"  [{agent.Name}] {mark} {result.Summary}");
    }

    /// <summary>Shows a block of agent output (prompt, plan, code...). Only in --verbose mode unless forced.</summary>
    public static void Detail(string label, string body, bool force = false)
    {
        if (!Verbose && !force) return;
        Write(ConsoleColor.DarkGray, $"    ┌ {label}");
        foreach (var line in body.TrimEnd().Split('\n'))
            Write(ConsoleColor.DarkGray, "    │ " + line.TrimEnd('\r'));
        Write(ConsoleColor.DarkGray, "    └");
    }

    private static void Write(ConsoleColor color, string text)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = old;
    }
}
