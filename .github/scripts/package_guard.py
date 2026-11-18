# /// script
# requires-python = ">=3.12"
# ///
"""Check the package-validation, signing and baseline rules that the build can't enforce itself (ADR-0011, ADR-0005).

Usage (from anywhere in the repository):
    uv run .github/scripts/package_guard.py    # locally
    python3 .github/scripts/package_guard.py   # CI (build.yml, the net10.0 job)

It reads only the working tree, the git tags and version.json, never pull-request data, so it gives the same answer on a PR,
in the release workflow and locally. It fails when:
  * a CompatibilitySuppressions.xml entry isn't allowed: only CP0006 on a client-interface member (`I...Client`) as a baseline
    suppression, or any baseline suppression while version.json carries a major above the latest release's; a patch version
    (a hotfix) may not add a CP0006 entry that its release tag doesn't have, because a patch adds no public API;
  * package validation is off or weakened: src/Directory.Build.props must set EnablePackageValidation to true and the baseline
    version; no file may list a CPxxxx code in NoWarn/WarningsNotAsErrors or set an ApiCompat* property, a PackageValidation*
    property other than PackageValidationBaselineVersion, or DisablePackageBaselineValidation;
  * the strong-name key or its settings change: OpenUrzednik.snk, and the root Directory.Build.props' SignAssembly,
    AssemblyOriginatorKeyFile and PublicKey (no condition, no other file setting them, no DelaySign or PublicSign), or
    assemblyVersion.precision other than `major`;
  * PackageValidationBaselineVersion isn't the latest stable vX.Y.Z tag merged into HEAD, a project sets an empty baseline
    although that tag shipped it, or version.json is a prerelease that wasn't raised above that tag.
    A tag of the stable version being built (a re-run of a release) is skipped.
Exit codes: 0 every rule holds, 1 a rule is broken, 2 the repository can't be read (or its tags weren't fetched).
"""

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

# The strong-name key pair is checked in on purpose and never changes (ADR-0005). Changing these values is a deliberate act.
SNK_SHA256 = "f75693b8f5562dfc81e19bd9ea8d87c91c460df819f5918b79320bccb255e1fb"
PUBLIC_KEY = (
    "002400000480000094000000060200000024000052534131000400000100010059140fccf92ca0de1fe0f857c7631caf9fac225b9b0525ff6dd9649013a5"
    "5396b09550fe8e0cb8a311c2d21e52541838e3ad45c559b80061fcc8a43f9a3fdb994ab86fa2e7b0b7864406bd7333e17b6e8a4d46f214903c6590587fe7"
    "c8b5df852b41ab3cbe99b11bcb3b36a2f5838c6eec705c976b9b5cb068d70700ea4598b8"
)
ROOT_PROPS = "Directory.Build.props"
SRC_PROPS = "src/Directory.Build.props"
# The root props sets exactly these, without a condition; no other file sets any signing property.
ROOT_SIGNING = {
    "SignAssembly": "true",
    "AssemblyOriginatorKeyFile": "$(MSBuildThisFileDirectory)OpenUrzednik.snk",
    "PublicKey": PUBLIC_KEY,
}
SIGNING_PROPERTIES = {*ROOT_SIGNING, "DelaySign", "PublicSign"}
CLIENT_MEMBER = re.compile(r"^[MPE]:OpenUrzednik(\.\w+)*\.I\w*Client\.")
STABLE_TAG = re.compile(r"^v(\d+)\.(\d+)\.(\d+)$")
CP_CODE = re.compile(r"\bCP\d{4}\b")

Version = tuple[int, int, int]


class RepoError(Exception):
    pass


class Repo:
    def __init__(self, root: Path) -> None:
        self.root = root

    def git(self, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
        return subprocess.run(["git", *args], cwd=self.root, capture_output=True, text=True, encoding="utf-8", check=check)

    def tracked(self) -> list[str]:
        return [path for path in self.git("ls-files", "-z").stdout.split("\0") if path]

    def read(self, path: str) -> str:
        try:
            return (self.root / path).read_text(encoding="utf-8-sig")
        except OSError as ex:
            raise RepoError(f"can't read {path}: {ex}") from ex

    def show(self, rev: str, path: str) -> str | None:
        result = self.git("show", f"{rev}:{path}", check=False)
        return result.stdout if result.returncode == 0 else None


def local(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def parse_xml(text: str) -> ET.Element | None:
    try:
        return ET.fromstring(text)
    except ET.ParseError:
        return None


def text_of(element: ET.Element) -> str:
    return (element.text or "").strip()


def tag_name(version: Version) -> str:
    return "v{}.{}.{}".format(*version)


def dotted(version: Version) -> str:
    return "{}.{}.{}".format(*version)


def read_version(repo: Repo) -> tuple[str, Version, bool, str]:
    """version.json's raw version, its X.Y.Z, whether it's a stable release version, and assemblyVersion.precision."""
    data = json.loads(repo.read("version.json"))
    raw = str(data["version"])
    match = re.match(r"^(\d+)\.(\d+)\.(\d+)", raw)
    if not match:
        raise RepoError(f"version.json has no X.Y.Z version: {raw}")
    version = (int(match[1]), int(match[2]), int(match[3]))
    return raw, version, raw == dotted(version), str(data.get("assemblyVersion", {}).get("precision", ""))


def latest_release(repo: Repo, building: Version, stable: bool) -> Version | None:
    """The latest stable vX.Y.Z tag merged into HEAD. A tag of the stable version being built is skipped: a re-run of a release
    that already tagged. A prerelease never skips its tag, so a branch that has vX.Y.Z but still builds X.Y.Z-preview fails."""
    tags = []
    for name in repo.git("tag", "--merged", "HEAD").stdout.split():
        match = STABLE_TAG.match(name)
        if match:
            version = (int(match[1]), int(match[2]), int(match[3]))
            if not (stable and version == building):
                tags.append(version)
    return max(tags, default=None)


def suppression_key(element: ET.Element) -> tuple[str, ...]:
    return tuple((element.findtext(name) or "").strip() for name in ("DiagnosticId", "Target", "Left", "Right"))


def check_suppressions(repo: Repo, files: list[str], building: Version, release: Version | None, errors: list[str]) -> None:
    new_major = release is None or building[0] > release[0]
    for path in files:
        if not path.startswith("src/") or not path.endswith("CompatibilitySuppressions.xml"):
            continue
        root = parse_xml(repo.read(path))
        if root is None:
            errors.append(f"{path}: not valid XML.")
            continue
        at_tag: set[tuple[str, ...]] = set()
        if building[2] > 0 and release is not None:
            old = parse_xml(repo.show(tag_name(release), path) or "<Suppressions />")
            at_tag = {suppression_key(s) for s in old.iter() if local(s.tag) == "Suppression"} if old is not None else set()
        for suppression in (s for s in root.iter() if local(s.tag) == "Suppression"):
            diagnostic, target, *_ = suppression_key(suppression)
            baseline = (suppression.findtext("IsBaselineSuppression") or "").strip().lower() == "true"
            where = f"{path}: {diagnostic} {target}"
            if diagnostic == "CP0006" and baseline and CLIENT_MEMBER.match(target):
                if building[2] > 0 and release is not None and suppression_key(suppression) not in at_tag:
                    errors.append(f"{where}: a patch version adds no public API, so no new CP0006 since "
                                  f"{tag_name(release)} (ADR-0011).")
                continue
            if baseline and new_major:
                continue
            errors.append(f"{where}: only CP0006 for a new client-interface member, or baseline suppressions while "
                          "version.json carries an unreleased major, may be suppressed (ADR-0011).")


def conditioned(element: ET.Element, parents: dict[ET.Element, ET.Element]) -> bool:
    node: ET.Element | None = element
    while node is not None:
        if "Condition" in node.attrib:
            return True
        node = parents.get(node)
    return False


def check_msbuild(repo: Repo, files: list[str], release: Version | None, errors: list[str]) -> None:
    expected = dotted(release) if release else ""
    seen: dict[str, list[str]] = {}
    for path in files:
        if not path.endswith((".props", ".targets", ".csproj")):
            continue
        root = parse_xml(repo.read(path))
        if root is None:
            errors.append(f"{path}: not valid XML.")
            continue
        parents = {child: parent for parent in root.iter() for child in parent}
        for element in root.iter():
            name = local(element.tag)
            text = text_of(element)
            if path in (ROOT_PROPS, SRC_PROPS) and name in (*ROOT_SIGNING, "EnablePackageValidation",
                                                            "PackageValidationBaselineVersion"):
                seen.setdefault(f"{path}:{name}", []).append(text)
                if conditioned(element, parents):
                    errors.append(f"{path}: {name} has a Condition; it's set for every project and target.")
            if name in ("NoWarn", "WarningsNotAsErrors") and CP_CODE.search(text):
                errors.append(f"{path}: {name} lists a package-validation code ({text}); never suppress it (ADR-0011).")
            elif name.startswith("ApiCompat"):
                errors.append(f"{path}: {name} changes how package validation reports or suppresses (ADR-0011).")
            elif name.startswith("PackageValidation") and name != "PackageValidationBaselineVersion":
                errors.append(f"{path}: {name} changes package validation (ADR-0011).")
            elif name == "DisablePackageBaselineValidation":
                errors.append(f"{path}: {name} turns off the comparison with the latest release (ADR-0011).")
            elif name == "EnablePackageValidation" and path != SRC_PROPS:
                errors.append(f"{path}: EnablePackageValidation is set only in {SRC_PROPS} (ADR-0011).")
            elif name in SIGNING_PROPERTIES and (path != ROOT_PROPS or name not in ROOT_SIGNING):
                errors.append(f"{path}: {name}: only the root {ROOT_PROPS} sets SignAssembly, AssemblyOriginatorKeyFile "
                              "and PublicKey, and nothing sets DelaySign or PublicSign; never change the key (ADR-0005).")
            elif name == "PackageValidationBaselineVersion" and path != SRC_PROPS:
                check_override(repo, path, text, release, errors)

    def require(path: str, name: str, value: str, why: str) -> None:
        values = seen.get(f"{path}:{name}", [])
        if values != [value]:
            found = ", ".join(v or "empty" for v in values) or "missing"
            errors.append(f"{path}: {name} must be set once, to {value or 'empty'} ({found}); {why}")

    require(SRC_PROPS, "EnablePackageValidation", "true", "package validation stays on (ADR-0011).")
    require(SRC_PROPS, "PackageValidationBaselineVersion", expected,
            f"it names the latest release merged into HEAD ({tag_name(release) if release else 'none'}); the back-merge "
            "after a release, or a hotfix or backport branch, raises it (ADR-0011).")
    for name, value in ROOT_SIGNING.items():
        require(ROOT_PROPS, name, value, "never change the strong-name key or turn signing off (ADR-0005).")


def check_override(repo: Repo, path: str, text: str, release: Version | None, errors: list[str]) -> None:
    if not path.startswith("src/") or not path.endswith(".csproj"):
        errors.append(f"{path}: PackageValidationBaselineVersion is set only in {SRC_PROPS} and, empty, in the csproj of "
                      "a package not released yet.")
        return
    if text:
        errors.append(f"{path}: a project doesn't set its own baseline version; {SRC_PROPS} sets it for every package.")
        return
    if release is None:
        return
    shipped = repo.show(tag_name(release), path)
    if shipped is not None and not re.search(r"<IsPackable>\s*false\s*</IsPackable>", shipped, re.IGNORECASE):
        errors.append(f"{path}: an empty PackageValidationBaselineVersion is only for a package not released yet, but "
                      f"{tag_name(release)} shipped it; remove the override (in the back-merge, or on a hotfix or backport "
                      "branch made from that tag).")


def check_version(raw: str, building: Version, stable: bool, precision: str, release: Version | None,
                  errors: list[str]) -> None:
    if not stable and release is not None and release >= building:
        errors.append(f"version.json: {raw} isn't above {tag_name(release)}, which is merged into HEAD; the back-merge raises "
                      "it to the next minor (ADR-0008).")
    if precision != "major":
        errors.append(f"version.json: assemblyVersion.precision is '{precision}'; it stays 'major' (ADR-0005).")


def check_key(repo: Repo, errors: list[str]) -> None:
    snk = repo.root / "OpenUrzednik.snk"
    if not snk.is_file() or hashlib.sha256(snk.read_bytes()).hexdigest() != SNK_SHA256:
        errors.append("OpenUrzednik.snk: missing or changed; never change, replace or remove the key (ADR-0005).")


def main() -> int:
    try:
        top = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, encoding="utf-8",
                             check=True).stdout.strip()
        repo = Repo(Path(top))
        raw, building, stable, precision = read_version(repo)
        files = repo.tracked()
        release = latest_release(repo, building, stable)
        if release is None and re.search(r"<PackageValidationBaselineVersion>\s*\S", repo.read(SRC_PROPS)):
            raise RepoError("no vX.Y.Z tag is merged into HEAD, but src/Directory.Build.props names a baseline; "
                            "fetch the tags (git fetch --tags, or actions/checkout with fetch-depth: 0).")

        errors: list[str] = []
        check_suppressions(repo, files, building, release, errors)
        check_msbuild(repo, files, release, errors)
        check_version(raw, building, stable, precision, release, errors)
        check_key(repo, errors)
    except (RepoError, OSError, subprocess.CalledProcessError, KeyError, json.JSONDecodeError) as ex:
        print(f"package_guard: can't check the repository: {ex}", file=sys.stderr)
        return 2

    summary = f"version.json {raw}, latest release {tag_name(release) if release else 'none'}"
    if errors:
        print(f"Package guard: {len(errors)} problem(s) ({summary}):")
        for error in errors:
            print(f"  - {error}")
        return 1
    print(f"Package guard: OK ({summary}).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
