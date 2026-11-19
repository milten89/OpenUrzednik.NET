#!/usr/bin/env bash
# Edge-case tests of the repository's scripts and of the workflow steps with logic (branches, parsing, loops).
#
# Usage (Linux, or WSL 2 on Windows; tools: bash 4.4+, git, jq, yq (Mike Farah's), python3, pwsh, the .NET SDK):
#   bash tests/Scripts/edge-tests.sh            # the repository this script is in
#   bash tests/Scripts/edge-tests.sh <repo>     # another checkout
# CI runs it in .github/workflows/scripts.yml. Exit codes: 0 every case passed (known failures don't count), 1 a case failed.
#
# Read-only on the repo: every case works in a temp directory. Each workflow step is taken with yq from the file as it is in
# the checkout (step <workflow> <job> <step name>) and run by run-step.sh the way an ubuntu runner runs it, so the tests
# follow the branch you have checked out.
#
# Covered: A format-cs.sh hook, B/C/G nuget-release.yml (version check, notes check, tag and release),
# D build.yml PR notes check, E release-label.yml, F build.yml test (net472) loop,
# H/I the /prepare-release scripts (check_packages.py with generated packages, check_nuget.py's SemVer order offline),
# J mutation.yml's shard selection (tests/Mutation/run.py --changed-since) on a copy of the repo's projects and configs,
# K .github/scripts/package_guard.py on generated repositories, L mutation.yml's summary and aggregate check.
# When a script or a workflow step with logic is added or changed, add its cases here: the normal case, then the edge cases
# (empty or missing input, unusual names and characters, re-runs, failures of the tools it calls). Single-command steps
# (`dotnet build …`) need none. Exempt: assets/branding/generate.py, a one-off script rerun only for a new provider icon.
set -u
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "${1:-$here/../..}" && pwd)"
# The temp repositories' commits and tags are never signed, whatever the user's git config says (CI's never are).
export GIT_CONFIG_COUNT=2 GIT_CONFIG_KEY_0=commit.gpgsign GIT_CONFIG_VALUE_0=false GIT_CONFIG_KEY_1=tag.gpgsign GIT_CONFIG_VALUE_1=false
# The cases run and import the repository's Python scripts: no __pycache__ in the checkout (python3 -I ignores this, so those
# calls pass -B).
export PYTHONDONTWRITEBYTECODE=1
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
pass=0 fail=0 known=0
results=()

# Known, accepted failures: the case still runs, and its failure doesn't fail the suite. A known case that passes is reported,
# so its entry can go.
#   A4s: the hook's sed fallback (no jq) doesn't decode JSON \u escapes; Claude Code sends raw UTF-8, so it never sees them (#168).
#   D6: a // comment in version.json, which Nerdbank.GitVersioning allows but jq can't parse.
KNOWN_FAILURES=" A4s D6 "

# run-step on PATH, for the cases that run a step inside `bash -c`.
mkdir -p "$work/bin"
printf '#!/usr/bin/env bash\nexec bash %q "$@"\n' "$here/run-step.sh" > "$work/bin/run-step"; chmod +x "$work/bin/run-step"
PATH="$work/bin:$PATH"

# expect <name> <expected exit: 0, nonzero or an exact code> <grep pattern that must appear in the output, or ""> command...
expect() {
  local name="$1" want="$2" pattern="$3"; shift 3
  local out code
  out="$("$@" 2>&1)"; code=$?
  local ok=1
  if [ "$want" = 0 ] && [ "$code" -ne 0 ]; then ok=0; fi
  if [ "$want" = nonzero ] && [ "$code" -eq 0 ]; then ok=0; fi
  if [ "$want" != nonzero ] && [ "$want" != 0 ] && [ "$code" -ne "$want" ]; then ok=0; fi
  if [ -n "$pattern" ] && ! grep -qE -- "$pattern" <<< "$out"; then ok=0; fi
  local id="${name%% *}"
  if [ "$ok" = 1 ]; then
    pass=$((pass+1))
    case "$KNOWN_FAILURES" in *" $id "*) results+=("PASS  $name (a known failure that passes now: remove $id from KNOWN_FAILURES)") ;;
      *) results+=("PASS  $name") ;; esac
  else
    case "$KNOWN_FAILURES" in
      *" $id "*) known=$((known+1)); results+=("KNOWN $name (exit $code)") ;;
      *) fail=$((fail+1)); results+=("FAIL  $name (exit $code)"); results+=("$(sed 's/^/        | /' <<< "$out" | tail -8)") ;;
    esac
  fi
}

step() { # step <workflow> <job> <step name> -> file with the step's run block
  local f="$work/step-$(echo "$3" | tr -c 'a-zA-Z0-9' '-').sh"
  yq ".jobs.$2.steps[] | select(.name == \"$3\") | .run" "$repo/.github/workflows/$1" > "$f"
  # A renamed step would leave an empty script (or "null") that passes every case expecting exit 0.
  if [ ! -s "$f" ] || [ "$(cat "$f")" = null ]; then
    printf 'echo "edge-tests: no step %s in job %s of %s" >&2\nexit 99\n' "$3" "$2" "$1" > "$f"
  fi
  echo "$f"
}

# ---------- A: format-cs.sh hook ----------
# Run twice: with jq (A1…), and with jq hidden from PATH (A1s…), which takes the hook's sed fallback
# (Git Bash on Windows has no jq).
hook="$repo/.claude/hooks/format-cs.sh"
nojq="$work/bin-nojq"; mkdir -p "$nojq"
IFS=: read -ra pathdirs <<< "$PATH"
for d in "${pathdirs[@]}"; do for f in "$d"/*; do
  b="${f##*/}"; [ "$b" = jq ] && continue; [ -x "$f" ] && [ ! -e "$nojq/$b" ] && ln -s "$f" "$nojq/$b"
done; done
for mode in jq sed; do
if [ "$mode" = jq ]; then hookpath="$PATH"; s=""; else hookpath="$nojq"; s="s"; fi
proj="$work/proj-$mode"; mkdir -p "$proj/sub dir" "$proj/Ąę"
printf 'class A{\n    void M( ){   }\n  }\n' > "$proj/A.cs"
cp "$proj/A.cs" "$proj/sub dir/B.cs"; cp "$proj/A.cs" "$proj/Ąę/C.cs"; cp "$proj/A.cs" "$proj/Upper.CS"
printf '{"x":1}\n' > "$proj/data.json"
hookrun() { PATH="$hookpath" CLAUDE_PROJECT_DIR="$proj" bash "$hook" <<< "$1"; }
# hookstub: the hook with a dotnet that only logs its arguments to $work/dotnet-calls
stub="$work/stub-dotnet"; mkdir -p "$stub"
printf '#!/bin/sh\necho "$*" >> "%s/dotnet-calls"\n' "$work" > "$stub/dotnet"; chmod +x "$stub/dotnet"
hookstub() { rm -f "$work/dotnet-calls"; PATH="$stub:$hookpath" CLAUDE_PROJECT_DIR="$proj" bash "$hook" <<< "$1"; }
nocall() { [ ! -s "$work/dotnet-calls" ]; }
changed() { ! cmp -s "$1" "$work/orig.cs"; }
cp "$proj/A.cs" "$work/orig.cs"
expect "A0$s hook: mode $mode (jq $( [ "$mode" = jq ] && echo present || echo hidden ))" 0 "" bash -c "PATH='$hookpath'; if [ '$mode' = jq ]; then command -v jq; else ! command -v jq; fi"

expect "A1$s hook: plain .cs path is formatted" 0 "" bash -c "$(declare -f hookrun changed); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath';
  hookrun '{\"tool_name\":\"Edit\",\"tool_input\":{\"file_path\":\"$proj/A.cs\",\"old_string\":\"a\",\"new_string\":\"b\"}}'; changed '$proj/A.cs'"
expect "A2$s hook: path with a space is formatted" 0 "" bash -c "$(declare -f hookrun changed); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath';
  hookrun '{\"tool_input\":{\"file_path\":\"$proj/sub dir/B.cs\"}}'; changed '$proj/sub dir/B.cs'"
expect "A3$s hook: non-ASCII directory (raw UTF-8) is formatted" 0 "" bash -c "$(declare -f hookrun changed); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath';
  hookrun '{\"tool_input\":{\"file_path\":\"$proj/Ąę/C.cs\"}}'; changed '$proj/Ąę/C.cs'"
expect "A4$s hook: non-ASCII directory as JSON \\u escapes" 0 "" bash -c "$(declare -f hookrun changed); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath';
  cp '$work/orig.cs' '$proj/Ąę/C.cs'; hookrun '{\"tool_input\":{\"file_path\":\"$proj/\\u0104\\u0119/C.cs\"}}'; changed '$proj/Ąę/C.cs'"
expect "A5$s hook: .CS (upper case) isn't passed to dotnet format (it ignores them)" 0 "" bash -c "$(declare -f hookstub nocall); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath' stub='$stub';
  hookstub '{\"tool_input\":{\"file_path\":\"$proj/Upper.CS\"}}' && nocall"
expect "A6$s hook: a non-.cs file exits 0 without calling dotnet" 0 "" bash -c "$(declare -f hookstub nocall); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath' stub='$stub';
  hookstub '{\"tool_input\":{\"file_path\":\"$proj/data.json\"}}' && nocall"
expect "A7$s hook: a missing file exits 0 without calling dotnet" 0 "" bash -c "$(declare -f hookstub nocall); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath' stub='$stub';
  hookstub '{\"tool_input\":{\"file_path\":\"$proj/nope.cs\"}}' && nocall"
expect "A7b$s hook: the stub sees the call for a .cs file (so A5-A7 can fail)" 0 "" bash -c "$(declare -f hookstub); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath' stub='$stub';
  hookstub '{\"tool_input\":{\"file_path\":\"$proj/A.cs\"}}' && grep -q 'format whitespace .*--include .*A.cs' '$work/dotnet-calls'"
expect "A8$s hook: no file_path / invalid JSON exits 0" 0 "" bash -c "$(declare -f hookrun); proj='$proj' hook='$hook' hookpath='$hookpath';
  hookrun '{\"tool_input\":{}}' && hookrun 'not json'"
cp "$work/orig.cs" "$proj/A.cs"
expect "A9$s hook: new_string containing \\\"file_path\\\" doesn't redirect it" 0 "" bash -c "$(declare -f hookrun changed); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath';
  hookrun '{\"tool_input\":{\"file_path\":\"$proj/A.cs\",\"new_string\":\"x \\\"file_path\\\": \\\"$proj/data.json\\\"\"},\"tool_response\":{\"filePath\":\"$proj/A.cs\"}}'; changed '$proj/A.cs'"
cp "$work/orig.cs" "$proj/A.cs"
expect "A10$s hook: file_path also in tool_response picks the edited file" 0 "" bash -c "$(declare -f hookrun changed); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath';
  hookrun '{\"tool_input\":{\"file_path\":\"$proj/A.cs\"},\"tool_response\":{\"file_path\":\"$proj/data.json\"}}'; changed '$proj/A.cs'"
expect "A11$s hook: pretty-printed (multi-line) JSON input" 0 "" bash -c "$(declare -f hookrun changed); proj='$proj' work='$work' hook='$hook' hookpath='$hookpath';
  cp '$work/orig.cs' '$proj/A.cs'; hookrun \"\$(printf '{\n  \"tool_input\": {\n    \"file_path\": \"%s\"\n  }\n}' '$proj/A.cs')\"; changed '$proj/A.cs'"
done

# ---------- B: nuget-release "Check the release version" ----------
version_step="$(step nuget-release.yml deploy "Check the release version")"
mkrepo() { # mkrepo <dir> <version> <packages...>; a git repo with artifacts/
  local d="$1" v="$2"; shift 2
  mkdir -p "$d/artifacts"; git -C "$d" init -q; git -C "$d" -c user.name=t -c user.email=t@t commit -q --allow-empty -m one
  for p in "$@"; do : > "$d/artifacts/$p.$v.nupkg"; : > "$d/artifacts/$p.$v.snupkg"; done
}
pkgs=(OpenUrzednik.Core OpenUrzednik.Nbp OpenUrzednik.Nbp.DependencyInjection OpenUrzednik.Diagnostics OpenUrzednik.Extensions.Logging)
b() { local d="$work/b-$1"; rm -rf "$d"; mkrepo "$d" "$2" "${pkgs[@]}"; echo "$d"; }
d=$(b ok 1.0.0);      expect "B1 version: stable 1.0.0 passes, sets output" 0 "version=1.0.0" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b pre 1.1.0-preview.3); expect "B2 version: prerelease fails" nonzero "is a prerelease" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b mixed 1.0.0); mv "$d/artifacts/OpenUrzednik.Nbp.1.0.0.nupkg" "$d/artifacts/OpenUrzednik.Nbp.1.0.1.nupkg"
expect "B3 version: one package with another version fails" nonzero "doesn't have the release version" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b nocore 1.0.0); rm "$d/artifacts/OpenUrzednik.Core.1.0.0.nupkg"
expect "B4 version: no Core package fails (ls names the missing pattern)" nonzero "OpenUrzednik\.Core\.\[0-9\]\*\.nupkg" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b tagother 1.0.0); git -C "$d" tag v1.0.0; git -C "$d" -c user.name=t -c user.email=t@t commit -q --allow-empty -m two
expect "B5 version: tag on another commit fails" nonzero "already released" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b tagsame 1.0.0); git -C "$d" tag v1.0.0
expect "B6 version: tag on this commit (re-run) passes" 0 "version=1.0.0" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b twocore 1.0.0); : > "$d/artifacts/OpenUrzednik.Core.0.9.0.nupkg"
expect "B7 version: two Core packages fail" nonzero "doesn't have the release version" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b corelike 1.0.0); : > "$d/artifacts/OpenUrzednik.Core.Extras.1.0.0.nupkg"
expect "B8 version: a package named OpenUrzednik.Core.X doesn't confuse it" 0 "version=1.0.0" run-step --event push --base main --workspace "$d" "$version_step"
d=$(b meta 1.0.0+build.5)
expect "B9 version: build metadata (1.0.0+x) passes as stable" 0 "version=1.0.0" run-step --event push --base main --workspace "$d" "$version_step"

# ---------- C: notes check in nuget-release ----------
notes_step="$(step nuget-release.yml deploy "Check the release notes")"
c() { local d="$work/c-$1"; rm -rf "$d"; mkdir -p "$d/docs/release-notes"; echo "$d"; }
d=$(c ok); echo "# notes" > "$d/docs/release-notes/v1.0.0.md"
expect "C1 notes: present passes" 0 "" run-step --event push --base main --workspace "$d" --env VERSION=1.0.0 "$notes_step"
d=$(c missing); expect "C2 notes: missing fails" nonzero "missing or empty" run-step --event push --base main --workspace "$d" --env VERSION=1.0.0 "$notes_step"
d=$(c empty); : > "$d/docs/release-notes/v1.0.0.md"
expect "C3 notes: empty fails" nonzero "missing or empty" run-step --event push --base main --workspace "$d" --env VERSION=1.0.0 "$notes_step"
d=$(c ws); printf ' \n\n' > "$d/docs/release-notes/v1.0.0.md"
expect "C4 notes: whitespace-only file fails" nonzero "missing or empty" run-step --event push --base main --workspace "$d" --env VERSION=1.0.0 "$notes_step"
d=$(c case); echo x > "$d/docs/release-notes/V1.0.0.md"
expect "C5 notes: wrong case (V1.0.0.md) fails on Linux" nonzero "missing or empty" run-step --event push --base main --workspace "$d" --env VERSION=1.0.0 "$notes_step"

# ---------- D: build.yml notes check on PRs into main ----------
pr_step="$(step build.yml build "Check the release notes")"
dd() { local d="$work/d-$1"; rm -rf "$d"; mkdir -p "$d/docs/release-notes"; printf '{"version": "%s"}\n' "$2" > "$d/version.json"; echo "$d"; }
d=$(dd ok 1.2.0); echo x > "$d/docs/release-notes/v1.2.0.md"
expect "D1 PR check: 1.2.0 with notes passes" 0 "" run-step --base main --workspace "$d" "$pr_step"
for v in "1.2.0-preview.{height}" "1.2" "v1.2.0" "1.2.0.4" " 1.2.0"; do
  d=$(dd "bad${v//[^a-z0-9]/}" "$v"); echo x > "$d/docs/release-notes/v$v.md" 2>/dev/null
  expect "D2 PR check: version '$v' rejected" nonzero "stable X.Y.Z" run-step --base main --workspace "$d" "$pr_step"
done
d=$(dd noversion x); echo '{}' > "$d/version.json"
expect "D3 PR check: no version property rejected" nonzero "has null" run-step --base main --workspace "$d" "$pr_step"
d=$(dd badjson x); echo '{ "version": ' > "$d/version.json"
expect "D4 PR check: invalid version.json fails" nonzero "" run-step --base main --workspace "$d" "$pr_step"
d=$(dd nonotes 1.2.0); expect "D5 PR check: missing notes fails" nonzero "missing or empty" run-step --base main --workspace "$d" "$pr_step"
d=$(dd comments 1.2.0); printf '{\n  // comment\n  "version": "1.2.0"\n}\n' > "$d/version.json"; echo x > "$d/docs/release-notes/v1.2.0.md"
expect "D6 PR check: version.json with a // comment (NBGV allows it)" 0 "" run-step --base main --workspace "$d" "$pr_step"

# ---------- E: release-label ----------
label_step="$(step release-label.yml release-label "Require a release label")"
e() { local d="$work/e-$1"; rm -rf "$d"; mkdir -p "$d/.github"; cp "$repo/.github/release.yml" "$d/.github/"; echo "$d"; }
d=$(e base)
expect "E1 labels: none fails" nonzero "Add at least one release label" run-step --workspace "$d" --env 'PR_LABELS=[]' "$label_step"
expect "E2 labels: spaces/quotes/unicode in other labels" 0 "Release label\(s\): bug" run-step --workspace "$d" --env 'PR_LABELS=["good first issue","say \"hi\"","zażółć","bug"]' "$label_step"
expect "E3 labels: a label literally named * doesn't count" nonzero "Add at least one" run-step --workspace "$d" --env 'PR_LABELS=["*"]' "$label_step"
expect "E4 labels: case differs (CI) doesn't count" nonzero "Add at least one" run-step --workspace "$d" --env 'PR_LABELS=["CI"]' "$label_step"
d=$(e noexclude); yq -i 'del(.changelog.exclude)' "$d/.github/release.yml"
expect "E5 labels: release.yml without exclude still works" 0 "Release label\(s\): ci" run-step --workspace "$d" --env 'PR_LABELS=["ci"]' "$label_step"
d=$(e anchor); printf 'x: &l [ "ci" ]\nchangelog:\n  categories:\n    - title: CI\n      labels: *l\n' > "$d/.github/release.yml"
expect "E6 labels: YAML anchor/alias in release.yml" 0 "Release label\(s\): ci" run-step --workspace "$d" --env 'PR_LABELS=["ci"]' "$label_step"

# ---------- F: test (net472) build loop (pwsh, Windows job), with a stub dotnet ----------
f="$work/f"; mkdir -p "$f/bin" "$f/tests/A.Tests" "$f/tests/OpenUrzednik.TestCommon" "$f/tests/B Tests" "$f/tests/OpenUrzednik.AotSmoke"
: > "$f/tests/A.Tests/A.Tests.csproj"; : > "$f/tests/OpenUrzednik.TestCommon/OpenUrzednik.TestCommon.csproj"; : > "$f/tests/B Tests/B Tests.csproj"
: > "$f/tests/OpenUrzednik.AotSmoke/OpenUrzednik.AotSmoke.csproj"
printf '#!/bin/sh\necho "dotnet $*" >> "%s/calls"\ncase "$*" in *"$FAIL_ON"*) [ -n "$FAIL_ON" ] && exit 3;; esac\nexit 0\n' "$f" > "$f/bin/dotnet"; chmod +x "$f/bin/dotnet"
netfx="$work/netfx.ps1"; yq '.jobs.test-netfx.steps[] | select(.name == "Build (net472)") | .run' "$repo/.github/workflows/build.yml" > "$netfx"
expect "F1 net472 loop: builds every test project except TestCommon and AotSmoke, paths with spaces" 0 "" bash -c "cd '$f' && rm -f calls && PATH='$f/bin':\$PATH pwsh -NoProfile -File '$netfx' && [ \$(grep -c . calls) = 2 ] && ! grep -q TestCommon calls && ! grep -q AotSmoke calls && grep -q 'B Tests.csproj' calls"
expect "F2 net472 loop: stops at the failing build, with its exit code" 3 "" bash -c "cd '$f' && rm -f calls; PATH='$f/bin':\$PATH FAIL_ON=A.Tests pwsh -NoProfile -File '$netfx'; code=\$?; ! grep -q 'B Tests' calls || exit 1; exit \$code"

# ---------- G: nuget-release "Tag and create the GitHub Release", with a bare origin and a stub gh ----------
tag_step="$(step nuget-release.yml deploy "Tag and create the GitHub Release")"
g() { # g <name> -> a clone of a bare origin, with notes and artifacts, and a stub gh logging to gh.log
  local d="$work/g-$1"; rm -rf "$d" "$d.git"; git init -q --bare "$d.git"; git clone -q "$d.git" "$d" 2>/dev/null
  mkdir -p "$d/docs/release-notes" "$d/artifacts" "$d/bin"; echo "# notes" > "$d/docs/release-notes/v1.0.0.md"; : > "$d/artifacts/X.1.0.0.nupkg"
  git -C "$d" add -A; git -C "$d" -c user.name=t -c user.email=t@t commit -q -m one; git -C "$d" push -q origin HEAD 2>/dev/null
  printf '#!/bin/sh\necho "gh $*" >> "%s/gh.log"\nif [ "$1 $2" = "release view" ]; then [ -f "%s/release-exists" ]; exit $?; fi\nexit 0\n' "$d" "$d" > "$d/bin/gh"; chmod +x "$d/bin/gh"
  echo "$d"
}
d=$(g fresh)
expect "G1 tag: first run tags, pushes and creates the release with the notes file" 0 "" bash -c "cd '$d' && PATH='$d/bin':\$PATH run-step --event push --base main --env VERSION=1.0.0 '$tag_step' && git -C '$d.git' rev-parse -q --verify refs/tags/v1.0.0 && grep -q 'release create v1.0.0 ./artifacts/X.1.0.0.nupkg --verify-tag --title v1.0.0 --notes-file docs/release-notes/v1.0.0.md' gh.log"
d=$(g rerun); git -C "$d" tag v1.0.0; git -C "$d" push -q origin v1.0.0; touch "$d/release-exists"
expect "G2 tag: re-run with tag and release present skips both" 0 "already exists, skipping.*|Tag v1.0.0 already exists" bash -c "cd '$d' && PATH='$d/bin':\$PATH run-step --event push --base main --env VERSION=1.0.0 '$tag_step' && ! grep -q 'release create' gh.log"
d=$(g half); git -C "$d" tag v1.0.0; git -C "$d" push -q origin v1.0.0
expect "G3 tag: re-run after a failed release creation creates only the release" 0 "Tag v1.0.0 already exists" bash -c "cd '$d' && PATH='$d/bin':\$PATH run-step --event push --base main --env VERSION=1.0.0 '$tag_step' && grep -q 'release create' gh.log"
d=$(g latest-main)
expect "G4 tag: a release from main is marked Latest" 0 "" bash -c "cd '$d' && PATH='$d/bin':\$PATH run-step --event push --base main --env VERSION=1.0.0 --env GITHUB_REF_NAME=main '$tag_step' && grep -q 'release create .* --latest=true' gh.log"
d=$(g latest-support)
expect "G5 tag: a release from a support branch (release/1.x) isn't marked Latest" 0 "" bash -c "cd '$d' && PATH='$d/bin':\$PATH run-step --event push --base release/1.x --env VERSION=1.0.0 --env GITHUB_REF_NAME=release/1.x '$tag_step' && grep -q 'release create .* --latest=false' gh.log"

# ---------- H: .claude/skills/prepare-release/scripts/check_packages.py, with generated .nupkg files ----------
cp_py="$repo/.claude/skills/prepare-release/scripts/check_packages.py"
mkpkg() { # mkpkg <file> [readme text, "-" = no nuspec] [readme path in nuspec] [icon path] [description]
  python3 - "$@" <<'EOF'
import sys, zipfile
f = sys.argv[1]; text = sys.argv[2] if len(sys.argv) > 2 else "# Pkg\nSee https://github.com/milten89/OpenUrzednik.NET/blob/main/README.md\n"
readme = sys.argv[3] if len(sys.argv) > 3 else "README.md"; icon = sys.argv[4] if len(sys.argv) > 4 else "icon.png"
desc = sys.argv[5] if len(sys.argv) > 5 else "Unofficial client."
with zipfile.ZipFile(f, "w") as z:
    if text != "-":
        z.writestr("P.nuspec", f'<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>P</id><version>1.0.0</version><description>{desc}</description><releaseNotes>https://github.com/milten89/OpenUrzednik.NET/releases/tag/v1.0.0</releaseNotes><icon>{icon}</icon><readme>{readme}</readme></metadata></package>')
        z.writestr("README.md", text)
    z.writestr("icon.png", b"x")
    z.writestr("sub/README.md", "# nested")
EOF
}
h() { local d="$work/h $1"; rm -rf "$d"; mkdir -p "$d"; echo "$d"; }   # a space in every folder name
d=$(h good); mkpkg "$d/P.1.0.0.nupkg"
expect "H1 packages: a correct package passes (folder with a space)" 0 "1 package\(s\), 0 problem" python3 "$cp_py" "$d" 1.0.0
d=$(h empty)
expect "H2 packages: an empty folder fails" nonzero "no .nupkg" python3 "$cp_py" "$d" 1.0.0
d=$(h nonuspec); mkpkg "$d/P.1.0.0.nupkg" -
expect "H3 packages: a package without a nuspec is a problem, not a traceback" nonzero "no .nuspec" python3 "$cp_py" "$d" 1.0.0
d=$(h backslash); mkpkg "$d/P.1.0.0.nupkg" "# ok" 'sub\README.md'
expect "H4 packages: a README path with Windows separators is found" 0 "0 problem" python3 "$cp_py" "$d" 1.0.0
d=$(h wording); mkpkg "$d/P.1.0.0.nupkg" $'# P\n\xf0\x9f\xa7\xaa Preview: may still change\n[x](https://github.com/milten89/OpenUrzednik.NET/blob/develop/a.md)\n'
expect "H5 packages: preview wording is reported" nonzero "README.md:2: preview wording" python3 "$cp_py" "$d" 1.0.0
expect "H5b packages: a blob/develop link is reported with its line" nonzero "README.md:3: link not on main" python3 "$cp_py" "$d" 1.0.0
d=$(h symbols); mkpkg "$d/P.1.0.0.nupkg"; echo junk > "$d/P.1.0.0.symbols.nupkg"
expect "H6 packages: .symbols.nupkg files are ignored" 0 "1 package\(s\), 0 problem" python3 "$cp_py" "$d" 1.0.0
d=$(h version); mkpkg "$d/P.1.0.0.nupkg" "# ok" README.md icon.png "OpenUrzednik client."
expect "H7 packages: a wrong version is reported" nonzero "version is '1.0.0', expected '1.0.1'" python3 "$cp_py" "$d" 1.0.1
expect "H7b packages: a Description without Unofficial is reported" nonzero "doesn't start with 'Unofficial'" python3 "$cp_py" "$d" 1.0.0
expect "H8 packages: usage error without arguments" nonzero "Usage" python3 "$cp_py"

# ---------- I: .claude/skills/prepare-release/scripts/check_nuget.py, SemVer order (offline) ----------
cn_dir="$repo/.claude/skills/prepare-release/scripts"
sv() { python3 -I -B -c "import sys; sys.path.insert(0, sys.argv[1]); from check_nuget import semver_key as k; sys.exit(0 if k(sys.argv[2]) < k(sys.argv[3]) else 1)" "$cn_dir" "$1" "$2"; }
expect "I1 nuget: a prerelease sorts below its release" 0 "" sv 1.1.0-preview.37 1.1.0
expect "I2 nuget: numeric identifiers compare as numbers (preview.9 < preview.10)" 0 "" sv 1.1.0-preview.9 1.1.0-preview.10
expect "I3 nuget: an old 1.0.N preview sorts above 1.0.0 (why they must be unlisted)" 0 "" sv 1.0.0 1.0.36-preview.g824c7f3c14
se() { python3 -I -B -c "import sys; sys.path.insert(0, sys.argv[1]); from check_nuget import semver_key as k; sys.exit(0 if k(sys.argv[2]) == k(sys.argv[3]) else 1)" "$cn_dir" "$1" "$2"; }
expect "I4 nuget: build metadata is ignored (1.0.0+abc equals 1.0.0)" 0 "" se 1.0.0+abc 1.0.0
expect "I5 nuget: numeric identifiers sort below alphanumeric ones" 0 "" sv 1.0.0-1 1.0.0-alpha
expect "I6 nuget: usage error for an unknown mode" nonzero "Usage" python3 "$cn_dir/check_nuget.py" during 1.0.0

# ---------- J: mutation.yml "Select the shards" (tests/Mutation/run.py --changed-since) ----------
sel=$(step mutation.yml shard "Select the shards")
mj="$work/mut"; mkdir -p "$mj"
# The projects, props and Stryker configs run.py reads, as in the checkout; committed as the PR's base.
# find, not git ls-files: a worktree's .git file may point to a path git can't open here (a Windows checkout under WSL).
( cd "$repo" && find tests/Mutation src tests .github/workflows/mutation.yml .config/dotnet-tools.json \( -name bin -o -name obj \) -prune -o -type f \( -path 'tests/Mutation/*' -o -name '*.csproj' -o -name '*.props' -o -path 'src/Privacy/*.cs' -o -name mutation.yml -o -name dotnet-tools.json \) -print ) |
  while IFS= read -r f; do mkdir -p "$mj/$(dirname "$f")"; cp "$repo/$f" "$mj/$f"; done
jgit() { git -C "$mj" -c user.name=t -c user.email=t@t "$@"; }
jgit init -q -b develop; jgit add -A; jgit commit -qm base
jbase=$(jgit rev-parse HEAD)
# jsel <event> <base ref> <base sha> <shards>: runs the step on the current branch and prints its GITHUB_OUTPUT
jsel() { (cd "$mj" && out="$(mktemp)" && GITHUB_EVENT_NAME="$1" GITHUB_BASE_REF="$2" BASE_SHA="$3" SHARDS="$4" GITHUB_OUTPUT="$out" bash -e "$sel" && cat "$out"); }
# jchange <branch> <command>: a branch from the base with one commit made by the command
jchange() { jgit checkout -q -B "$1" "$jbase" && (cd "$mj" && eval "$2") && jgit add -A && jgit commit -qm "$1"; }
jchange docs 'mkdir -p docs && echo x > docs/a.md'
expect "J1 mutation: a docs-only PR selects no shard" 0 "^shards=$" jsel pull_request develop "$jbase" "http http-netstandard"
jchange http 'mkdir -p src/Http/Infrastructure && echo x > src/Http/Infrastructure/New.cs'
expect "J2 mutation: src/Http/Infrastructure selects http and http-netstandard" 0 "^shards=http http-netstandard$" jsel pull_request develop "$jbase" "http http-netstandard"
expect "J2b mutation: ... and no shard of the identifiers runner" 0 "^shards=$" jsel pull_request develop "$jbase" "identifiers identifiers-netstandard dates"
jchange tooling 'echo "# x" >> tests/Mutation/run.py'
expect "J3 mutation: a change to run.py selects every shard of the runner" 0 "^shards=di logging diagnostics$" jsel pull_request develop "$jbase" "di logging diagnostics"
jchange docs2 'mkdir -p docs && echo y > docs/a.md'
expect "J4 mutation: a PR into main runs every shard" 0 "^shards=nbp$" jsel pull_request main "$jbase" "nbp"
expect "J5 mutation: the weekly run runs every shard" 0 "^shards=core transport-http$" jsel schedule "" "" "core transport-http"
expect "J6 mutation: an unknown base fails the step" nonzero "failed" jsel pull_request develop 0000000000000000000000000000000000000000 "nbp"
expect "J6b mutation: an empty base fails the step" nonzero "needs a commit" jsel pull_request develop "" "nbp"
jchange odd 'mkdir -p "src/Privacy/Ąę dir" && echo x > "src/Privacy/Ąę dir/Zażółć.cs"'
expect "J7 mutation: a non-ASCII path with a space is matched" 0 "^shards=identifiers identifiers-netstandard$" jsel pull_request develop "$jbase" "identifiers identifiers-netstandard dates"
jchange moved 'f=$(ls src/Privacy/*.cs | head -1); mkdir -p docs; git mv "$f" docs/moved.cs'
expect "J8 mutation: a file moved out of a strict folder selects its shards" 0 "^shards=identifiers identifiers-netstandard$" jsel pull_request develop "$jbase" "identifiers identifiers-netstandard dates"
jchange config 'echo " " >> tests/Mutation/stryker-logging.json'
expect "J9 mutation: a Stryker config selects only its shard" 0 "^shards=logging$" jsel pull_request develop "$jbase" "di logging diagnostics"
jchange linked 'mkdir -p tests/OpenUrzednik.Core.Tests/Privacy && echo x > tests/OpenUrzednik.Core.Tests/Privacy/NewTest.cs'
expect "J10 mutation: a test another test project links selects that shard too" 0 "^shards=identifiers identifiers-netstandard dates$" jsel pull_request develop "$jbase" "identifiers identifiers-netstandard dates"

jchange packages 'echo "<!-- x -->" >> Directory.Packages.props'
expect "J11 mutation: a package-version change runs every shard of the runner" 0 "^shards=http http-netstandard$" jsel pull_request develop "$jbase" "http http-netstandard"

# ---------- K: .github/scripts/package_guard.py, on generated repositories ----------
pg="$repo/.github/scripts/package_guard.py"
kgit() { git -C "$1" -c user.name=t -c user.email=t@t "${@:2}"; }
# kversion <dir> <version>: sets version.json's version (python, not sed -i, which differs between GNU and BSD)
kversion() { python3 -I -c 'import json, sys; p = sys.argv[1] + "/version.json"; d = json.load(open(p)); d["version"] = sys.argv[2]; json.dump(d, open(p, "w"), indent=2)' "$1" "$2"; }
# kbaseline <dir> <version>: sets src/Directory.Build.props' PackageValidationBaselineVersion
kbaseline() { python3 -I -c 'import re, sys; p = sys.argv[1] + "/src/Directory.Build.props"; s = open(p).read(); open(p, "w").write(re.sub(r"<PackageValidationBaselineVersion>[^<]*<", "<PackageValidationBaselineVersion>" + sys.argv[2] + "<", s))' "$1" "$2"; }
# kg <name> -> a repository with the real root and src props and key, and a package src/P that v1.0.0 shipped. The version and
# the baseline are set here, not taken from the checkout, so the cases hold on every branch (a release PR, a back-merge, a
# hotfix): the first commit is v1.0.0 (tagged), the second develop after it (1.1.0-preview, baseline 1.0.0).
kg() {
  local d="$work/k-$1"; rm -rf "$d"; mkdir -p "$d/src/P"
  cp "$repo/Directory.Build.props" "$repo/OpenUrzednik.snk" "$d/"; cp "$repo/src/Directory.Build.props" "$d/src/"
  printf '{\n  "version": "1.0.0",\n  "assemblyVersion": { "precision": "major" }\n}\n' > "$d/version.json"; kbaseline "$d" 1.0.0
  printf '<Project Sdk="Microsoft.NET.Sdk">\n</Project>\n' > "$d/src/P/P.csproj"
  kgit "$d" init -q; kgit "$d" add -A; kgit "$d" commit -qm release; kgit "$d" tag v1.0.0
  kversion "$d" '1.1.0-preview.{height}'; kgit "$d" add -A; kgit "$d" commit -qm develop
  echo "$d"
}
# kretag <dir>: the current state becomes v1.0.0 (version 1.0.0), then develop goes on to 1.1.0-preview, as kg does
kretag() { kversion "$1" 1.0.0; kcommit "$1"; kgit "$1" tag -f v1.0.0 >/dev/null; kversion "$1" '1.1.0-preview.{height}'; kcommit "$1"; }
# kcommit <dir>: commits whatever a case changed, since the guard reads tracked files only
kcommit() { kgit "$1" add -A; kgit "$1" commit -qm change --allow-empty; }
guard() { (cd "$1" && python3 -I "$pg"); }
# ksup <dir> <diagnostic> <target> [IsBaselineSuppression]: a CompatibilitySuppressions.xml for src/P
ksup() {
  printf '<?xml version="1.0" encoding="utf-8"?>\n<Suppressions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">\n  <Suppression>\n    <DiagnosticId>%s</DiagnosticId>\n    <Target>%s</Target>\n    <Left>lib/net10.0/P.dll</Left>\n    <Right>lib/net10.0/P.dll</Right>\n    <IsBaselineSuppression>%s</IsBaselineSuppression>\n  </Suppression>\n</Suppressions>\n' \
    "$2" "$3" "${4:-true}" > "$1/src/P/CompatibilitySuppressions.xml"
}
# kprop <file> <xml>: adds a PropertyGroup with the given content to an MSBuild file
kprop() { python3 -I -c 'import sys; p = sys.argv[1]; s = open(p).read(); i = s.rindex("</Project>"); open(p, "w").write(s[:i] + "  <PropertyGroup>\n    " + sys.argv[2] + "\n  </PropertyGroup>\n" + s[i:])' "$1" "$2"; }

d=$(kg ok)
expect "K1 guard: the repository's own props and key pass" 0 "Package guard: OK .*latest release v1.0.0" guard "$d"
d=$(kg cp1); ksup "$d" CP0001 T:OpenUrzednik.P.Removed; kcommit "$d"
expect "K2 guard: a suppression other than CP0006 fails" 1 "only CP0006 for a new client-interface member" guard "$d"
d=$(kg cp6); ksup "$d" CP0006 "M:OpenUrzednik.P.IPClient.GetAsync(System.Threading.CancellationToken)"; kcommit "$d"
expect "K3 guard: CP0006 for a new client-interface member passes" 0 "Package guard: OK" guard "$d"
d=$(kg cp6nonclient); ksup "$d" CP0006 "M:OpenUrzednik.P.IPOptions.Get"; kcommit "$d"
expect "K3b guard: CP0006 on an interface that isn't a client fails" 1 "only CP0006" guard "$d"
d=$(kg cp6notbaseline); ksup "$d" CP0006 "M:OpenUrzednik.P.IPClient.GetAsync" false; kcommit "$d"
expect "K3c guard: CP0006 that isn't a baseline suppression fails" 1 "only CP0006" guard "$d"
d=$(kg hotfix); kversion "$d" 1.0.1; ksup "$d" CP0006 "M:OpenUrzednik.P.IPClient.GetAsync"; kcommit "$d"
expect "K4 guard: a hotfix (patch) adding CP0006 fails" 1 "a patch version adds no public API" guard "$d"
d=$(kg hotfixkeep); ksup "$d" CP0006 "M:OpenUrzednik.P.IPClient.GetAsync"; kretag "$d"; kversion "$d" 1.0.1; kcommit "$d"
expect "K4b guard: a hotfix keeping a CP0006 its release tag has passes" 0 "Package guard: OK" guard "$d"
d=$(kg major); kversion "$d" 2.0.0-preview.{height}; ksup "$d" CP0002 M:OpenUrzednik.P.Old.Get; kcommit "$d"
expect "K5 guard: a baseline suppression while version.json carries an unreleased major passes" 0 "Package guard: OK" guard "$d"
d=$(kg badxml); printf '<Suppressions>' > "$d/src/P/CompatibilitySuppressions.xml"; kcommit "$d"
expect "K5b guard: an unreadable suppression file fails" 1 "not valid XML" guard "$d"
d=$(kg nowarn); kprop "$d/src/P/P.csproj" '<NoWarn>$(NoWarn);CS1591;CP0002</NoWarn>'; kcommit "$d"
expect "K6 guard: NoWarn with a CP code fails" 1 "NoWarn lists a package-validation code" guard "$d"
d=$(kg apicompat); kprop "$d/src/P/P.csproj" '<ApiCompatSuppressionFile>x.xml</ApiCompatSuppressionFile>'; kcommit "$d"
expect "K6b guard: an ApiCompat property fails" 1 "ApiCompatSuppressionFile changes how package validation" guard "$d"
d=$(kg disable); kprop "$d/src/P/P.csproj" '<EnablePackageValidation>false</EnablePackageValidation>'; kcommit "$d"
expect "K6c guard: EnablePackageValidation in a project fails" 1 "EnablePackageValidation is set only in" guard "$d"
d=$(kg srcoff); python3 -I -c 'import sys; p = sys.argv[1] + "/src/Directory.Build.props"; s = open(p).read(); open(p, "w").write(s.replace("<EnablePackageValidation>true<", "<EnablePackageValidation>false<"))' "$d"; kcommit "$d"
expect "K6f guard: package validation turned off in src/Directory.Build.props fails" 1 "EnablePackageValidation must be set once, to true \(false\)" guard "$d"
d=$(kg condition); python3 -I -c 'import sys; p = sys.argv[1] + "/src/Directory.Build.props"; s = open(p).read(); open(p, "w").write(s.replace("<EnablePackageValidation>", "<EnablePackageValidation Condition=\"\x27$(CI)\x27 == \x27true\x27\">"))' "$d"; kcommit "$d"
expect "K6g guard: a Condition on a guarded property fails" 1 "EnablePackageValidation has a Condition" guard "$d"
d=$(kg otherprops); mkdir -p "$d/tests"; printf '<Project>\n  <PropertyGroup>\n    <PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>\n  </PropertyGroup>\n</Project>\n' > "$d/tests/Directory.Build.props"; kcommit "$d"
expect "K6h guard: a baseline set in another props file fails" 1 "PackageValidationBaselineVersion is set only in" guard "$d"
d=$(kg uncommitted); kprop "$d/src/P/P.csproj" '<NoWarn>CP0002</NoWarn>'
expect "K6d guard: an uncommitted change to a tracked file is read (the working tree, as locally)" 1 "NoWarn lists a package-validation code" guard "$d"
d=$(kg untracked); mkdir -p "$d/src/Q"; printf '<Project>\n  <PropertyGroup>\n    <NoWarn>CP0002</NoWarn>\n  </PropertyGroup>\n</Project>\n' > "$d/src/Q/Q.csproj"
expect "K6e guard: an untracked file isn't read (only tracked files are)" 0 "Package guard: OK" guard "$d"
d=$(kg snk); printf 'x' >> "$d/OpenUrzednik.snk"; kcommit "$d"
expect "K7 guard: a changed key fails" 1 "OpenUrzednik.snk: missing or changed" guard "$d"
d=$(kg delaysign); kprop "$d/src/P/P.csproj" '<DelaySign>true</DelaySign>'; kcommit "$d"
expect "K7b guard: DelaySign in a project fails" 1 "DelaySign: only the root" guard "$d"
d=$(kg precision); python3 -I -c 'import json, sys; p = sys.argv[1] + "/version.json"; d = json.load(open(p)); d["assemblyVersion"]["precision"] = "minor"; json.dump(d, open(p, "w"))' "$d"; kcommit "$d"
expect "K7c guard: assemblyVersion.precision other than major fails" 1 "precision is 'minor'" guard "$d"
d=$(kg baseline); kbaseline "$d" 0.9.0; kcommit "$d"
expect "K8 guard: a baseline that isn't the latest release fails" 1 "PackageValidationBaselineVersion must be set once, to 1.0.0 \(0.9.0\)" guard "$d"
d=$(kg newrelease); kgit "$d" tag v1.1.0
expect "K8b guard: a newer release merged but the baseline not raised fails" 1 "must be set once, to 1.1.0" guard "$d"
expect "K8c guard: ... and the preview not raised above it fails" 1 "isn't above v1.1.0" guard "$d"
d=$(kg rerun); kversion "$d" 1.1.0; kcommit "$d"; kgit "$d" tag v1.1.0
expect "K9 guard: a re-run of the 1.1.0 release (its own tag on HEAD) passes" 0 "Package guard: OK .*latest release v1.0.0" guard "$d"
d=$(kg notags); kgit "$d" tag -d v1.0.0 >/dev/null
expect "K10 guard: no tags fetched but a baseline set exits 2" 2 "fetch the tags" guard "$d"
d=$(kg shipped); kprop "$d/src/P/P.csproj" '<PackageValidationBaselineVersion></PackageValidationBaselineVersion>'; kcommit "$d"
expect "K11 guard: an empty baseline override on a package v1.0.0 shipped fails" 1 "shipped it; remove the override" guard "$d"
d=$(kg newpkg); mkdir -p "$d/src/Q"; printf '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <PackageValidationBaselineVersion></PackageValidationBaselineVersion>\n  </PropertyGroup>\n</Project>\n' > "$d/src/Q/Q.csproj"; kcommit "$d"
expect "K11b guard: an empty baseline override on a package not released yet passes" 0 "Package guard: OK" guard "$d"
d=$(kg notpackable); kprop "$d/src/P/P.csproj" '<IsPackable>false</IsPackable>'; kretag "$d"
python3 -I -c 'import sys; p = sys.argv[1] + "/src/P/P.csproj"; s = open(p).read(); open(p, "w").write(s.replace("<IsPackable>false</IsPackable>", "<PackageValidationBaselineVersion></PackageValidationBaselineVersion>"))' "$d"; kcommit "$d"
expect "K11d guard: an empty baseline override on a package v1.0.0 had as not packable passes" 0 "Package guard: OK" guard "$d"
d=$(kg ownbaseline); kprop "$d/src/P/P.csproj" '<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>'; kcommit "$d"
expect "K11c guard: a project setting its own baseline version fails" 1 "doesn't set its own baseline version" guard "$d"
d="$work/k-nogit"; mkdir -p "$d"
expect "K12 guard: outside a git repository exits 2" 2 "can't check the repository" guard "$d"
d=$(kg nojson); printf '{' > "$d/version.json"; kcommit "$d"
expect "K12b guard: an unreadable version.json exits 2" 2 "can't check the repository" guard "$d"

# ---------- L: mutation.yml "Add the summary" and "Check every shard" ----------
sum_step="$(step mutation.yml shard "Add the summary")"
check_step="$(step mutation.yml mutation "Check every shard")"
l() { local d="$work/l-$1"; rm -rf "$d"; mkdir -p "$d"; echo "$d"; }
d=$(l report); mkdir -p "$d/artifacts/mutation"; echo "| http | pass |" > "$d/artifacts/mutation/summary.md"
expect "L1 summary: the shards' summary goes into the job summary" 0 "GITHUB_STEP_SUMMARY.*|\| http \| pass \|" run-step --workspace "$d" --env SHARDS=http --env SELECTED=http --env SELECTION=success "$sum_step"
d=$(l none)
expect "L2 summary: no affected shard says so" 0 "No shard of this job is affected by the PR's changes \(http http-netstandard\)" run-step --workspace "$d" --env "SHARDS=http http-netstandard" --env SELECTED= --env SELECTION=success "$sum_step"
expect "L3 summary: a failed selection doesn't claim no shard was affected" 0 "" bash -c "! run-step --workspace '$d' --env SHARDS=http --env SELECTED= --env SELECTION=failure '$sum_step' | grep -q 'No shard'"
expect "L4 check: every shard passed" 0 "" run-step --workspace "$d" --env RESULT=success "$check_step"
for r in failure cancelled skipped ""; do
  expect "L5 check: shard result '$r' fails" 1 "didn't pass" run-step --workspace "$d" --env "RESULT=$r" "$check_step"
done

printf '%s\n' "${results[@]}"
echo "== $pass passed, $fail failed, $known known failures"
[ "$fail" -eq 0 ]
