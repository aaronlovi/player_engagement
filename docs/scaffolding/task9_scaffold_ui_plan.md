# Task 9 – Scaffold Angular Workspace Plan

Goal: generate the initial Angular workspace under `ui/player-engagement-config-ui/` with routing enabled and environment files prepared for the backend host.

**Status:** Completed — workspace scaffolded and quickstart/docs updated.

## Step-by-Step Checklist

- [x] Confirm context  
  Re-read Task 9 requirements in `docs/scaffolding/scaffolding_detailed_tasks.md` and the UI expectations in `docs/scaffolding/scaffolding_technical_requirements.md`.
- [x] Decide workspace options  
  Choose Angular CLI parameters (standalone components vs NgModule, SCSS vs CSS, strict templates) to align with scaffolding scope.
- [x] Generate workspace  
  Run Angular CLI to create `ui/player-engagement-config-ui/` with routing and default spec generation disabled.
- [x] Configure environment base URLs  
  Update `environment.ts` and `environment.development.ts` with the API base (`http://localhost:5000`) and note where future env overrides go.
- [x] Adjust npm scripts  
  Angular CLI scaffold already ships `start`, `build`, and placeholder `test`/`watch` scripts in `package.json`; no additional changes needed yet.
- [x] Capture proxy guidance  
  Document (via README inside the workspace or inline comment) how to add an Angular CLI proxy to forward `/xp/*` to the host when needed.
- [x] Initial smoke test  
  From the workspace run `npm install` and `npm run start -- --open` (or equivalent) to validate the scaffold builds; note any manual steps in the plan.
- [x] Update docs/backlog  
  Link the Angular workspace path and run instructions in `docs/scaffolding/scaffolding_detailed_tasks.md` notes or the quickstart as appropriate.

## Dependencies & References

- Requires the backend host (Tasks 1–8) to be in place for API wiring.
- Angular CLI 17+ per `scaffolding_technical_requirements.md`.
