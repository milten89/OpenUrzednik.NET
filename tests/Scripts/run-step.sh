#!/usr/bin/env bash
# Runs one GitHub Actions `run:` step locally, the way an ubuntu runner does (used by edge-tests.sh).
# Usage: run-step.sh [--shell default|bash|sh] [--event pull_request|push|workflow_call] [--ref REF]
#                [--base BRANCH] [--head BRANCH] [--workspace DIR] [--env K=V]... <step script>
#   --shell default  bash -e {0}                    (no `shell:` in the step; what our logs show)
#   --shell bash     bash --noprofile --norc -eo pipefail {0}
# Expressions (${{ … }}) aren't evaluated: pass their values with --env.
# Prints GITHUB_OUTPUT, GITHUB_ENV, GITHUB_PATH and the step summary afterwards; exits with the step's code.
set -uo pipefail

shell=default event=pull_request ref="" base="develop" head="feature/local" workspace="$PWD"
extra=()   # expanded as ${extra[@]+…}: bash before 4.4 (macOS's 3.2) calls an empty array unbound under set -u
while [ $# -gt 1 ]; do
  case "$1" in
    --shell) shell="$2"; shift 2 ;;
    --event) event="$2"; shift 2 ;;
    --ref) ref="$2"; shift 2 ;;
    --base) base="$2"; shift 2 ;;
    --head) head="$2"; shift 2 ;;
    --workspace) workspace="$2"; shift 2 ;;
    --env) extra+=("$2"); shift 2 ;;
    *) echo "run-step: unknown option $1" >&2; exit 2 ;;
  esac
done
[ $# -eq 1 ] || { echo "usage: run-step.sh [options] <step script>" >&2; exit 2; }
script="$(realpath "$1")"

runner_temp="$(mktemp -d)"
trap 'rm -rf "$runner_temp"' EXIT
for f in output env path step_summary; do : > "$runner_temp/$f"; done

if [ "$event" = pull_request ]; then
  ref="${ref:-refs/pull/1/merge}"; base_ref="$base"; head_ref="$head"
else
  ref="${ref:-refs/heads/$base}"; base_ref=""; head_ref=""
fi
ref_name="${ref#refs/heads/}"; ref_name="${ref_name#refs/tags/}"; ref_name="${ref_name#refs/pull/}"

# Run inside a CI job, the step must not see that job's own GITHUB_* and RUNNER_* values (its event file, ref, SHA).
while IFS= read -r name; do unset "$name"; done < <(compgen -e | grep -E '^(GITHUB|RUNNER)_' || true)

case "$shell" in
  default) cmd=(bash -e "$script") ;;
  bash) cmd=(bash --noprofile --norc -eo pipefail "$script") ;;
  sh) cmd=(sh -e "$script") ;;
  *) echo "run-step: unknown shell $shell" >&2; exit 2 ;;
esac

cd "$workspace"
env CI=true GITHUB_ACTIONS=true RUNNER_OS=Linux RUNNER_ARCH=X64 RUNNER_TEMP="$runner_temp" \
  GITHUB_WORKSPACE="$workspace" GITHUB_EVENT_NAME="$event" GITHUB_REF="$ref" GITHUB_REF_NAME="$ref_name" \
  GITHUB_BASE_REF="$base_ref" GITHUB_HEAD_REF="$head_ref" GITHUB_REPOSITORY=milten89/OpenUrzednik.NET \
  GITHUB_OUTPUT="$runner_temp/output" GITHUB_ENV="$runner_temp/env" GITHUB_PATH="$runner_temp/path" \
  GITHUB_STEP_SUMMARY="$runner_temp/step_summary" ${extra[@]+"${extra[@]}"} "${cmd[@]}"
code=$?

for f in output env path step_summary; do
  if [ -s "$runner_temp/$f" ]; then echo "--- GITHUB_$(echo "$f" | tr '[:lower:]' '[:upper:]')"; cat "$runner_temp/$f"; fi
done
echo "--- exit code $code"
exit "$code"
