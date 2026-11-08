# Architecture Decision Records

Decisions are recorded in [MADR](https://adr.github.io/madr/) format, see [ADR-0022](0022-record-architecture-decisions-frozen-at-release.md). An accepted record is binding and is corrected in place, with a dated line, until a release ships it; then it is `released` and frozen, and a changed decision gets a new record that supersedes the old one. Start new records from [`template.md`](template.md) (or run the `/adr` skill in Claude Code).

| # | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | superseded by [ADR-0022](0022-record-architecture-decisions-frozen-at-release.md) |
| [0002](0002-result-pattern-and-error-handling.md) | Result pattern and error handling | released (v1.0.0) |
| [0003](0003-telemetry-abstractions.md) | Telemetry abstractions and adapter packages | released (v1.0.0) |
| [0004](0004-dependency-policy.md) | Dependency policy | released (v1.0.0) |
| [0005](0005-target-frameworks.md) | Target frameworks and one public API | released (v1.0.0) |
| [0006](0006-shared-http-layer.md) | Shared HTTP layer for REST providers | superseded by [ADR-0019](0019-shared-http-layer-tested-through-test-host.md) |
| [0007](0007-client-api-and-extensibility.md) | Client API: simple defaults, internal request shaping | released (v1.0.0) |
| [0008](0008-branching-versioning-and-release.md) | Branching, versioning and release | released (v1.0.0) |
| [0009](0009-documentation-language.md) | Documentation language | released (v1.0.0) |
| [0010](0010-provider-readiness-gate.md) | Framework first: NBP as the reference provider | superseded by [ADR-0016](0016-next-providers-and-package-naming.md) |
| [0011](0011-compatibility-policy.md) | Compatibility policy | released (v1.0.0) |
| [0012](0012-error-types-describe-conditions.md) | Error types describe conditions, not transports | superseded by [ADR-0021](0021-transport-subclasses-of-condition-errors.md) |
| [0013](0013-version-support-policy.md) | Version support policy | released (v1.0.0) |
| [0014](0014-trimming-and-native-aot.md) | Trimming and Native AOT compatibility | released (v1.0.0) |
| [0015](0015-log-and-trace-correlation.md) | Log entries are written inside the operation's span | released (v1.0.0) |
| [0016](0016-next-providers-and-package-naming.md) | Next providers and package naming | accepted |
| [0017](0017-http-client-logging-for-identifier-paths.md) | HttpClient logging in DI packages of identifier-carrying providers | accepted |
| [0018](0018-mutation-testing-before-release.md) | Mutation testing as a required check before every release | superseded by [ADR-0020](0020-mutation-testing-of-every-target-branch.md) |
| [0019](0019-shared-http-layer-tested-through-test-host.md) | Shared HTTP layer for REST providers, tested through a test-only host | accepted |
| [0020](0020-mutation-testing-of-every-target-branch.md) | Mutation testing as a required check before every release, on every target branch | accepted |
| [0021](0021-transport-subclasses-of-condition-errors.md) | Error details: transport packages, typed subclasses and a JSON-shaped metadata view | accepted |
| [0022](0022-record-architecture-decisions-frozen-at-release.md) | Record architecture decisions, frozen at release | accepted |
