# AGENTS.md

Tool-neutral instructions for any coding agent (Copilot, Claude Code, Codex, etc.).
Same content as `.github/copilot-instructions.md`, in the format other agents look for.

## Build and run
- `dotnet build`
- `dotnet run --project src/AgentDemo` — must exit 0 and print `DONE ✔`

## Layout
- `src/AgentDemo/Core` — `IAgent`, `TaskContext` (shared state), `Orchestrator` (the workflow)
- `src/AgentDemo/Agents` — one class per role: Planner, Coder, Tester, Reviewer
- `src/AgentDemo/Llm` — `MockLlmClient` (offline) and `AnthropicLlmClient` (real)
- `workspace/` — generated at runtime by the agents; don't edit

## Rules
- No new NuGet packages.
- Agents don't call each other; only the orchestrator decides order.
