---
name: issue-start
description: Start work on one GitHub issue of Squishy Bump (given an issue number or SB id). Use at the beginning of any coding task tied to an issue, to check hard deps, create the branch, load module rules and GDD refs, and post a plan. Do not use for reviewing a PR (review-pr) or finishing work (issue-finish).
---

# issue-start

Input: issue number `n` (or an SB id; resolve with `gh issue list --search "SB-xx in:title" --json number,title`).

1. Read the issue: `gh issue view n --json number,title,body,labels,assignees,milestone`.
   - The SB id is in the title (`[SB-xx]`). The "Tiêu chí hoàn thành" checkboxes are hard acceptance criteria.
   - The GDD refs are on the `**GDD:**` line.
2. Hard deps are listed under "⛔ Phải xong (merge vào main) trước khi bắt đầu" (and GitHub "blocked by").
   For each `#m`: `gh issue view m --json state,title`. If any is still OPEN, **stop**: list them and
   tell the user. Soft deps ("phụ thuộc mềm") only need a stub behind a Core interface.
3. Branch from an up-to-date main:
   `git switch main && git pull --ff-only && git switch -c feat/SB-xx-<short-slug>`
   (slug: 2–4 lowercase words from the title, translated to English).
4. Find the module(s) from the issue's `area:` label and title, then read
   `Assets/_Project/Scripts/<Module>/AGENTS.md` (owner, invariants). If the issue touches more than one
   module, say so: plan mode (Claude) and Core changes need review from both members.
5. For each GDD ref: an ID → `docs/gdd/ids.md` → open the linked file#heading; a section number →
   the `## <number>` heading in `docs/gdd/`. Note exact values (balance numbers come from GDD 6.1).
6. Post a short plan as an issue comment (≤ 15 lines, English):
   `gh issue comment n --body-file <tmpfile>` with:
   - Branch name
   - Files/modules to touch
   - Each acceptance checkbox → how it will be met and verified (test name or command)
   - GDD values used (with ID/section)
   - Open questions / design deviations (→ `design-change` skill)
7. Optionally move the board item to In Progress (WIP limit 2 per person).

Output to the user: branch, hard-dep status, the plan comment URL.
