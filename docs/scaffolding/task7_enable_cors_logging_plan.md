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
- [x] Apply CORS middleware  
  Added `app.UseCors(CorsPolicyName)` prior to endpoint mapping to ensure responses include CORS headers.
- [x] Configure request logging  
  Registered `AddHttpLogging` with method/path/status fields and wired `app.UseRequestPipelineLogging()` extension around `UseHttpLogging`.
- [x] Verify middleware ordering  
  Positioned `UseRequestPipelineLogging` ahead of `UseCors` so requests are logged regardless of CORS outcome; Orleans dashboard remains disabled for now so no further ordering needed.
- [ ] Manual verification  
  Blocked: `dotnet run` currently waits on the Postgres-backed `EnsureDatabaseAsync` health check, so endpoint probing needs the compose stack running before re-testing CORS/log headers.
- [ ] Update documentation  
  Note the CORS policy and logging approach in `docs/scaffolding/quickstart.md` or relevant docs once Task 8 is executed.

## Dependencies & References

- Requires completion of Task 6 Minimal API stubs.
- Review `docs/scaffolding/scaffolding_technical_requirements.md` for logging scope expectations.
