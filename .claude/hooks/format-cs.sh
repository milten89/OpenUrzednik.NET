#!/usr/bin/env bash
# PostToolUse hook: fix whitespace formatting of an edited .cs file.
# Uses `dotnet format whitespace --folder`, which needs no MSBuild workspace, so it is fast.
# Full `dotnet format --verify-no-changes` (style + analyzers) still runs in CI and before finishing a task.

input="$(cat)"

# Extract tool_input.file_path without depending on jq.
file="$(printf '%s' "$input" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"
file="${file//\\\\/\\}"

case "$file" in
  *.cs) ;;
  *) exit 0 ;;
esac

[ -f "$file" ] || exit 0
command -v dotnet >/dev/null 2>&1 || exit 0

root="${CLAUDE_PROJECT_DIR:-$(pwd)}"
dotnet format whitespace "$root" --folder --include "$file" >/dev/null 2>&1 || true
exit 0
