# Agent Orchestration Demo

A tiny .NET 8 project that shows how "agents" and an "orchestrator" work together to write code —
first as plain C# you can step through in the debugger, then as the same team defined for
GitHub Copilot inside VS Code / Visual Studio.

It runs **offline with no API key** (a scripted fake model), and needs nothing but the .NET 8 SDK.

---

## 1. The idea in one picture

```
                    ┌──────────────── Orchestrator (plain code, not AI) ────────────────┐
                    │                                                                   │
 Goal ──► Planner ──► Coder ──► Tester ──(pass)──► Reviewer ──► Done                     │
            │           ▲          │                                                    │
            │           └──(fail)──┘  test output is fed back as the next prompt        │
            │                                                                           │
            └────────── all agents read/write one shared TaskContext ───────────────────┘
```

| Term | What it means here | Where to look |
|---|---|---|
| **Agent** | A role with instructions, a model and/or tools, doing one job | `src/AgentDemo/Agents/*.cs` |
| **Model (LLM)** | Text in → text out. Swappable. | `Llm/MockLlmClient.cs`, `Llm/AnthropicLlmClient.cs` |
| **Tools** | Real actions an agent may take (write a file, run a command) — with guard rails | `Tools/WorkspaceTools.cs` |
| **Shared context** | The "blackboard" agents read from and write to; they never talk directly | `Core/TaskContext.cs` |
| **Orchestrator** | Decides who runs next, retries on failure, stops when done | `Core/Orchestrator.cs` |
| **Feedback loop** | Failing test output becomes the Coder's next prompt | `Orchestrator.RunAsync` + `CoderAgent` |

Three things worth noticing in the code:
1. **The orchestrator is not AI.** It's a `for` loop with `if` statements. That's what makes the system predictable.
2. **The Tester uses no model at all.** It really compiles and runs the generated code. Let models *generate*, let real tools *verify*.
3. **Each agent gets only what it needs.** The Planner and Reviewer can't write files; only the Coder and Tester have tools.

---

## 2. Run it

```bash
dotnet run --project src/AgentDemo              # offline, scripted model
dotnet run --project src/AgentDemo -- --verbose # also print every prompt sent to the model
```

What you'll see: the Planner makes a plan → the Coder writes a **buggy** first version →
the Tester runs 7 real test cases and 2 fail → the orchestrator sends those failures back →
the Coder fixes it → all tests pass → the Reviewer approves.

The agents' files land in `workspace/` (`Solution.cs`, `Program.cs`, `Workspace.csproj`). Open them after a run.

### In VS Code
1. Install the **C# Dev Kit** extension and open this folder.
2. Open `src/AgentDemo/Core/Orchestrator.cs`, put a breakpoint on the `Step(_coder, ...)` line.
3. Run and Debug (F5) → pick **Agent demo (offline)**.
4. Each time it stops, hover `ctx` to watch `Plan`, `Code`, `TestReport` and `Feedback` fill in.

### In Visual Studio
1. Open `AgentOrchestrationDemo.sln`.
2. Choose a launch profile from the dropdown next to the green ▶ (Offline / Offline, show prompts / Real model).
3. Same breakpoint as above; use the **Locals** or **Watch** window on `ctx`.
4. `workspace/` isn't in the solution — use **Solution Explorer → Show All Files** or *File → Open → Folder* to see it.

### With a real model (optional)
Set `ANTHROPIC_API_KEY` (and optionally `ANTHROPIC_MODEL`), then run with `--real`.
The real model may get it right first time, or fail differently — the loop handles both.
To try another provider, write a new class implementing `ILlmClient`; nothing else changes.

---

## 3. The same team, inside the IDE

Section 2 is *you* building an orchestrator. Copilot's agent mode in VS Code and Visual Studio
is a ready-made orchestrator: the model plans, calls tools (edit files, run terminal commands,
run tests), reads the results and loops — exactly like `Orchestrator.cs`, but the loop is driven by the model.

This repo includes the config files that customise that built-in agent:

| File | What it does | VS Code | Visual Studio |
|---|---|---|---|
| `.github/copilot-instructions.md` | Project rules added to every request | ✅ | ✅ |
| `AGENTS.md` | Same idea, tool-neutral (Claude Code, Codex, etc. read it) | ✅ (setting-dependent) | check your version |
| `.github/agents/*.agent.md` | Custom agents: role, allowed tools, subagents, handoff buttons | ✅ | check your version |

The four custom agents mirror the C# classes:

| C# class | Custom agent | Tools it's allowed |
|---|---|---|
| `Orchestrator` | `orchestrator.agent.md` | search, runSubagent, todos — lists the others under `agents:` |
| `PlannerAgent` | `planner.agent.md` | search, fetch (read-only) |
| `CoderAgent` + `TesterAgent` | `implementer.agent.md` | edit, runInTerminal (it runs the build/tests itself) |
| `ReviewerAgent` | `reviewer.agent.md` | search, changes (read-only) |

Two ways to use them in VS Code (Copilot Chat):
- **Handoffs (you stay in control):** pick *planner* in the agent dropdown, ask for a change, then click the
  **Implement this plan** button it offers, then **Review the change**. You are the orchestrator.
- **Subagents (automatic):** pick *orchestrator* and ask for a change. It calls the others as subagents and loops on its own.

Tool names and frontmatter keys evolve between releases. If an agent shows a warning in the chat view,
hover it — VS Code tells you which tool name it doesn't recognise. See the VS Code docs page
"Custom agents" for the current list.

---

## 4. Exercises (in order)

1. **Watch the loop.** Run with `--verbose`. Find the exact prompt where the Coder receives `FEEDBACK FROM TESTS`.
2. **Break the safety net.** Set `maxAttempts: 1` in `Program.cs`. What does the orchestrator do now?
3. **Add a test.** Add `("Was it a car or a cat I saw?", true)` to `TesterAgent.Cases`. Does the fixed code still pass?
4. **Make the Reviewer push back.** Edit the mock's `Review` text to start with `REQUEST_CHANGES`. Then change the orchestrator so a rejection sends the Coder another round (hint: copy the test-failure pattern).
5. **Add an agent.** Write a `DocsAgent` that writes `workspace/README.md` describing the function, and run it after the Reviewer.
6. **Use the IDE agents.** Open Copilot Chat, choose *orchestrator*, and ask: *"Add exercise 5's DocsAgent to the project."*
   Compare what it does with the loop you read in `Orchestrator.cs`.

---

## Layout

```
AgentOrchestrationDemo.sln
AGENTS.md                         tool-neutral agent instructions
.github/
  copilot-instructions.md         project rules for Copilot (VS Code + Visual Studio)
  agents/*.agent.md               custom agents: orchestrator, planner, implementer, reviewer
.vscode/launch.json, tasks.json   F5 configs for VS Code
src/AgentDemo/
  Program.cs                      wires up the team and runs it
  Core/        IAgent, TaskContext, Orchestrator, Trace
  Agents/      PlannerAgent, CoderAgent, TesterAgent, ReviewerAgent
  Llm/         ILlmClient, MockLlmClient, AnthropicLlmClient
  Tools/       WorkspaceTools (sandboxed file write + dotnet run)
workspace/                        created at runtime by the agents
```
