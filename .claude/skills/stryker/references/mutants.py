# /// script
# requires-python = ">=3.12"
# ///
"""Read Stryker.NET mutation reports (mutation-testing-report-schema v2, reports/mutation-report.json).

Usage (from the repository root):
    uv run .claude/skills/stryker/references/mutants.py list <report.json> [--all] [--file <substring>] [--tests]
    uv run .claude/skills/stryker/references/mutants.py diff <before.json> <after.json> [--file <substring>] [--ignore-lines]

list: every mutant that isn't Killed or Ignored (with --all: every mutant), per file, with line, status, mutator,
      replacement and the status reason; --tests adds the names of the tests that killed or cover it.
diff: matches mutants of two reports by (file, line, mutator, replacement) and prints the ones whose status changed,
      appeared or disappeared. Ids aren't stable between runs, so they aren't used. Lines move when code changes above
      a mutant: a moved mutant shows as one removed and one added entry, unless --ignore-lines matches by file, mutator
      and replacement only (statuses of identical mutants in one file are then compared as a sorted list).
"""

from __future__ import annotations

import json
import sys
from collections import Counter
from pathlib import Path

HIDDEN = {"Killed", "Ignored"}


def load(path: str) -> dict:
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def repo_relative(name: str) -> str:
    """Key a file relative to the repository root, so that reports from two checkouts (a worktree, CI) compare.

    File keys are absolute (sn#3725) and projectRoot is the shard's workdir, which for the test-only hosts isn't above
    the mutated files (src/...). Cut everything before the first `src` or `tests` segment instead.
    """
    parts = name.replace("\\", "/").split("/")
    for i, part in enumerate(parts):
        if part in ("src", "tests"):
            return "/".join(parts[i:])
    return "/".join(parts)


def mutants(report: dict, file_filter: str | None):
    for name, file in sorted(report.get("files", {}).items()):
        name = repo_relative(name)
        if file_filter and file_filter not in name:
            continue
        for m in file.get("mutants", []):
            yield name, m


def key(name: str, m: dict) -> tuple:
    line = m.get("location", {}).get("start", {}).get("line", 0)
    return name, line, m.get("mutatorName", "?"), " ".join(str(m.get("replacement", "")).split())


def test_names(report: dict) -> dict[str, str]:
    names = {}
    for tf in report.get("testFiles", {}).values():
        for t in tf.get("tests", []):
            names[t["id"]] = t.get("name", t["id"])
    return names


def short(test: str) -> str:
    """`Ns.Class.Method(Ns.Type)` -> `Method(Ns.Type)`: drop the namespace and class, keep the arguments."""
    head, paren, args = test.partition("(")
    return head.rsplit(".", 1)[-1] + paren + args


def cmd_list(report: dict, show_all: bool, file_filter: str | None, tests: bool) -> None:
    counts = Counter()
    names = test_names(report) if tests else {}
    current = None
    for name, m in mutants(report, file_filter):
        status = m.get("status", "?")
        counts[status] += 1
        if not show_all and status in HIDDEN:
            continue
        if name != current:
            print(f"\n{name}")
            current = name
        _, line, mutator, replacement = key(name, m)
        reason = m.get("statusReason") or ""
        print(f"  {line:>5} {status:<12} {mutator}: `{replacement[:70]}`" + (f"  ({reason})" if reason else ""))
        if tests:
            for label in ("killedBy", "coveredBy"):
                ids = m.get(label) or []
                if ids:
                    shown = ", ".join(short(names.get(i, i[:12])) for i in ids[:5])
                    print(f"        {label} ({len(ids)}): {shown}{' …' if len(ids) > 5 else ''}")
    print("\n" + ", ".join(f"{s}: {n}" for s, n in sorted(counts.items())))


def cmd_diff(before: dict, after: dict, file_filter: str | None, ignore_lines: bool) -> None:
    def index(report: dict) -> dict[tuple, list[str]]:
        out: dict[tuple, list[str]] = {}
        for name, m in mutants(report, file_filter):
            k = key(name, m)
            if ignore_lines:
                k = (k[0], 0, k[2], k[3])
            out.setdefault(k, []).append(m.get("status", "?"))
        return out

    a, b = index(before), index(after)
    changed = 0
    for k in sorted(set(a) | set(b), key=lambda k: (k[0], k[1], k[2], k[3])):
        sa, sb = sorted(a.get(k, [])), sorted(b.get(k, []))
        if sa == sb:
            continue
        name, line, mutator, replacement = k
        left = "/".join(sa) or "-"
        right = "/".join(sb) or "-"
        print(f"{name}:{line} {mutator}: `{replacement[:60]}`  {left} -> {right}")
        changed += 1
    if not changed:
        print("no changes")


def main(argv: list[str]) -> int:
    if len(argv) < 2 or argv[0] not in {"list", "diff"}:
        print(__doc__)
        return 2
    file_filter = None
    if "--file" in argv:
        i = argv.index("--file")
        if i + 1 >= len(argv):
            print("--file needs a value")
            return 2
        file_filter = argv[i + 1]
    paths = [a for a in argv[1:] if a.endswith(".json")]
    if len(paths) < (1 if argv[0] == "list" else 2):
        print(__doc__)
        return 2
    if argv[0] == "list":
        cmd_list(load(paths[0]), "--all" in argv, file_filter, "--tests" in argv)
    else:
        cmd_diff(load(paths[0]), load(paths[1]), file_filter, "--ignore-lines" in argv)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
