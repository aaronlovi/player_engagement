# Codex Prompt Runner (General)

Use this to select and execute prompt files in `./prompts/` (numbered) or explicit prompt paths (e.g., slugged `.prompts/{slug}-{purpose}/prompt.md`) within the current Codex context. Helpers in `/prompts` are not runnable tasks. Default to safe, sequential execution.

## Usage
- Invocation argument `$TARGETS`: empty → most recent numbered prompt; number(s) or partial name(s) → resolve matching files; explicit paths are allowed (including `.prompts/**/prompt.md`); optional `--parallel` or `--sequential` flag (default sequential).
- Run only prompts you explicitly resolved; do not move/rename prompt files unless instructed.

## Safety and sandbox
- Respect current sandbox/approval constraints; do not request escalations if disallowed. Follow the target repo’s `AGENTS.md`; if absent for prompt work, use shared defaults in `prompts/PROMPT_RULES.md`.
- Prefer read/search commands; avoid destructive git commands; do not commit unless the user explicitly asks.
- Parallel only if prompts touch disjoint files/directories; otherwise force sequential.

## Steps
1) Snapshot context (optional but helpful):
   - `git status --short`
   - `ls -t ./prompts/*.md | head -5` (ignore if none)
   - `find ./.prompts -name prompt.md -not -path '*/completed/*' -type f 2>/dev/null | sort -r | head -5` (helpful for slugged meta prompts by mtime across depths)
   - `rg --files -g 'prompt.md' ./.prompts -g '!*/completed/*' 2>/dev/null` (quick list of all slugged prompt files; ignore paths under `/completed/`)
2) Parse `$TARGETS`:
   - Flags: `--parallel`, `--sequential` (default sequential if multiple targets).
   - Targets: numbers map to zero-padded prefixes (`5` → `005-*`); text matches substring in filename; explicit paths are used as-is. Sort filenames deterministically.
   - Empty/`last`: pick most recent numbered prompt by mtime under `./prompts/`; slugged prompts require explicit selection or path.
   - If you explicitly pass non-numbered targets (e.g., slugged meta prompt paths), order by mtime when running multiples. Runnable prompts under `./prompts/` should follow numeric prefixes; helper files in `/prompts` stay unnumbered.
3) Resolve files:
   - If one match per target, use it.
   - If multiple matches, list options with indices and ask the user to choose; default to sequential ordering and force disambiguation. Exclude archived prompts under `/completed/` unless explicitly requested.
   - If none, report and list available prompts.
4) Decide execution strategy:
   - Single prompt: run it.
   - Multiple prompts: order by numeric prefix/filename; sequential by default; parallel only if confirmed non-overlapping.
   - If user requested parallel but overlap is unclear, ask or switch to sequential.
   - If >5 prompts selected, warn about scope and confirm before proceeding.
5) Confirmation gate:
   - Present resolved prompts and strategy. Ask: Proceed / Reselect / Abort. If still ambiguous after two passes, default to sequential with the current set.
6) Execute (sequential recommended):
   - Read the prompt file.
   - Follow its instructions faithfully (use `rg` for search, `apply_patch` for edits, respect style/sandbox notes). Read linked repo conventions (README/CONTRIBUTING/AGENTS/etc.) before edits. Honor prompt-specific verification/sandbox directions. For chained meta prompts, read the latest metadata in referenced research/plan/implementation notes before proceeding; expect `### Status` to be one of success/partial/failed.
   - Run tests/commands only if allowed; if blocked by sandbox/approval, record what to run later.
   - Complete one prompt before starting the next.
7) Parallel note:
   - If truly independent and safe, you may interleave work, but ensure no file conflicts. Check prompt contents for overlapping paths; if uncertain, force sequential.
   - To check overlap, scan each prompt for file paths (`./...`) and avoid parallel if any intersect. For slugged meta prompts, read referenced file paths and metadata to confirm separation.
8) Wrap-up:
   - Summarize what was done per prompt.
   - Note any skipped steps due to sandbox (e.g., tests not run) and what to run later; record assumptions.
   - Do not auto-archive or commit; only do so if explicitly requested.

## Output format
- For each prompt: `✓ Executed: <prompt-path>` plus a brief result summary.
- For multiple prompts: indicate strategy (sequential/parallel), list prompts with status, and provide a consolidated summary.
