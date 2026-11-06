"""Check the listed versions of the published packages on nuget.org.

Usage:
  python check_nuget.py before <version>   every listed version sorts below <version>
  python check_nuget.py after <version>    every package lists <version>
Prints one line per package and exits with 1 if a check fails.
"""

import gzip
import json
import sys
import urllib.error
import urllib.request

PACKAGES = [
    "OpenUrzednik.Core",
    "OpenUrzednik.Extensions.Logging",
    "OpenUrzednik.Diagnostics",
    "OpenUrzednik.Nbp",
    "OpenUrzednik.Nbp.DependencyInjection",
]
REGISTRATION = "https://api.nuget.org/v3/registration5-gz-semver2/{}/index.json"


def get_json(url: str):
    # The registration5-gz hive is always gzip-encoded, and urllib doesn't decompress it.
    with urllib.request.urlopen(url, timeout=30) as response:
        body = response.read()
    if response.headers.get("Content-Encoding") == "gzip" or body[:2] == b"\x1f\x8b":
        body = gzip.decompress(body)
    return json.loads(body)


def listed_versions(package: str) -> list[str] | None:
    try:
        index = get_json(REGISTRATION.format(package.lower()))
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise
    versions = []
    for page in index["items"]:
        items = page.get("items") or get_json(page["@id"])["items"]
        versions += [i["catalogEntry"]["version"] for i in items if i["catalogEntry"].get("listed", True)]
    return versions


def semver_key(version: str):
    core, _, suffix = version.split("+")[0].partition("-")
    numbers = tuple(int(n) for n in core.split("."))
    # A version with a suffix sorts below the same numbers without one.
    return numbers, suffix == "", [(0, int(p), "") if p.isdigit() else (1, 0, p) for p in suffix.split(".")] if suffix else []


def main() -> int:
    if len(sys.argv) != 3 or sys.argv[1] not in ("before", "after"):
        print(__doc__.strip())
        return 2
    mode, version = sys.argv[1], sys.argv[2]
    failed = False
    for package in PACKAGES:
        versions = listed_versions(package)
        if versions is None:
            print(f"{package}: not on nuget.org")
            failed |= mode == "after"
            continue
        if mode == "before":
            blocking = [v for v in versions if semver_key(v) >= semver_key(version)]
            failed |= bool(blocking)
            print(f"{package}: {len(versions)} listed" + (f", not below {version}: {', '.join(blocking)}" if blocking else ", all below"))
        else:
            ok = version in versions
            failed |= not ok
            print(f"{package}: {version} {'listed' if ok else 'NOT listed'}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
