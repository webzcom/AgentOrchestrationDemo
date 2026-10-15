using System.Diagnostics;

namespace AgentDemo.Tools;

/// <summary>
/// "Tools" are the actions an agent may take in the real world.
/// In VS Code / Visual Studio agent mode these are things like editFiles, runInTerminal,
/// runTests, search. Here we have just two: write a file, and run a command.
///
/// Notice the guard rails: files can only be written INSIDE the workspace folder,
/// and only the dotnet CLI can be run. Limiting tools is how you keep agents safe.
/// </summary>
public sealed class WorkspaceTools
{
    private readonly string _root;

    public WorkspaceTools(string workspaceDir)
    {
        _root = Path.GetFullPath(workspaceDir);
        Directory.CreateDirectory(_root);
    }

    public string WriteFile(string relativePath, string content)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativePath));
        if (!full.StartsWith(_root, StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"Agents may only write inside the workspace: {relativePath}");

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    /// <summary>Runs `dotnet &lt;args&gt;` in the workspace and returns exit code + combined output.</summary>
    public async Task<(int ExitCode, string Output)> RunDotnetAsync(string args, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo("dotnet", args)
        {
            WorkingDirectory = _root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, (await stdout + await stderr).Trim());
    }
}
