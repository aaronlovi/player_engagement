# Workflow Guidelines

To keep the XP service scaffolding healthy, follow these checkpoints for every change:

1. **Build before you call it done** – run `dotnet build` (or the equivalent build command) after finishing any task or plan step. Do not mark the work complete until the build succeeds.
2. **Run tests whenever they exist** – if unit/integration tests are already in place, execute them before declaring success. If tests are missing, note that as a follow-up and plan to add them.
3. **Keep tasks atomic** – focus changes on the specific task/plan step and verify the results immediately. Avoid rolling forward with known build/test failures.
4. **Favor application-layer logic** – keep domain rules, projections, and invariants in C# services where possible. Use database triggers/functions only when there’s a clear operational need and document the rationale when you do.
5. **Document deviations** – if a task can’t meet these standards (e.g., blocked on external dependency), record the reason and next steps in the relevant planning doc.

Refer back to this checklist before finalizing any task.
