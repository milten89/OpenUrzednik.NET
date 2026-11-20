"""Move every package's unshipped public API to its shipped list, on a release branch (/prepare-release, step 3).

Usage: python ship_public_api.py [repository root]   (default: the current directory)

For each src/**/PublicAPI.Unshipped.txt (the package folder and its PublicAPI/net/ folder) of a packable project: its entries
join the PublicAPI.Shipped.txt next to it, a `*REMOVED*<entry>` line takes <entry> out of it, and the unshipped file keeps only its
`#nullable enable` header. Shipped entries are sorted by their UTF-8 bytes. Nothing is written if any file has a problem: an
unshipped file without a shipped one, or a removed entry the shipped list doesn't have. A second run changes nothing. A project
with <IsPackable>false</IsPackable> keeps its entries: the release doesn't ship it.
Exit codes: 0 done (or nothing to move), 1 a problem, nothing written.
"""

import re
import sys
from pathlib import Path

HEADER = "#nullable enable"
NOT_PACKABLE = re.compile(r"<IsPackable>\s*false\s*</IsPackable>", re.IGNORECASE)
REMOVED = "*REMOVED*"


def entries(text: str) -> list[str]:
    return [line for line in text.splitlines() if line.strip() and line.strip() != HEADER]


def render(lines: list[str], header: bool) -> str:
    return "".join(line + "\n" for line in ([HEADER] if header else []) + lines)


def project_folder(unshipped: Path) -> Path:
    """The folder of the project the file belongs to: its own, or two up for PublicAPI/net/."""
    folder = unshipped.parent
    return folder.parent.parent if folder.name == "net" and folder.parent.name == "PublicAPI" else folder


def packable(unshipped: Path) -> bool:
    projects = list(project_folder(unshipped).glob("*.csproj"))
    return not any(NOT_PACKABLE.search(project.read_text(encoding="utf-8-sig")) for project in projects)


def plan(unshipped: Path) -> tuple[dict[Path, str], list[str]]:
    """The new contents of the two files, or the problems that stop the move."""
    shipped = unshipped.with_name("PublicAPI.Shipped.txt")
    if not shipped.is_file():
        return {}, [f"{unshipped}: no PublicAPI.Shipped.txt next to it"]
    unshipped_text = unshipped.read_text(encoding="utf-8-sig")
    shipped_text = shipped.read_text(encoding="utf-8-sig")
    new = entries(unshipped_text)
    if not new:
        return {}, []

    current = set(entries(shipped_text))
    removed = {line[len(REMOVED):] for line in new if line.startswith(REMOVED)}
    missing = sorted(removed - current)
    if missing:
        return {}, [f"{unshipped}: *REMOVED* {entry} isn't in PublicAPI.Shipped.txt" for entry in missing]

    added = {line for line in new if not line.startswith(REMOVED)}
    result = sorted((current - removed) | added, key=lambda line: line.encode("utf-8"))
    header = HEADER in shipped_text.splitlines() or HEADER in unshipped_text.splitlines()
    return {shipped: render(result, header), unshipped: render([], HEADER in unshipped_text.splitlines())}, []


def main(argv: list[str]) -> int:
    root = Path(argv[0]) if argv else Path(".")
    files = sorted((root / "src").rglob("PublicAPI.Unshipped.txt"))
    writes: dict[Path, str] = {}
    problems: list[str] = []
    kept = [unshipped for unshipped in files if not packable(unshipped)]
    for unshipped in kept:
        print(f"kept (not packable): {unshipped.parent}")
    for unshipped in (file for file in files if file not in kept):
        changes, issues = plan(unshipped)
        writes.update(changes)
        problems += issues
    if problems:
        print("\n".join(problems))
        print(f"{len(problems)} problem(s); nothing written.")
        return 1
    for path, text in writes.items():
        path.write_bytes(text.encode("utf-8"))
    moved = [path for path in writes if path.name == "PublicAPI.Unshipped.txt"]
    for path in moved:
        print(f"shipped: {path.parent}")
    print(f"{len(files)} unshipped file(s), {len(moved)} moved to shipped.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
