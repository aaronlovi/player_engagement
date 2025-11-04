# Task 10 – Angular Placeholder Page Plan

Goal: add an HttpClient-based service and temporary UI that surfaces the host's `/health` and `/xp` stub responses so the scaffolding proves end-to-end wiring.

**Status:** Completed — placeholder UI validated against host stubs.

## Step-by-Step Checklist

- [x] Confirm context  
  Revisit Task 10 description in `docs/scaffolding/scaffolding_detailed_tasks.md` and note expectations for placeholder data handling.
- [x] Define API interactions  
  Decide which endpoints to call (`/health/live`, `/health/ready`, `/xp/ledger`, `/xp/grants`) and the dummy payload structure we display.
- [x] Scaffold Angular service  
  Create a dedicated service (e.g., `XpApiService`) using `HttpClient` and the provided `API_BASE_URL` token; ensure stubs handle 501 responses gracefully.
- [x] Wire environment token  
  Confirm the service injects the base URL token and optionally expose a configuration interface for future expansion.
- [x] Build placeholder component  
  Implement a top-level component or route that calls the service on init, displays health status, and renders XP stub responses (status codes/message).
- [x] Handle loading/error states  
  Provide minimal UI feedback (loading spinner text, error banner) so later teams know where to extend behavior.
- [x] Manual smoke test  
  Run the host + Angular app concurrently and confirm the placeholder page renders data without console errors.
- [x] Update docs/backlog  
  Capture new commands/notes in `docs/scaffolding/quickstart.md` or backlog files reflecting the placeholder UI behavior.

## Dependencies & References

- Requires Task 9 workspace scaffold and Task 7 CORS setup.
- Host must be running locally (`dotnet run --project src/PlayerEngagement.Host`).
