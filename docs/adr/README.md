# Architecture Decision Records

Decisions are recorded in [MADR](https://adr.github.io/madr/) format, see [ADR-0001](0001-record-architecture-decisions.md). The log is append-only: an accepted record isn't edited, and a changed decision gets a new record that supersedes the old one. Start new records from [`template.md`](template.md) (or run the `/adr` skill in Claude Code).

| # | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | accepted |
| [0002](0002-result-pattern-and-error-handling.md) | Result pattern and error handling | accepted |
| [0003](0003-telemetry-abstractions.md) | Telemetry abstractions and adapter packages | accepted |
| [0004](0004-dependency-policy.md) | Dependency policy | accepted |
| [0005](0005-target-frameworks.md) | Target frameworks and one public API | accepted |
| [0006](0006-shared-http-layer.md) | Shared HTTP layer for REST providers | accepted |
| [0007](0007-client-api-and-extensibility.md) | Client API: simple defaults, internal request shaping | accepted |
| [0008](0008-branching-versioning-and-release.md) | Branching, versioning and release | accepted |
| [0009](0009-documentation-language.md) | Documentation language | accepted |
| [0010](0010-provider-readiness-gate.md) | Framework first: NBP as the reference provider | superseded by [ADR-0016](0016-next-providers-and-package-naming.md) |
| [0011](0011-compatibility-policy.md) | Compatibility policy | accepted |
| [0012](0012-error-types-describe-conditions.md) | Error types describe conditions, not transports | accepted |
| [0013](0013-version-support-policy.md) | Version support policy | accepted |
| [0014](0014-trimming-and-native-aot.md) | Trimming and Native AOT compatibility | accepted |
| [0015](0015-log-and-trace-correlation.md) | Log entries are written inside the operation's span | accepted |
| [0016](0016-next-providers-and-package-naming.md) | Next providers and package naming | accepted |
| [0017](0017-http-client-logging-for-identifier-paths.md) | HttpClient logging in DI packages of identifier-carrying providers | accepted |
