# Prompt Helper Rules (portable)

Use this file when copying the helper prompts into other repositories. Follow the target repo’s own `AGENTS.md`/guides first; use these as shared defaults for the helpers.

## Reading order
- Read the target repo’s `AGENTS.md` (or equivalent) first. If it includes prompt-specific rules, they override these defaults (including tooling preferences).
- If anything is unspecified there, apply these rules.

## Practices
- Search with `rg`; avoid broad `find` unless necessary.
- Edit single files with `apply_patch` when possible; avoid destructive git commands.
- Default to ASCII; match existing style; add comments sparingly.
- Note sandbox/approval limits if present; state when tests/commands cannot be run.
- Do not overwrite the target repo’s governance files (e.g., `AGENTS.md`); defer to them where they differ.

## Naming and locations
- Helper prompts stay unnumbered in `/prompts`.
- Runnable prompts: `./prompts/NNN-name.md` (zero-padded; increment the highest existing number or start at `001`).
- Meta prompts/outputs: `.prompts/{slug}-{purpose}/` (purpose ∈ research, plan, do); no numeric prefixes. Derive slug: lowercase, hyphenate words, drop common stopwords.

## Metadata (meta outputs)
- Append `## Metadata` with `### Status` (use success/partial/failed), `### Confidence`, `### Dependencies`, `### Open Questions`, `### Assumptions` (use `None` when empty).

## Execution flow
- Read the relevant helper prompt (create/run/meta) before acting.
- Follow verification steps in the prompt; if blocked, record what to run later.
