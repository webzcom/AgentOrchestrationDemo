# Agent Orchestration Demo

A small, fully commented .NET 8 project that shows how **AI agents** and an **orchestrator** work together to write software. It covers:

- **Part A — Build it yourself:** a C# orchestrator coordinating four agents (Planner, Coder, Tester, Reviewer). You can run it, debug it and change it.
- **Part B — Use it in your IDE:** the same team defined as GitHub Copilot custom agents for VS Code and Visual Studio.

It runs **offline, for free, with no API key**, using a scripted "fake" model. You can switch to a real model with one flag.

---

## Table of contents

1. [Prerequisites](#1-prerequisites)
2. [Quick start](#2-quick-start)
3. [Key concepts](#3-key-concepts)
4. [How it works, step by step](#4-how-it-works-step-by-step)
5. [The code, file by file](#5-the-code-file-by-file)
6. [Running and debugging in VS Code](#6-running-and-debugging-in-vs-code)
7. [Running and debugging in Visual Studio](#7-running-and-debugging-in-visual-studio)
8. [Using a real model](#8-using-a-real-model)
9. [The same team as IDE agents (Copilot)](#9-the-same-team-as-ide-agents-copilot)
10. [Command-line options](#10-command-line-options)
11. [Exercises](#11-exercises)
12. [Troubleshooting](#12-troubleshooting)
13. [Glossary](#13-glossary)
14. [Project layout](#14-project-layout)

---

## 1. Prerequisites

| Requirement | Needed for | Notes |
|---|---|---|
| **.NET 8 SDK** (or newer) | Everything | Check with `dotnet --version` |
| **VS Code** + **C# Dev Kit** extension | Running and debugging in VS Code | |
| **Visual Studio 2022 (17.8+)** or later with the *.NET desktop development* workload | Running and debugging in Visual Studio | |
| **GitHub Copilot** (Chat / agent mode) | Part B only | Not needed for the C# demo |
| **Anthropic API key** | `--real` mode only | Optional |

The project has **no NuGet packages**, so nothing else needs installing.

---

## 2. Quick start

```bash
# from the folder containing AgentOrchestrationDemo.sln
dotnet run --project src/AgentDemo
```

Or press **F5** in VS Code or Visual Studio.

You should see (shortened):

```
Model:     Mock model (offline, scripted)
Workspace: .../AgentOrchestrationDemo/workspace

════════════════════════════════════════════════════════════════
  GOAL: Write a C# function IsPalindrome(string) that ignores case, spaces and punctuation.
════════════════════════════════════════════════════════════════
[Orchestrator] → handing off to Planner
  [Planner] ✔ Produced a 4-step plan with acceptance criteria.

── Attempt 1 of 3 ──────────────────────────────────────────────
[Orchestrator] → handing off to Coder
  [Coder] ✔ Wrote first version → Solution.cs (9 lines).
[Orchestrator] → handing off to Tester
    │ FAIL  IsPalindrome("A man, a plan, a canal: Panama") expected True but got False
    │ FAIL  IsPalindrome("No 'x' in Nixon") expected True but got False
  [Tester] ✖ 2 of 7 tests failed.
[Orchestrator] Tests failed → sending the failures back to the Coder.

── Attempt 2 of 3 ──────────────────────────────────────────────
[Orchestrator] → handing off to Coder
  [Coder] ✔ Rewrote code using test feedback → Solution.cs (22 lines).
[Orchestrator] → handing off to Tester
  [Tester] ✔ All 7 tests passed.
[Orchestrator] → handing off to Reviewer
  [Reviewer] ✔ Approved.

════════════════════════════════════════════════════════════════
  DONE ✔  Code written, tested and approved.
════════════════════════════════════════════════════════════════
```

The first run takes 10–20 seconds because the Tester builds a small project. Later runs are faster.

After a run, open the `workspace/` folder to see the files the agents wrote.

---

## 3. Key concepts

### What is an agent?

An **agent** is a worker with:

- **A role and instructions** (its "system prompt"), e.g. *"You are the code reviewer…"*
- **A model** that turns text into text (optional: the Tester has none)
- **Tools** that act on the real world, such as writing files or running commands (optional: the Planner has none)
- **One job.** It reads what it needs, does that job, and records the result.

### What is an orchestrator?

The **orchestrator** is the manager. It decides:

- **Order:** who runs next
- **Data flow:** what each agent gets as input
- **Failure handling:** retry, change course, or give up
- **Stopping:** when the work is done

In this demo the orchestrator is **ordinary C# code, not AI**: a `for` loop and some `if` statements. That keeps the overall process predictable even though the individual agents use a model.

### Why split the work across several agents?

- **Focus:** a short, specific prompt works better than one huge prompt that does everything.
- **Safety:** each agent gets only the tools it needs. A reviewer that can't edit files can't break them.
- **Verification:** generated work is checked by something else, ideally real tools such as a compiler and tests.
- **Feedback loops:** a failure becomes useful input for the next attempt instead of the end of the run.

### Two styles of orchestration

| | Code-driven (this C# demo) | Model-driven (Copilot agent mode) |
|---|---|---|
| Who decides the next step? | Your code | The model |
| Predictability | High: always the same order | Lower: it adapts as it goes |
| Flexibility | Low: only the steps you wrote | High: handles open-ended tasks |
| Typical use | Pipelines, CI, repeatable workflows | Interactive coding in the IDE |

Real systems often mix the two. Section 9 shows the model-driven version of this same team.

---

## 4. How it works, step by step

```
                 ┌──────────────── Orchestrator (plain code) ────────────────┐
                 │                                                           │
 Goal ──► Planner ──► Coder ──► Tester ──(pass)──► Reviewer ──► Done        │
                        ▲          │                                         │
                        └──(fail)──┘  failing test output → next prompt      │
                                                                             │
          every agent reads from and writes to one shared TaskContext ───────┘
```

| Step | Agent | Reads from context | Writes to context | Uses model? | Uses tools? |
|---|---|---|---|---|---|
| 1 | **Planner** | `Goal` | `Plan` | ✅ | ❌ |
| 2 | **Coder** | `Goal`, `Plan`, `Feedback`, previous `Code` | `Code` (+ `workspace/Solution.cs`) | ✅ | ✅ write file |
| 3 | **Tester** | (`Solution.cs` on disk) | `TestReport` | ❌ | ✅ write files, `dotnet run` |
| 3b | *Orchestrator* | `TestReport` | `Feedback` (on failure) | ❌ | ❌ |
| 4 | **Reviewer** | `Plan`, `Code`, `TestReport` | `Review` | ✅ | ❌ |

Steps 2–3 repeat up to `maxAttempts` (3) times.

### In detail

1. **Plan.** The orchestrator gives the Planner the goal. The Planner asks the model for numbered steps and acceptance criteria and stores them in `ctx.Plan`.
2. **Code.** The Coder builds a prompt from the goal and plan, asks the model for code, extracts the ` ```csharp ` block from the reply, and uses its file-writing tool to save `workspace/Solution.cs`.
3. **Test.** The Tester writes a test harness (`workspace/Program.cs`) containing 7 test cases plus a project file, then runs `dotnet run` in the workspace. That command really compiles and executes the Coder's code. Exit code 0 means all tests passed.
4. **Loop on failure.** If tests fail, the orchestrator copies the test output into `ctx.Feedback`. On the next attempt the Coder's prompt includes `FEEDBACK FROM TESTS` and its previous code, so the model knows exactly what to fix. **This is the core of how coding agents improve their own work.**
5. **Review.** Once tests pass, the Reviewer gets the plan, the code and the test output. Its reply must start with `APPROVE` or `REQUEST_CHANGES`, and that decides the final result.
6. **Finish.** The program prints the history of every step and exits with code `0` (success) or `1` (failure).

### Why the first attempt fails

The scripted model deliberately writes a naive version that reverses the string and compares it. That handles `"racecar"` but not `"A man, a plan, a canal: Panama"`, because it doesn't ignore case or punctuation. This guarantees you see the feedback loop every time. With a real model (`--real`) the first attempt may pass, or fail in a different way.

---

## 5. The code, file by file

All source is in `src/AgentDemo/`. Every file is short and commented.

### `Program.cs`: wiring it together
- Reads the command-line flags.
- Chooses the model client: `MockLlmClient` by default, `AnthropicLlmClient` with `--real`.
- Creates `WorkspaceTools` pointing at `<repo>/workspace`.
- Builds the four agents, giving each only what it needs:
  ```csharp
  planner:  new PlannerAgent(llm),           // model only
  coder:    new CoderAgent(llm, tools),      // model + tools
  tester:   new TesterAgent(tools),          // tools only
  reviewer: new ReviewerAgent(llm),          // model only
  ```
- Runs the orchestrator, then prints the history.

### `Core/`: the framework
| File | Purpose |
|---|---|
| `IAgent.cs` | The contract every agent implements: `Name`, `Color`, `RunAsync(TaskContext)`. Also defines `AgentResult(bool Success, string Summary)`. |
| `TaskContext.cs` | The shared state ("blackboard"): `Goal`, `Plan`, `Code`, `TestReport`, `Review`, `Feedback`, `Attempt`, `History`. |
| `Orchestrator.cs` | The workflow: plan, then the code/test loop, then review. **Start reading here.** |
| `Trace.cs` | Coloured console output. `--verbose` shows the full prompts. |

### `Agents/`: the team
| File | Role | Key detail |
|---|---|---|
| `PlannerAgent.cs` | Turns the goal into steps and acceptance criteria | Read-only, no tools |
| `CoderAgent.cs` | Writes `Solution.cs` | Adds test feedback to the prompt on retries; uses a regex to pull the code block out of the model's reply |
| `TesterAgent.cs` | Builds and runs the code against 7 cases | **No model.** Generates `Program.cs` and `Workspace.csproj`, then runs `dotnet run` |
| `ReviewerAgent.cs` | Final quality gate | Approval is decided by whether the reply starts with `APPROVE` |

### `Llm/`: the models
| File | Purpose |
|---|---|
| `ILlmClient.cs` | One method: `CompleteAsync(role, systemPrompt, userPrompt)`. |
| `MockLlmClient.cs` | Scripted replies chosen by `role`. Returns buggy code first and fixed code once the prompt contains `FEEDBACK FROM TESTS`. |
| `AnthropicLlmClient.cs` | Calls the Anthropic Messages API with plain `HttpClient` (no SDK), so you can see exactly what is sent. |

### `Tools/WorkspaceTools.cs`: the agents' hands
- `WriteFile(path, content)` refuses any path outside `workspace/`. This is a guard rail.
- `RunDotnetAsync(args)` can only run the `dotnet` CLI, only inside `workspace/`, and returns the exit code and output.

Limiting what tools can do is how real agent systems stay safe. The IDE agents in Part B use the same idea through their `tools:` lists.

### `workspace/`: created at runtime
| File | Written by |
|---|---|
| `Solution.cs` | Coder |
| `Program.cs` | Tester (test harness) |
| `Workspace.csproj` | Tester |

This folder is git-ignored and recreated on every run.

---

## 6. Running and debugging in VS Code

1. Install the **C# Dev Kit** extension.
2. **File → Open Folder…** and choose `AgentOrchestrationDemo`.
3. Open **Run and Debug** (Ctrl+Shift+D) and pick a configuration:
   | Configuration | What it runs |
   |---|---|
   | **Agent demo (offline)** | Scripted model |
   | **Agent demo (verbose prompts)** | Scripted model, prints every prompt |
   | **Agent demo (real model)** | Real model, verbose. Needs `ANTHROPIC_API_KEY` set *before* VS Code starts |
4. Press **F5**. Output appears in the integrated terminal.

**Suggested breakpoints** in `Core/Orchestrator.cs`:
- On `if (!await Step(_coder, ctx, ct)) return false;`: stops before each coding attempt.
- On `ctx.Feedback = ctx.TestReport;`: stops at the moment failure becomes feedback.

When it stops, hover over `ctx` or add it to **Watch** to see `Plan`, `Code`, `TestReport` and `Feedback` change between attempts.

You can also use **Terminal → Run Task… → run demo**, or Ctrl+Shift+B to build.

---

## 7. Running and debugging in Visual Studio

1. Double-click `AgentOrchestrationDemo.sln`.
2. Choose a launch profile from the dropdown next to the green **▶** button:
   | Profile | What it runs |
   |---|---|
   | **Offline (scripted model)** | Default |
   | **Offline, show prompts** | Adds `--verbose` |
   | **Real model (needs ANTHROPIC_API_KEY)** | Adds `--real --verbose` |
3. Press **F5**.
4. Use the same breakpoints as in the VS Code section, and inspect `ctx` in the **Locals**, **Watch** or **Autos** windows.

The profiles are defined in `src/AgentDemo/Properties/launchSettings.json`.

The `workspace/` folder isn't part of the solution. To see it, click **Show All Files** in Solution Explorer, or open it with **File → Open → Folder**.

---

## 8. Using a real model

1. Get an API key from the Anthropic Console.
2. Set the environment variables:
   ```powershell
   # Windows (PowerShell); restart VS / VS Code afterwards
   setx ANTHROPIC_API_KEY "sk-ant-..."
   setx ANTHROPIC_MODEL "claude-sonnet-5-5"      # optional
   ```
   ```bash
   # macOS / Linux
   export ANTHROPIC_API_KEY="sk-ant-..."
   export ANTHROPIC_MODEL="claude-sonnet-5-5"     # optional
   ```
3. Run:
   ```bash
   dotnet run --project src/AgentDemo -- --real --verbose
   ```

Things to notice:
- Results vary from run to run. The orchestrator's loop and limits are what keep the process reliable.
- Each run makes 3–5 API calls, which costs a small amount.
- **Using another provider** (Azure OpenAI, OpenAI, a local Ollama model…): create a class that implements `ILlmClient` and choose it in `Program.cs`. The agents and orchestrator don't change. That separation is the point of the interface.

---

## 9. The same team as IDE agents (Copilot)

In Part A you wrote the orchestrator yourself. **Copilot agent mode** in VS Code and Visual Studio is a ready-made orchestrator. The model plans, calls tools (edit files, run terminal commands, read errors), looks at the results and loops until done, just like `Orchestrator.cs`, except the model decides each next step.

You customise that built-in agent with files in the repo:

| File | What it does | VS Code | Visual Studio |
|---|---|---|---|
| `.github/copilot-instructions.md` | Project rules automatically added to every Copilot request | ✅ | ✅ |
| `AGENTS.md` | The same kind of rules in a tool-neutral format (also read by Claude Code, Codex and others) | ✅ (setting-dependent) | Check your version |
| `.github/agents/*.agent.md` | **Custom agents:** a role, its allowed tools, subagents and handoff buttons | ✅ | Check your version |

Copilot features change often. If something here doesn't match what you see, check the current VS Code docs page **"Custom agents"** and the Visual Studio Copilot docs.

### The four custom agents

Each `.agent.md` file has YAML frontmatter (settings) followed by Markdown instructions:

```markdown
---
description: Read-only planner. Turns a request into numbered steps and acceptance criteria.
tools: ['search', 'fetch']          # what this agent may do
handoffs:                           # buttons offered when it finishes
  - label: Implement this plan
    agent: implementer
    prompt: Implement the plan above, then run the tests.
    send: false                     # false = pre-fill the prompt, you press Send
---
# Planner
You plan; you never edit files. ...
```

| C# class | Custom agent file | Allowed tools | Hands off to |
|---|---|---|---|
| `Orchestrator` | `orchestrator.agent.md` | search, runSubagent, todos. Lists the others under `agents:` | (calls them as subagents) |
| `PlannerAgent` | `planner.agent.md` | search, fetch (read-only) | implementer |
| `CoderAgent` + `TesterAgent` | `implementer.agent.md` | search, edit, runInTerminal, problems | reviewer |
| `ReviewerAgent` | `reviewer.agent.md` | search, changes, problems (read-only) | implementer (to fix comments) |

The Coder and Tester are combined into one **implementer** because an IDE agent can edit code *and* run the tests itself, using `runInTerminal`.

### Way 1: Handoffs (you are the orchestrator)

1. Open **Copilot Chat** and choose **planner** from the agent dropdown.
2. Ask for something, e.g. *"Add a test case for 'Was it a car or a cat I saw?' to the TesterAgent."*
3. When the plan appears, click **Implement this plan**. Chat switches to the implementer with the prompt filled in. Press Send.
4. When it finishes, click **Review the change**.
5. If the reviewer requests changes, click **Fix review comments**.

You decide every step, the way a person reviews a pull request.

### Way 2: Subagents (the model is the orchestrator)

1. Choose **orchestrator** from the agent dropdown.
2. Ask for a change.
3. It runs **planner**, then **implementer** (retrying up to 3 times on test failures), then **reviewer** as subagents, and gives you a summary.

Subagents run with their own separate context, so each one only sees what the orchestrator passes it. This is the same idea as `TaskContext` in Part A. Depending on your version, subagent support may need to be enabled in settings. If the orchestrator can't call the others, search VS Code settings for **"subagent"**.

### If an agent shows a warning

Tool names change between releases. In the Chat view, hover over the warning on an agent. VS Code tells you which tool name it doesn't recognise. Fix the `tools:` list in that `.agent.md` file.

---

## 10. Command-line options

| Option | Effect |
|---|---|
| *(none)* | Offline scripted model |
| `--verbose` | Also print the full prompt sent to the model and all test output |
| `--real` | Use the Anthropic API (requires `ANTHROPIC_API_KEY`) |

| Environment variable | Default | Used by |
|---|---|---|
| `ANTHROPIC_API_KEY` | none | `--real` |
| `ANTHROPIC_MODEL` | `claude-sonnet-5-5` | `--real` |

**Exit codes:** `0` = approved, `1` = tests never passed or the reviewer rejected, `2` = `--real` without an API key.

To pass options with `dotnet run`, put them after `--`:
```bash
dotnet run --project src/AgentDemo -- --verbose
```

---

## 11. Exercises

They build on each other, so work through them in order.

1. **Watch the loop.** Run with `--verbose`. Find the prompt where the Coder receives `FEEDBACK FROM TESTS`. What else is in that prompt, and why does it include the previous code?
2. **Remove the safety net.** In `Program.cs`, set `maxAttempts: 1`. Run it. What does the orchestrator do, and why is a retry limit important?
3. **Add a test.** Add `("Was it a car or a cat I saw?", true)` to `Cases` in `TesterAgent.cs`. Does the fixed code still pass? Now add a case the fixed code gets wrong, and see the run fail.
4. **Make the Reviewer push back.** In `MockLlmClient.cs`, change the `Review` text to start with `REQUEST_CHANGES`. Then change `Orchestrator.cs` so a rejection sends the work back to the Coder with the review as `Feedback` (hint: copy the test-failure pattern). You'll also need the mock Coder to respond to review feedback.
5. **Add an agent.** Write a `DocsAgent : IAgent` that writes `workspace/README.md` describing the function. Run it after the Reviewer. Decide whether it needs a model, tools or both.
6. **Swap the model.** Write an `ILlmClient` for another provider, or for a local model with Ollama.
7. **Let the IDE do it.** In Copilot Chat, choose **orchestrator** and ask: *"Implement exercise 5: add a DocsAgent."* Compare its steps with `Orchestrator.cs`. Then do the same task using **planner** and handoffs, and compare the two ways.

---

## 12. Troubleshooting

| Problem | Fix |
|---|---|
| `dotnet: command not found` | Install the .NET 8 SDK and open a new terminal |
| First run is slow | Normal. The Tester builds a project. Later runs reuse the build |
| `NU1301: Unable to load the service index` | NuGet can't be reached (offline or behind a proxy). The demo uses no packages, but the SDK still contacts NuGet during restore once. Connect once, or add a `nuget.config` with `<clear />` under `packageSources` |
| Tester says "Build failed" with `--real` | The model returned code that doesn't compile. Run with `--verbose` to see it. The next attempt receives the compiler errors as feedback |
| `Set ANTHROPIC_API_KEY to use --real` | Set the variable, then restart the IDE so it picks it up |
| `Model call failed (401)` | The API key is wrong or expired |
| VS Code F5 says it can't find `AgentDemo.dll` | Build first (Ctrl+Shift+B). Check the target framework in `AgentDemo.csproj` matches the path in `.vscode/launch.json` |
| Custom agents don't appear in Copilot Chat | Make sure you opened the *repo root* folder (the one containing `.github/`), update Copilot, and check your IDE version supports custom agents |
| Strange characters (`âœ"`) in the output | Your console isn't using UTF-8. In Windows Terminal it's fine; in old consoles run `chcp 65001` first |

---

## 13. Glossary

| Term | Meaning |
|---|---|
| **Agent** | A worker with a role, instructions, and optionally a model and tools, that does one job |
| **Orchestrator** | The component that decides which agent runs, in what order, and what to do on failure |
| **LLM / model** | A large language model: text in, text out |
| **System prompt** | An agent's standing instructions ("You are the reviewer…") |
| **User prompt** | The specific input for one call (goal, plan, feedback) |
| **Tool** | An action an agent can take outside the model (edit a file, run a command, search) |
| **Shared context / blackboard** | Common state that agents read from and write to instead of messaging each other |
| **Feedback loop** | Passing the result of a check (e.g. failing tests) back to an agent so it can correct itself |
| **Guard rail** | A limit on what an agent can do (allowed folders, allowed commands, max attempts) |
| **Handoff** | A button in Copilot Chat that switches to another agent with a prepared prompt; a person stays in control |
| **Subagent** | An agent called by another agent, with its own separate context |
| **Agent mode** | Copilot's built-in mode where the model plans, uses tools and loops on its own |

---

## 14. Project layout

```
AgentOrchestrationDemo/
├── AgentOrchestrationDemo.sln          open this in Visual Studio
├── README.md                           this file
├── AGENTS.md                           tool-neutral instructions for any coding agent
├── .gitignore
├── .github/
│   ├── copilot-instructions.md         project rules for Copilot (VS Code + Visual Studio)
│   └── agents/
│       ├── orchestrator.agent.md       coordinates the others as subagents
│       ├── planner.agent.md            read-only planning
│       ├── implementer.agent.md        edits code, runs build and tests
│       └── reviewer.agent.md           read-only review
├── .vscode/
│   ├── launch.json                     F5 configurations
│   └── tasks.json                      build / run tasks
├── src/AgentDemo/
│   ├── AgentDemo.csproj
│   ├── Program.cs                      wires up the team and runs it
│   ├── Properties/launchSettings.json  Visual Studio launch profiles
│   ├── Core/
│   │   ├── IAgent.cs                   agent contract + AgentResult
│   │   ├── TaskContext.cs              shared state
│   │   ├── Orchestrator.cs             the workflow  ← start reading here
│   │   └── Trace.cs                    coloured console output
│   ├── Agents/
│   │   ├── PlannerAgent.cs
│   │   ├── CoderAgent.cs
│   │   ├── TesterAgent.cs
│   │   └── ReviewerAgent.cs
│   ├── Llm/
│   │   ├── ILlmClient.cs
│   │   ├── MockLlmClient.cs            offline scripted model
│   │   └── AnthropicLlmClient.cs       real model over HTTP
│   └── Tools/
│       └── WorkspaceTools.cs           sandboxed file write + dotnet run
└── workspace/                          created at runtime by the agents (git-ignored)
```
