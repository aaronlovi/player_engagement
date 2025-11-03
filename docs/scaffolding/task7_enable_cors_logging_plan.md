# Task 7 – Enable CORS and Request Logging Plan

Goal: permit local UI origins while adding lightweight request logging to the Orleans host without introducing business logic.

**Status:** In progress — context reviewed and ready to proceed with configuration decisions.

## Step-by-Step Checklist

- [x] Confirm context  
  Review Task 7 requirements in `docs/scaffolding/scaffolding_detailed_tasks.md` and current host configuration in `src/PlayerEngagement.Host/Program.cs`.
- [x] Decide configuration layout  
  Keep CORS policy registration in `Program.cs` for visibility, but delegate request logging to an extension method (e.g., `LoggingPipelineExtensions`) so middleware wiring stays readable as features grow.
- [x] Define allowed origins  
  Allow `http://localhost:4200` (Angular dev server). Document additional origins later as new clients appear.
- [x] Register CORS services  
  Added `LocalDevCors` policy in `Program.cs` allowing `http://localhost:4200` with any header/method.
- [ ] Apply CORS middleware  
  Use `app.UseCors("<policy-name>")` before endpoint mappings; document ordering to avoid regressions.
- [ ] Configure request logging  
  Add standard ASP.NET Core logging (e.g., `UseHttpLogging` or custom middleware) that logs method, path, status code, and duration without PII.
- [ ] Verify middleware ordering  
  Confirm Orleans dashboard (if enabled later), CORS, and logging middlwares execute in correct order with minimal pipeline changes.
- [ ] Manual verification  
  Run `dotnet run --project src/PlayerEngagement.Host` and issue test requests (including an `Origin` header) to ensure CORS headers and logs appear as expected.
- [ ] Update documentation  
  Note the CORS policy and logging approach in `docs/scaffolding/quickstart.md` or relevant docs once Task 8 is executed.

## Dependencies & References

- Requires completion of Task 6 Minimal API stubs.
- Review `docs/scaffolding/scaffolding_technical_requirements.md` for logging scope expectations.
