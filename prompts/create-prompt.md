# Codex Prompt Creator (General)

Use this prompt to craft task-specific prompts for Codex across any stack. Keep it tool-compatible (shell + Codex tools), concise, and explicit about outputs and verification. Your goal is to create prompts that get things done accurately and efficiently for `$TASK` (the task description provided at invocation).

## Usage and flow
- Invoked with a task description `$TASK` (e.g., "Create the Inventory Manager module with capacity limits").
- Intake focuses on business/outcome questions: ask whatever you need to clarify desired outcomes, success criteria, scope, stack, test command, and key files. Avoid "how" questions—the implementation is yours to decide. Aim for 1–2 rounds; allow up to 4 only if genuinely complex; stop once outcomes are clear.
- Before asking, auto-skim easy context: always read the target repo’s `AGENTS.md` if present; also check `README.md` and `CONTRIBUTING.md`; if no prompt-specific guidance exists, apply shared rules in `prompts/PROMPT_RULES.md`. List files (e.g., `ls`, `rg --files`) to avoid unnecessary questions.
- Decide if the task needs one prompt or multiple (sequential if dependent, parallel if independent and non-overlapping).
- Generate the prompt(s) yourself—optimized for Codex execution—using the structure below, filling concrete paths and commands (no placeholders).
- Save each prompt to `./prompts/NNN-name.md` (increment number automatically based on existing files).
- After saving, briefly note next steps (e.g., run/review). Do not add explanations inside the saved prompt files.
- Keep generated prompts lean: concise bullets, no repeated boilerplate, only sections that add value.
- Note on file locations: the helper prompts in `/prompts` (this file, create-meta-prompt, run-prompt) stay unnumbered; new runnable prompts in `./prompts/` must use zero-padded numeric prefixes.

## Intake
- If the task is missing or vague, ask outcome-focused questions (what success looks like, constraints, priorities) plus essentials (scope, stack, test command, key files). Avoid asking how to implement—decide that yourself. Aim for 1–2 rounds; allow up to 4 only if genuinely complex; stop once outcomes are clear.
- Capture stack (language/framework), test command, and any repo conventions (style guides, linting, docs).
- Infer stack/test command from manifests when possible (e.g., `package.json`, `pubspec.yaml`, `mix.exs`, `Cargo.toml`, `*.csproj`).

### Question templates (use only if gaps exist)
- Ambiguous scope: ask what type (e.g., admin vs analytics vs end-user), target audience, and primary outcome.
- Unclear target: where does the change live (frontend/UI, backend/API, database, infra/tooling)?
- Auth/security tasks: auth method (JWT, session, OAuth/SSO) and why.
- Performance tasks: main concern (load time, runtime, database) and target constraints.
- Output/deliverable clarity: intended use (production, prototype, internal), and required polish.

### Question rules
- Only ask when there is a real gap; avoid redundancy.
- Prefer structured choices when options are knowable; always allow "Other" if you need flexibility.
- Keep it brief; stop once success outcomes and constraints are clear; target ≤1–2 rounds (max 4 for complex cases).

## File numbering
- Save new prompts as `./prompts/NNN-name.md` (zero-padded) for standard tasks under `./prompts/`.
- Meta-prompt chains live under `.prompts/{slug}-{purpose}/` (slugged, not numbered) per `prompts/create-meta-prompt.md`; reference exact paths when handing off to the runner.
- To pick the next number: list existing prompt files (e.g., `ls ./prompts` sorted), take the highest 3-digit prefix, and increment. If none exist, start at `001`.
- Keep slugged `.prompts/` artifacts out of `./prompts/` to avoid mixing naming schemes.

## Prompt structure (Markdown)
Include these sections in the generated prompt (omit optional ones if not needed):

````markdown
# Objective
- What to build/fix/refactor and why it matters.

# Context
- Project type/stack: [fill in]
- Constraints: [style/perf/security/compatibility]
- Key files to read: `README.md`, `CONTRIBUTING.md`, `STYLE.md`, `AGENTS.md`, `[specific files]`
- Practices: use `rg` for search; prefer `apply_patch` for single-file edits; keep ASCII; avoid destructive git commands.

# Requirements
- Functional: [inputs/outputs, edge cases]
- Non-functional: [perf/security/data integrity]
- Avoid: [what to avoid and why]
- Include brief "why" for key constraints.

# Plan
- Step 1: …
- Step 2: …
- Reflect after commands before proceeding.

# Output
- `./path/to/file.ext` — [what goes here]
- `./[project-appropriate-tests-path]/...` — [tests in the repo’s convention]
- `./[project-appropriate-docs-path]/...` — [docs/changelog per repo norms]
- Ensure no bracketed placeholders remain; resolve concrete paths and commands.

# Verification
- Run: `[TEST_CMD]` (or state explicitly if no tests/commands exist).
- Manual checks: [e.g., render component, hit endpoint, inspect logs].
- If commands/tests cannot be run in current sandbox, state that and specify what should be run when permitted.

# Success Criteria
- [Measurable: tests pass, behavior matches spec, style adhered, docs updated].
- Derive measurable checks from `$TASK` outcomes (e.g., capacity limits enforced, error cases handled).

# Research (optional)
- If needed, explore with `rg` and inspect files before editing.

# Examples (optional)
- Example behavior/API usage if ambiguity remains.
````

## Agent practices (Codex)
- Use `rg` for search; avoid broad `find` unless necessary.
- Default to ASCII; match existing style; add comments sparingly.
- Prefer `apply_patch` for single-file edits; do not use destructive git commands.
- If multiple independent shell checks are needed, run them in parallel when sensible; reflect on results before acting.
- If a numbered prompt depends on slugged meta outputs, read the relevant metadata (`## Metadata` in research/plan/implementation notes) before proceeding and reference the exact paths.

## Output expectation
- Generate the task-specific prompt following the structure above.
- Save it to `./prompts/NNN-name.md` with a short descriptive name.
- Do not include explanations in the saved file—only the prompt content.
- Drop unused sections to keep prompts lean. Keep each section to a few bullets; keep total prompt concise (avoid unnecessary verbosity).
- Before saving, ensure no placeholders remain (`[]`, `<>`); fill concrete paths, commands, and criteria.

## Sandbox and approval cues
- Assume sandbox/approval constraints may apply; avoid destructive commands. If approvals are disabled, do not request them—work within the sandbox.
- Prefer read/search commands when exploring; note test commands but avoid running them if not permitted.
- If sandbox blocks an essential action (including network), state the limitation in the prompt summary instead of attempting escalation.

## Multi-prompt guidance
- Decide single vs multiple prompts: split when tasks are independent or have clear dependency stages (e.g., schema → service → API).
- Sequential example: `005-schema.md` → `006-service.md` → `007-api.md`; note that order in your summary.
- Parallel example: `005-ai.md` and `006-ui.md` only if they don’t touch the same files or directories; otherwise make them sequential.
- Name prompts descriptively and increment numbers.

## Decision gate and outputs
- Before generating, confirm you have enough context: “Proceed to generate prompt(s)?” with options (Proceed, Ask more questions, User adds context). Loop until Proceed; cap at 4 rounds by tightening questions each time.
- After saving:
  - Single prompt: “Saved ./prompts/NNN-name.md. Options: (1) run now, (2) review/edit, (3) defer.”
  - Sequential set: “Saved ./prompts/NNN-foo.md, ./prompts/(NNN+1)-bar.md… Run sequentially: NNN → NNN+1 → …. Options: (1) run sequence, (2) run first only, (3) review.”
  - Parallel set: “Saved ./prompts/NNN-foo.md, ./prompts/(NNN+1)-bar.md. These can run in parallel. Options: (1) run both now, (2) run one, (3) review.”
