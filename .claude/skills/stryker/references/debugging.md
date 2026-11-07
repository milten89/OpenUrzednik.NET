# Seeing what Stryker did

Part of `/stryker` ([SKILL.md](../SKILL.md)); notation for source paths and `sn#N` as in its introduction. Use this when a run, a rollback or a compile error needs explaining.

## Logging and diagnostics

| Need | Flag | What you get |
|---|---|---|
| Which mutants were rolled back and why | default `info` | `Safe Mode! Stryker will remove all mutations in <method>…` and `An unidentified mutation … resulted in a compile error` warnings (`Compiling/CSharpRollbackProcess.cs:232-240`) |
| Each rollback step | `--verbosity debug` | `RollBacking mutations from <file>`, `Found mutant <id> of type '<Mutator>'`, `Trying compilation for the N time` |
| The injected code | `--diag --verbosity trace` | the full mutated syntax tree per file (`MutationTest/CsharpMutationProcess.cs:50-53`) and compiler errors with source lines (`Dumping build error in file`). The messages come in the OS language without the `CS` id (Polish on this machine). The docs call this flag `--dev-mode`; the 5.0.0 CLI flag is `--diag` (`CommandLineConfigReader.cs:213`, sn#3977). |
| A log file | `-L` / `--log-to-file` | `<output>/logs/log-<date>.txt` |

## How CLI options combine with the config

The config file is read first, then the command line (`Stryker.CLI/Stryker.CLI/ConfigBuilder.cs:66-71`), and a repeated option such as `-m` or `-r` sets the whole list (`Stryker.CLI/Stryker.CLI/CommandLineConfig/CommandLineConfigReader.cs:175`). So `-m` drops the config's exclusions (`!**/Polyfills/**`, `!**/Dto/**`, `!**/Http/**`), and `-r html` alone drops the JSON report the scripts read.

A scoped run still mutates and compiles the whole project and filters by `-m` afterwards: the other files' mutants show as `Ignored` (`Removed by mutate filter`), and compile-error collateral is the same as in a full run.
