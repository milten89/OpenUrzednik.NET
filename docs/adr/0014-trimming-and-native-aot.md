---
status: accepted
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0014. Trimming and Native AOT compatibility

## Context and Problem Statement

The libraries' .NET builds set `IsAotCompatible` ([ADR-0005](0005-target-frameworks.md)), so the trimming and AOT analyzers (IL2xxx/IL3xxx) run on their own code. The analyzers don't see code across assemblies or in dependencies, and Microsoft recommends also publishing a trimmed test app, because a dependency update can bring new warnings ([Prepare .NET libraries for trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)). Apps that publish with trimming or Native AOT need to know whether they can use the packages. Is trimming and Native AOT compatibility part of the contract, for which packages, and how is it checked?

## Decision Drivers

* Apps that publish with trimming or Native AOT can rely on the packages without testing them themselves.
* A promise in the READMEs is documented behaviour, covered within a major version ([ADR-0011](0011-compatibility-policy.md)).
* Future providers may use other transports, e.g. SOAP for GUS ([ADR-0010](0010-provider-readiness-gate.md)).

## Considered Options

* Every published package is compatible on its .NET builds
* Per package: today's packages promise it, and each new provider decides in its provider ADR
* No promise: the CI check is an internal quality gate only

## Decision Outcome

Chosen option: "Every published package is compatible on its .NET builds", because one rule holds for the whole family and every new package is checked like the existing ones.

* The .NET builds (`net8.0`, `net10.0`) of every published package are trimming- and Native AOT-compatible: publishing an app with trimming or Native AOT gives no trimming or AOT warning from them. The `netstandard2.0` build isn't covered (.NET Framework has neither).
* The package READMEs and the root README state it.
* A dependency of a published package must be trimming- and AOT-safe on the .NET targets ([ADR-0004](0004-dependency-policy.md)).
* A future provider uses AOT-safe code for its transport: e.g. for SOAP, `XmlReader`/`XmlWriter` or source-generated serialization, not the reflection-based `XmlSerializer`. Its provider ADR designs it ([ADR-0010](0010-provider-readiness-gate.md)).
* Every new published package joins the AOT smoke app (below) in the PR that adds it.

### Consequences

* Good, because apps that publish with trimming or Native AOT can use every package, and a new package can't silently break that.
* Good, because a dependency update that brings trimming or AOT warnings fails CI before it ships.
* Bad, because a dependency or transport library that isn't AOT-safe can't be used, even where it would be the easiest choice.
* Bad, because CI does a Native AOT publish on every PR, which takes a few minutes.

### Confirmation

* The analyzers check the libraries' own code on both .NET targets (`IsAotCompatible`, [ADR-0005](0005-target-frameworks.md)); in CI their warnings are errors.
* The CI job `aot (net10.0)` publishes `tests/OpenUrzednik.AotSmoke` with Native AOT, every published `src/` assembly rooted (`TrimmerRootAssembly`) and warnings as errors, so a warning in the libraries or their dependencies fails it. It then runs the native binary, which calls the clients offline with responses captured from the real API, and fails on a wrong result.
* The `reviewer` agent flags a new published package that isn't in the smoke app, and a dependency or code path that isn't trimming- or AOT-safe.

## Pros and Cons of the Options

### Per package

* Good, because a provider could use a library that isn't AOT-safe, e.g. for SOAP.
* Bad, because users would have to check each package's README before relying on it.

### No promise

* Good, because nothing becomes part of the contract.
* Bad, because apps would have to test the packages under trimming or Native AOT themselves.

## More Information

Introduced in [#118](https://github.com/milten89/OpenUrzednik.NET/issues/118) (audit finding g26), with the smoke app and the CI job. An accepted record isn't edited later ([ADR-0001](0001-record-architecture-decisions.md)): a changed decision gets a new record that supersedes this one.
