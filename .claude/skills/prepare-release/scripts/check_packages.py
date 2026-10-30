"""Check the packed .nupkg files of a release before its PR into main.

Usage: python check_packages.py <folder with .nupkg files> <version>
Prints one line per problem and exits with 1 if there is any.
"""

import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = "github.com/milten89/OpenUrzednik.NET"
# Wording that belongs to a preview, not to a stable package page.
PREVIEW_WORDING = re.compile(r"\bpreview\b|may still change|not (yet )?stable", re.IGNORECASE)
# Links into the repository must point at main, which holds the released code.
NON_MAIN_LINK = re.compile(re.escape(REPO) + r"/(?:blob|tree)/(?!main/)[^\s)\"'>]+")


def check(nupkg: Path, version: str) -> list[str]:
    problems = []
    with zipfile.ZipFile(nupkg) as package:
        names = package.namelist()
        nuspec_name = next((n for n in names if n.endswith(".nuspec") and "/" not in n), None)
        if nuspec_name is None:
            return [f"{nupkg.name}: no .nuspec"]
        root = ET.fromstring(package.read(nuspec_name))
        metadata = root.find("{*}metadata")

        def field(name: str) -> str:
            element = metadata.find("{*}" + name)
            return (element.text or "").strip() if element is not None else ""

        def packed_path(name: str) -> str:
            # The nuspec may use Windows separators; zip entries use "/".
            return field(name).replace("\\", "/")

        package_id = field("id")
        if field("version") != version:
            problems.append(f"version is {field('version')!r}, expected {version!r}")
        expected_notes = f"https://{REPO}/releases/tag/v{version}"
        if field("releaseNotes") != expected_notes:
            problems.append(f"releaseNotes is {field('releaseNotes')!r}, expected {expected_notes!r}")
        icon = packed_path("icon")
        if not icon:
            problems.append("no icon")
        elif icon not in names:
            problems.append(f"icon {icon!r} is not in the package")
        if not field("description").startswith("Unofficial"):
            problems.append("description doesn't start with 'Unofficial'")
        readme = packed_path("readme")
        if not readme or readme not in names:
            problems.append("no packed README")
        else:
            text = package.read(readme).decode("utf-8")
            for number, line in enumerate(text.splitlines(), 1):
                if PREVIEW_WORDING.search(line):
                    problems.append(f"{readme}:{number}: preview wording: {line.strip()[:120]}")
                for link in NON_MAIN_LINK.findall(line):
                    problems.append(f"{readme}:{number}: link not on main: {link}")
    return [f"{package_id or nupkg.name}: {p}" for p in problems]


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__.strip())
        return 2
    folder, version = Path(sys.argv[1]), sys.argv[2]
    packages = sorted(p for p in folder.glob("*.nupkg") if not p.name.endswith(".symbols.nupkg"))
    if not packages:
        print(f"no .nupkg files in {folder}")
        return 1
    problems = [problem for package in packages for problem in check(package, version)]
    for problem in problems:
        print(problem)
    print(f"{len(packages)} package(s), {len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
