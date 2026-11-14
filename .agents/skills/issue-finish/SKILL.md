---
name: issue-finish
description: Finish a Squishy Bump issue - run verify and repo checks, tick satisfied acceptance checkboxes, update docs/testing.md, and open the PR with Closes #n requesting the teammate's review. Use when the code for an issue is complete on its feat/SB-xx branch. Do not use to start an issue (issue-start) or to review someone else's PR (review-pr).
---

# issue-finish

Input: issue number `n` (default: from the branch name `feat/SB-xx-*`, resolved via `gh issue list --search "SB-xx in:title"`).

1. Run the `verify` skill. Run `python tools/check_repo.py` (full). Both must pass; otherwise stop and fix.
2. Map evidence to criteria: for every "Tiêu chí hoàn thành" checkbox, name the command/test/file that
   proves it. Only tick boxes with real evidence.
3. Tick them in the issue body:
   `gh issue view n --json body -q .body > body.md`, change `- [ ]` → `- [x]` for proven lines only,
   `gh issue edit n --body-file body.md`. Never tick boxes that need the other person (e.g. "Nhật clone…").
4. If any T01–T20 acceptance test or new automated test ran, update `docs/testing.md` (status, date,
   short commit, who, evidence) and commit it: `docs: record T0x result` or within the SB commit.
5. Commit remaining work (`SB-xx: <imperative>`), then `git push -u origin HEAD` (never `--force`).
6. Open the PR from the template:
   `gh pr create --title "SB-xx: <summary>" --body-file pr.md --reviewer <teammate>`
   where `pr.md` follows `.github/pull_request_template.md`, contains `Closes #n` on its own line,
   the commands run with their result lines, and the GDD refs. Teammate: `ZteHahah` if you are
   `cornel05`, else `cornel05` (`gh api user -q .login`).
7. Design deviated? Make sure the `design-change` skill was run and the D-entry is in this PR.

Output: PR URL, ticked/unticked criteria with the reason for any unticked one.
