# ADR-0001: Agent-first repo layout

**Status:** accepted · **Date:** 15/11/2026 · **Deciders:** Đình Duy, Minh Nhật (review)

## Context
Both members code mostly through AI agents (Duy: Claude Code, Nhật: Codex) on Windows, with an 11-day
deadline. Agents repeat mistakes when rules are scattered, duplicated or only written as prose.

## Decision
- One source of truth per fact: agent rules in `AGENTS.md` (Codex reads it natively; `CLAUDE.md` only
  imports it), design facts in `docs/gdd/`, plans in GitHub issues.
- Progressive disclosure: root `AGENTS.md` ≤ 150 lines; module detail in per-module `AGENTS.md` (≤ 40 lines);
  workflows as skills in `.agents/skills/` (Claude reads thin wrappers in `.claude/skills/`).
- Boundaries enforced by tools: asmdef references, `tools/check_repo.py` (pre-commit, CI, Claude Stop hook),
  Claude permissions and PreToolUse guard, CODEOWNERS.
- Every workflow ends in a command: `tools/verify.ps1`, `tools/build-windows.ps1`, `python tools/check_repo.py`.
- Traceability: SB id + issue number in branch, commit and PR; design deviations in `docs/design-changelog.md`.

## Consequences
- Agents of either vendor find the command, owner and GDD fact for an issue in ≤ 3 file reads.
- Adding a module means adding an asmdef plus `AGENTS.md` and `CLAUDE.md`, or `check_repo.py` fails.
- Docs must stay short; long material goes to `docs/` and is linked, not copied.
