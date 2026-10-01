# Claude API Demo - Official C#/.NET SDK

Console demo for a presentation on the Claude API. A menu runs independent
scenarios, one per feature of the official `Anthropic` NuGet package, in the
order of the presentation: messages, tools and MCP, Skills, Managed Agents, then
batches.

The slides of the presentation are in `DemoPresentation/`
(`Claude_API_Presentation_v9.pptx`).

## Prerequisites

- .NET 10 SDK
- A Claude API key, either in `appsettings.local.json` or in the
  `ANTHROPIC_API_KEY` environment variable
- The official `Anthropic` NuGet package, already referenced in the `.csproj`
  (`dotnet restore` fetches it)

## Configuration

`appsettings.json` is a template: each value is a `<USE_LOCAL_APPSETTINGS_TO_SET_...>`
placeholder. Create `appsettings.local.json` next to it and put your own values
there. It overrides the template and is git-ignored, so the key and the IDs are
never committed or shared with the project. Without it, the placeholder IDs are
used as they are and the scenarios that need them fail.

```json
{
  "Anthropic": { "ApiKey": "sk-ant-..." },
  "CustomSkillId": { "expense-compliance-check": "skill_..." },
  "ManagedAgent": {
    "expense-compliance-agent": "agent_...",
    "expense-workflow-agent": "agent_...",
    "EnvironmentId": "env_...",
    "MemoryStoreId": "memstore_..."
  }
}
```

A configured `Anthropic:ApiKey` takes precedence over the `ANTHROPIC_API_KEY`
environment variable, so a key in `appsettings.local.json` always wins. A value
still starting with `<` is a placeholder and is ignored, so the environment
variable stays in use. Without any key, the SDK silently falls back to another local login (for example
Claude Code's), which may belong to a different organization or workspace than
the one you expect.

The IDs are the resources created in the Claude Console, so they belong to one
organization:

| Key | Resource |
|---|---|
| `CustomSkillId:expense-compliance-check` | The custom Skill of scenario 8 |
| `ManagedAgent:expense-compliance-agent` | The agent of scenario 9 |
| `ManagedAgent:expense-workflow-agent` | The coordinator agent of scenario 10 |
| `ManagedAgent:EnvironmentId` | The environment the agents run in |
| `ManagedAgent:MemoryStoreId` | The memory store mounted in the sessions |

## Running

```bash
dotnet run
```

A menu lists the scenarios. To run one without going through the menu:

```bash
dotnet run -- 3
```

## Console commands

The demo never prints these commands. Type them where a prompt is expected.

| Command | Effect | Where |
|---|---|---|
| `/auto` | Uses a ready-made demo prompt instead of typing one. In chat scenarios, each `/auto` sends the next prompt of a short script, then reports that none are left. | Scenarios 1, 2, 4 to 10, the token counting reference, and the bonus ones |
| `/ref` | Opens a sub-menu with the scenarios of the `References/` folder (`0` goes back). | Main menu |
| `/bonus` | Opens a sub-menu with the scenarios of the `Scenarios/Bonus/` folder (`0` goes back). | Main menu |
| `/exit` | Ends the chat and returns to the menu. An empty line also ends scenario 2. | Scenarios 2, 9, 10 |

The automatic prompts and the other demo texts live in `Demo/DemoInput.cs`,
outside the scenarios.

## Scenarios

### Sending messages

1. **Simple prompt with streaming**: one prompt, and the answer is streamed
   token by token (`CreateStreaming`).
2. **Multi-turn chat**: the API is stateless, so the whole history is resent on
   every turn. Prompt caching keeps the earlier turns cheap.
3. **Prompt caching**: a large system prompt with a cache breakpoint, sent
   twice. The token counters show the cache being written, then read.
4. **Assistant prefill**: the conversation ends with a partial Assistant
   message and Claude continues from it (here, an `appsettings.json`). Sent
   first to Haiku 4.5, then to Sonnet 5, which rejects it with an API error.
5. **Structured outputs**: a support ticket extracted as guaranteed JSON into
   a C# class annotated with `[SchemaClass]` and `[SchemaProperty]`.

### Tools and MCP

6. **ToolRunner - one tool**: a `get_current_weather` tool calling the free
   Open-Meteo API. The `ToolRunner` helper (beta) runs the tool-use loop.
7. **MCP connector**: Claude reaches a remote MCP server (DeepWiki, public and
   keyless) from Anthropic's infrastructure, with no client-side loop (beta).

### Skills

8. **Custom Skill**: the `expense-compliance-check` Skill, uploaded once in the
   Claude Console, is referenced by its ID in the request container with the
   code execution tool enabled. The Skill (sources in `skills/`) checks an
   expense against fixed caps and always returns the same verdict for the same
   input. The response is displayed with `StreamDisplayMode`: the full thought
   process, or only the final answer.

> **The `skills/` folder.** It holds the source of every Skill used by
> scenarios 8, 9 and 10, one sub-folder each (`SKILL.md`, plus `scripts/`,
> `references/` and `assets/` when needed): `expense-compliance-check` for
> scenarios 8 and 9, and the single-purpose Skills of the scenario 10 workflow
> (`expense-categorization`, `currency-conversion`, `compliance-check`,
> `expense-audit-log`, `expense-approval-notification`,
> `expense-receipt-extraction`). The C# code never reads this folder: the Skills
> run from the Claude Console, so each one must be uploaded there once (zip the
> sub-folder, with `SKILL.md` at its root), and its ID put in
> `appsettings.local.json`. Re-upload a Skill after editing its sources here.

### Managed Agents (beta)

The agents are created once in the Claude Console and only referenced here by
their ID. The C# code creates a **session**, sends `user.message` events and
reads the event stream. Anthropic runs the agent loop in a hosted container.
Scenarios 9 and 10 share `Scenarios/ManagedAgentScenario.cs`.

9. **Managed Agent - expense compliance**: `expense-compliance-agent` with one
   Skill and a memory store. `/auto` sends a foreign-currency expense; a second
   run of the scenario, a new session with an empty history, asks what the
   agent remembers, so the answer comes from the store. The store accumulates
   notes between runs: empty it in the Console before a fresh demo.
10. **Managed Agent - full expense workflow**: `expense-workflow-agent`, a
    coordinator composing six single-purpose Skills (categorization, currency
    conversion, compliance check, audit log, approval notification, receipt
    extraction), plus a subagent, `policy-exception-advisor`, consulted only
    for exception requests. The C# code has the same shape as scenario 9. The
    receipt extraction Skill is part of the agent but not exercised: the demo
    only sends text.

### Batches

11. **Create a batch**: three independent requests sent in one batch at half
    price. The scenario prints the batch ID, saves it in `last-batch-id.txt`
    (`Services/LastBatchStore.cs`) and ends without waiting.
12. **Retrieve its results later**: reads the status of a batch from its ID
    (empty for the last one) and prints the results once it has ended. A batch
    can take up to 24 hours, so create it well before the demo. Batches are also
    listed in the Console, under the workspace's *Batches* page.

### References and bonus

- `/ref` opens the scenarios of `References/`: token counting (`CountTokens`
  before sending, compared with the usage the API bills), the manual tool-use loop,
  citations, a handoff between peer agents, a chat with logging, the full
  multi-turn chat, streaming, a multi-tool `ToolRunner`, the fan-out / fan-in
  agent team and agents as tools. They are kept as references and are not part
  of the presentation.
- `/bonus` opens the Vision and PDF scenarios. They read the first image and the
  first PDF of the `asset/` folder; rebuild after adding one so it is copied
  next to the executable.

## Project layout

| Folder | Content |
|---|---|
| `Scenarios/` | One class per scenario, limited to the use of the SDK |
| `Demo/` | Demo plumbing: the scenario menu, `/auto` and the demo texts |
| `Services/` | `ClaudeConsoleService` (Managed Agents sessions and events), the `StreamDisplayService` and `SessionDisplayService` display modes, the console logger and the batch ID store |
| `MCPTools/` | The tools and sub-agents used by the `ToolRunner` scenarios |
| `Const/` | The system prompts |
| `DemoPresentation/` | The PowerPoint of the presentation (not used by the C# code) |
| `skills/` | The sources of the custom Skills, to upload to the Claude Console (not used by the C# code) |

## Notes

- Models vary by scenario: Haiku 4.5 for the simple, high-volume cases (chat,
  prefill, Skill, batch) and Sonnet 5 elsewhere.
- **Beta features**: the `Anthropic` package is a stable release, but the
  `ToolRunner` helper, the MCP connector and Managed Agents are in public beta
  and use `client.Beta...`. They can change without following the SDK's own
  versioning, and some must be enabled for the organization behind your API
  key. The other scenarios use the stable API.
- Update the SDK (`dotnet add package Anthropic`) if a scenario returns an
  error about a recent feature.
