# TODO Backlog

- **Split interface contracts**: Refactor the solution so shared interfaces live in dedicated projects whose names end with `.Contracts`, decoupling abstractions from their infrastructure implementations.
- **Operator RBAC for policy APIs**: Add role-based access control around policy management endpoints once multiple operators begin using the system, ensuring only authorized roles can create/publish/retire policies.
- **Segment-follow publishing**: Explore automatically updating segment overrides when a new policy version goes live (so cohorts follow the global active policy) if operator workflows start demanding it.
- **Test object factories**: Refactor test factories (e.g., `PolicyDtoFactory`) to accept optional/defaultable parameters for every member instead of hard-coded values to improve reuse and readability. **Done for policy DTOs.**
- **DateTime provider**: Introduce a time provider abstraction to eliminate direct `DateTime.UtcNow` usage; include a configurable test provider to control time in unit tests.
- **Fixed decimal type**: Define a shared fixed-decimal shape for multipliers to avoid float drift end-to-end (e.g., `FixedDecimal { whole:int64 (keep within JS safe integer), nanos:int32 (0..999_999_999) }` with a TS/JS equivalent and helpers to convert to/from decimal/double).
