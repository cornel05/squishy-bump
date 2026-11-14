---
name: review-pr
description: Review a Squishy Bump pull request against its issue's acceptance criteria, GDD values, module boundaries and host-authority rules, and post findings as PR review comments. Use when asked to review a PR or the teammate requests review. Do not use for your own unfinished branch (use verify) or for general refactoring.
---

# review-pr

Input: PR number `p`.

1. Load context: `gh pr view p --json number,title,body,files,headRefName,author` and `gh pr diff p`.
   Find the linked issue from `Closes #n`; `gh issue view n` for the acceptance checkboxes and GDD refs.
2. Check out locally (`gh pr checkout p`) and run `tools/verify.ps1` and `python tools/check_repo.py`.
   Note the result lines.
3. Review checklist (one finding per real problem; skip style nits):
   - **Acceptance:** each checkbox is met by code/tests in this PR; evidence in the PR body is real.
   - **GDD values:** numbers match `docs/gdd/` (look up IDs in `docs/gdd/ids.md`, balance in GDD 6.1) and
     come from ScriptableObjects, not literals. A deviation must have a D-entry in `docs/design-changelog.md`.
   - **Boundaries:** asmdef references follow ADR-0003 (only Core + own packages); no module→module refs;
     namespaces = assembly names; Core changes are reviewed by both members.
   - **Host authority:** gameplay state written only on the host (`IsServer`); clients send `InputCommand`;
     no keyboard reads in gameplay modules; Single Player also runs in host mode.
   - **Unity hygiene:** every new asset has its `.meta`; no hand-written GUIDs; no edits to
     `Library/ Temp/ Logs/ UserSettings/`; binaries in LFS; no files > 20 MB; scene edits only by the owner.
   - **Tests:** new logic has tests named `<Scenario>_<Expected>`; `docs/testing.md` updated if T-tests ran.
4. Post findings as review comments on lines:
   `gh api repos/{owner}/{repo}/pulls/p/reviews -f event=COMMENT -f body="<summary>" -F "comments[][path]=<file>" ...`
   or simpler `gh pr review p --comment --body-file review.md` with `path:line - problem - fix` bullets.
   Use `--request-changes` only for blockers (failing verify, broken authority/boundary rule, missing
   acceptance criterion); `--approve` when none remain.

Output: verdict (approve / changes requested / comment) and the findings list.
