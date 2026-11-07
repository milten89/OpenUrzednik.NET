# /// script
# requires-python = ">=3.12"
# ///
"""Run the Stryker.NET mutation shards and check them against their gates (ADR-0018).

Usage (from the repository root):
    uv run tests/Mutation/run.py <shard> [<shard> ...]    # locally
    python3 tests/Mutation/run.py <shard> [<shard> ...]   # CI
    uv run tests/Mutation/run.py --all
    uv run tests/Mutation/run.py --list

Each shard (tests/Mutation/shards.json) runs `dotnet stryker` with its config into artifacts/mutation/<shard>, then this script reads
reports/mutation-report.json and fails the shard when:
  * Stryker exits non-zero, or no mutant was tested (Stryker exits 0 when it can't compute a score),
  * a strict glob matches no file of the report (a typo or a moved folder would turn its check off),
  * a mutant in a strict file survived or has no coverage, or
  * the mutation score is below the shard's `break` value.
It writes a markdown summary to artifacts/mutation/summary.md (CI copies it into the job summary).
Exit codes: 0 every shard passed, 1 a gate failed, 2 a shard couldn't run or its report is missing.
"""

from __future__ import annotations

import json
import os
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SHARDS_FILE = ROOT / "tests" / "Mutation" / "shards.json"
OUTPUT_ROOT = ROOT / "artifacts" / "mutation"

DETECTED = {"Killed", "Timeout"}
UNDETECTED = {"Survived", "NoCoverage"}


@dataclass
class Shard:
    name: str
    workdir: Path
    config: Path
    strict: list[str]
    break_at: float


@dataclass
class Result:
    shard: Shard
    counts: dict[str, int] = field(default_factory=dict)
    score: float | None = None
    strict_survivors: list[str] = field(default_factory=list)
    unmatched_strict: list[str] = field(default_factory=list)
    other_survivors: int = 0
    error: str | None = None

    @property
    def tested(self) -> int:
        return sum(self.counts.get(status, 0) for status in DETECTED | UNDETECTED)

    @property
    def passed(self) -> bool:
        return (
            self.error is None
            and self.tested > 0
            and not self.strict_survivors
            and not self.unmatched_strict
            and self.score is not None
            and self.score >= self.shard.break_at
        )


def glob_to_regex(pattern: str) -> re.Pattern[str]:
    """A repository-relative glob (`**` crosses directories, `*` and `?` don't) as an anchored regex."""
    out = []
    i = 0
    while i < len(pattern):
        if pattern.startswith("**/", i):
            out.append("(?:.*/)?")
            i += 3
        elif pattern.startswith("**", i):
            out.append(".*")
            i += 2
        elif pattern[i] == "*":
            out.append("[^/]*")
            i += 1
        elif pattern[i] == "?":
            out.append("[^/]")
            i += 1
        else:
            out.append(re.escape(pattern[i]))
            i += 1
    return re.compile("^" + "".join(out) + "$")


def load_shards() -> dict[str, Shard]:
    data = json.loads(SHARDS_FILE.read_text(encoding="utf-8"))
    return {
        name: Shard(
            name=name,
            workdir=ROOT / spec["workdir"],
            config=ROOT / spec["config"],
            strict=spec.get("strict", []),
            break_at=float(spec.get("break", 0)),
        )
        for name, spec in data["shards"].items()
    }


def run_stryker(shard: Shard, output: Path) -> int:
    config = os.path.relpath(shard.config, shard.workdir)
    command = ["dotnet", "stryker", "--config-file", config, "--output", str(output)]
    # Stryker 5's MTP runner passes the active mutant through a fixed file in the temp folder (stryker-mutant-0.txt), so two runs
    # sharing one temp folder corrupt each other's results. Each shard gets its own.
    temp = output / "tmp"
    temp.mkdir(parents=True, exist_ok=True)
    env = {**os.environ, "TEMP": str(temp), "TMP": str(temp), "TMPDIR": str(temp)}
    print(f"::group::{shard.name}: {' '.join(command)} (in {shard.workdir.relative_to(ROOT).as_posix()})", flush=True)
    try:
        return subprocess.run(command, cwd=shard.workdir, env=env, check=False).returncode
    finally:
        print("::endgroup::", flush=True)


def check(shard: Shard, report_path: Path) -> Result:
    result = Result(shard)
    if not report_path.is_file():
        result.error = f"no report at {report_path.relative_to(ROOT).as_posix()}"
        return result

    report = json.loads(report_path.read_text(encoding="utf-8"))
    project_root = Path(report.get("projectRoot") or shard.workdir)
    strict = {pattern: glob_to_regex(pattern) for pattern in shard.strict}
    matched: set[str] = set()
    for file_name, file in report.get("files", {}).items():
        path = Path(file_name)
        absolute = path if path.is_absolute() else project_root / path
        relative = os.path.relpath(absolute.resolve(), ROOT).replace(os.sep, "/")
        hits = {pattern for pattern, regex in strict.items() if regex.match(relative)}
        matched |= hits
        is_strict = bool(hits)
        for mutant in file.get("mutants", []):
            status = mutant.get("status", "Unknown")
            result.counts[status] = result.counts.get(status, 0) + 1
            if status not in UNDETECTED:
                continue
            if is_strict:
                line = mutant.get("location", {}).get("start", {}).get("line", "?")
                replacement = " ".join(str(mutant.get("replacement", "")).split())
                result.strict_survivors.append(f"{relative}:{line} {status} {mutant.get('mutatorName', '?')}: `{replacement[:80]}`")
            else:
                result.other_survivors += 1

    result.unmatched_strict = [pattern for pattern in shard.strict if pattern not in matched]
    if result.tested:
        detected = sum(result.counts.get(status, 0) for status in DETECTED)
        result.score = 100.0 * detected / result.tested
    return result


def summary(results: list[Result]) -> str:
    lines = [
        "| Shard | Result | Score | Break | Killed | Timeout | Survived | No coverage | Strict survivors |",
        "|---|---|---|---|---|---|---|---|---|",
    ]
    for r in results:
        verdict = "✅ pass" if r.passed else "❌ fail"
        score = "—" if r.score is None else f"{r.score:.2f}%"
        lines.append(
            f"| {r.shard.name} | {verdict} | {score} | {r.shard.break_at:g}% | {r.counts.get('Killed', 0)} | {r.counts.get('Timeout', 0)} "
            f"| {r.counts.get('Survived', 0)} | {r.counts.get('NoCoverage', 0)} | {len(r.strict_survivors)} |"
        )
    for r in results:
        if r.error:
            lines += ["", f"**{r.shard.name}:** {r.error}"]
        elif r.tested == 0:
            lines += ["", f"**{r.shard.name}:** no mutant was tested (Stryker found no score)."]
        if r.unmatched_strict:
            lines += ["", f"**{r.shard.name}:** strict globs matching no mutated file: {', '.join(r.unmatched_strict)}"]
        if r.strict_survivors:
            lines += ["", f"**{r.shard.name}: undetected mutants in strict files**", ""]
            lines += [f"- {survivor}" for survivor in r.strict_survivors]
    return "\n".join(lines) + "\n"


def main(argv: list[str]) -> int:
    shards = load_shards()
    if not argv or argv == ["--help"]:
        print(__doc__)
        return 2
    if argv == ["--list"]:
        print("\n".join(shards))
        return 0
    names = list(shards) if argv == ["--all"] else argv
    unknown = [name for name in names if name not in shards]
    if unknown:
        print(f"Unknown shard(s): {', '.join(unknown)}. Known: {', '.join(shards)}", file=sys.stderr)
        return 2

    results = []
    for name in names:
        shard = shards[name]
        output = OUTPUT_ROOT / name
        # A failed run must not leave an older report behind to be checked instead.
        shutil.rmtree(output / "reports", ignore_errors=True)
        exit_code = run_stryker(shard, output)
        result = check(shard, output / "reports" / "mutation-report.json")
        if exit_code != 0:
            # No Stryker threshold is configured (the break check is ours), so a non-zero exit is always a failed run.
            result.error = f"dotnet stryker exited with {exit_code}"
        results.append(result)

    text = summary(results)
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    (OUTPUT_ROOT / "summary.md").write_text(text, encoding="utf-8")
    print(text)

    if any(r.error or r.tested == 0 or r.unmatched_strict for r in results):
        return 2
    return 0 if all(r.passed for r in results) else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
