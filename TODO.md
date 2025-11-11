# TODO Backlog

- **Split interface contracts**: Refactor the solution so shared interfaces live in dedicated projects whose names end with `.Contracts`, decoupling abstractions from their infrastructure implementations.
- **Operator RBAC for policy APIs**: Add role-based access control around policy management endpoints once multiple operators begin using the system, ensuring only authorized roles can create/publish/retire policies.
- **Segment-follow publishing**: Explore automatically updating segment overrides when a new policy version goes live (so cohorts follow the global active policy) if operator workflows start demanding it.
