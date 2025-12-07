# Codex Meta-Prompt Creator (Research/Plan/Do)

Build a Codex-to-Codex pipeline that can chain tasks and prompts reliably. Ensure Codex writes the prompts it needs efficiently while still meeting the user's business outcomes. Use this to generate dependency-aware prompts for research → plan → do workflows, producing Markdown-structured outputs (no XML) that chain cleanly. Optimize for Codex tooling, lean intake, and safe execution.

## Usage
- Start by reading the target repo’s `AGENTS.md`; if prompt-specific rules are absent, fall back to `prompts/PROMPT_RULES.md`. Skim `README.md`/`CONTRIBUTING.md` and list relevant files (`rg --files`) before questions.
- Invoke with a task description `$TASK`.
- Ask 1–5 outcome-focused questions to lock down scope and success criteria; skip questions if answers are obvious from context; stop once purpose and success outcomes are clear; confirm test/verification commands if manifests suggest them.
- Purposes:
  - Research: gather information, comparisons, risks.
  - Plan: turn research into a concrete implementation plan.
  - Do: implement according to a plan or, if none, a minimal self-authored plan.
- If purpose is ambiguous, propose the best-fit purpose with a brief rationale and let the user confirm.
- Note on file locations: the helper prompts in `/prompts` (this file, create-prompt, run-prompt) stay unnumbered; numbered runnable prompts still belong under `./prompts/NNN-name.md`; slugged meta outputs belong under `.prompts/{slug}-{purpose}/`.

## Dependency detection
- Derive the topic slug: lowercase, hyphenate words, drop common stopwords (a, an, the, and, or, of, for, to). Use this slug for creation and lookup.
- Reuse an existing slug when possible; append a variant suffix (e.g., `-2`) only if collision is unavoidable.
- Scan for existing research/plan outputs under `.prompts/**/{slug}-{purpose}/` whose folder/filename contains the slug; prefer the most recent per purpose. Include numbered prompts under `./prompts/` only if they contain relevant research/plan content to reference.
- Present up to 5 matches with short descriptions; ask which (if any) to reference. Default to none if the user declines or matches are weak.
- Split into parallel branches only when topics are independent or exploratory; otherwise stay sequential. If you create multiple research branches, suffix slugs (`{slug}-research-a`, `-b`) and require a reconciliation step naming the canonical branch before any plan/do work proceeds.

## Prompt generation
- Meta prompts: name descriptively without numeric prefixes; format: `.prompts/{slug}-{purpose}/prompt.md` (slug = short hyphenated topic, purpose ∈ {research, plan, do}); meta-prompt artifacts stay slugged.
- Numbered runnable prompts: keep using `./prompts/NNN-name.md` for standard tasks. Leave the helper files in `/prompts` unnumbered.
- To run via the standard runner, surface the exact path(s) to pass into the flow from `prompts/run-prompt.md` (e.g., target `.prompts/{slug}-{purpose}/prompt.md` explicitly). If numbered prompts are relevant references, list them under “Key references.”
- After execution, move `prompt.md` to `.prompts/{slug}-{purpose}/completed/prompt.md` only on success; do not auto-archive. If execution fails or is partial, leave the prompt in place and record status in the output file.
- Structure the generated prompt with concise sections (omit unused):
  - Objective
  - Context (stack, constraints, key files, chosen references)
  - Requirements (functional + non-functional)
  - Plan (ordered steps; reflect between steps where useful)
  - Outputs (explicit file paths)
  - Verification (commands + manual checks; if sandboxed, state what to run later)
  - Success Criteria (measurable outcomes)
  - Practices (rg for search, apply_patch for single-file edits, avoid destructive git commands, ASCII by default)

## Output expectations (Markdown metadata)
- Research output file: `.prompts/{slug}-research/research.md`
- Plan output file: `.prompts/{slug}-plan/plan.md`
- Do/implementation uses the repo itself; by default add a brief `.prompts/{slug}-do/implementation-notes.md` for discoverability; only co-locate with the primary change if that improves clarity. Keep location consistent per task.
- For research, plan, and any implementation notes, append a `## Metadata` block with Markdown subsections (no XML):
  - `### Status` — success / partial / failed (for implementation, note if follow-up is needed; use this vocabulary consistently).
  - `### Confidence` — brief text or percentage.
  - `### Dependencies` — bullet list of relied-on files/decisions.
  - `### Open Questions` — bullet list; `None` if empty.
  - `### Assumptions` — bullet list; `None` if empty.

## Execution flow
1) Clarify purpose and scope (minimal questions).
2) Detect and confirm references to chain from; note if multiple branches should proceed in parallel vs sequentially.
3) Generate the prompt as above and save to `.prompts/{slug}-{purpose}/prompt.md`.
4) Offer next action: run now via the Codex runner flow in `prompts/run-prompt.md` (sequential by default unless branches are safely parallel) or review first; remind where outputs and metadata should go. Suggest listing candidates with `ls -t ./.prompts/*/*/prompt.md 2>/dev/null` (or `rg --files -g 'prompt.md' ./.prompts -g '!*/completed/*'`) when selecting slugged prompts; avoid archived `/completed/` paths and adjust the glob if nested deeper.
5) If running, follow `prompts/run-prompt.md` safety rules; respect sandbox and avoid unsolicited escalations; if blocked, note commands to run later. On success archive the prompt; on failure leave it in place and record status in the output/notes.

## Examples
- Slug derivation: "Add checkout fraud checks" → `checkout-fraud-checks`.
- Runner handoff: target `.prompts/checkout-fraud-checks-plan/prompt.md` (or the relevant purpose) when invoking the flow in `prompts/run-prompt.md`; expect outputs in `.prompts/checkout-fraud-checks-plan/plan.md` with a `## Metadata` block.
- Chained flow: `./.prompts/checkout-fraud-checks-research/prompt.md` → summarize findings in `research.md` (with metadata), then run `./.prompts/checkout-fraud-checks-plan/prompt.md` to produce `plan.md`, then execute a do-prompt to implement; archive each successful prompt to its `/completed/` folder.

## Sandboxing and style
- Use `rg` for search; prefer `apply_patch` for single-file edits; avoid destructive git commands.
- Default to ASCII; match existing style; add comments only when clarifying non-obvious logic.
- Avoid XML; use Markdown metadata blocks.
- If sandbox blocks commands, note what should be run later.
- Do not request approvals when disallowed; if blocked, note what should be run when permitted.
