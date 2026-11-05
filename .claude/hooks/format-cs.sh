#!/usr/bin/env bash
# PostToolUse hook: fix whitespace formatting of an edited .cs file.
# Uses `dotnet format whitespace --folder`, which needs no MSBuild workspace, so it is fast.
# Full `dotnet format --verify-no-changes` (style + analyzers) still runs in CI and before finishing a task.

input="$(cat)"

# Read tool_input.file_path; jq when available, otherwise the first "file_path" after "tool_input"
# (not the last one in the input, which could come from tool_response).
if command -v jq >/dev/null 2>&1; then
  file="$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty' 2>/dev/null | tr -d '\r')"
else
  case "$input" in
    *'"tool_input"'*) rest="${input#*\"tool_input\"}" ;;
    *) exit 0 ;;
  esac
  case "$rest" in
    *'"file_path"'*) rest="${rest#*\"file_path\"}" ;;
    *) exit 0 ;;
  esac
  file="$(printf '%s' "$rest" | sed -n '1s/^[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
  file="${file//\\\\/\\}"
fi

# Only *.cs: dotnet format ignores an upper-case .CS file even when it is passed with --include.
case "$file" in
  *.cs) ;;
  *) exit 0 ;;
esac

[ -f "$file" ] || exit 0
command -v dotnet >/dev/null 2>&1 || exit 0

root="${CLAUDE_PROJECT_DIR:-$(pwd)}"
dotnet format whitespace "$root" --folder --include "$file" >/dev/null 2>&1 || true
exit 0
