# How Stryker rolls back compile errors

Part of `/stryker` ([SKILL.md](../SKILL.md), section 5); notation for source paths and `sn#N` as in its introduction. Read it when a CompileError doesn't match a shape in the section 5 table, or to check a `// Stryker disable` comment on an uncompilable mutant.

## The rollback

Stryker puts all mutants into one compilation (`IsActive(n) ? mutated : original`). When that fails, `CSharpRollbackProcess` removes mutants until it compiles (up to 50 attempts, `Compiling/CsharpCompilingProcess.cs:38`) and marks every removed one `CompileError`:

- For each compiler error it takes the node at the error and walks **up** to the nearest enclosing mutant (`FindMutationWithNode`, `CSharpRollbackProcess.cs:93-112`). That mutant is blamed even if it compiles fine; the broken one may be a sibling or a child.
- CS0165/CS0177 (unassigned variable) first look for a mutant that erases the assignment (`ScanErasingMutation`); a negated pattern variable isn't recognised, so it falls through to the walk-up from the variable's use.
- If no mutant is found, **Safe Mode** rolls back every mutant in the enclosing method, accessor or constructor, or in the whole file (`ScanForSuspiciousMutations`, lines 210-249).

To watch it happen, use the flags in [debugging.md](debugging.md).

## Why configuration can't free collateral

`ignore-mutations` (even `["linq"]`) and `ignore-methods` filter **after** compilation and skip CompileError mutants (`MutationTest/CsharpMutationProcess.cs:118-126`, sn#962; verified on the core shard and a repro). Only a `// Stryker disable` comment acts before compilation: the mutant is created as Ignored (`Mutants/CsharpMutantOrchestrator.cs:170-176`) and only Pending mutants are injected (`Mutants/MutationStore.cs:267`), so its neighbours compile.
