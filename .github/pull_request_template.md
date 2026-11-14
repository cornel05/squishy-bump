Closes #

**SB id:** SB-

## What and why
<!-- What changed, and why (link the issue's acceptance criteria). -->

## How verified
<!-- Commands you ran and their result lines. -->
- `tools/verify.ps1` → 
- `python tools/check_repo.py` → 
- Manual check (scene / build / LAN): 

## GDD refs
<!-- IDs and sections, e.g. PF03, GDD 6.2 (docs/gdd/ids.md). Note any value that differs from the GDD. -->

## Checklist
- [ ] `tools/verify.ps1` passed (0 errors, 0 warnings from SquishyBump.*)
- [ ] `python tools/check_repo.py` passed
- [ ] Every new asset has its `.meta` committed (no hand-written GUIDs)
- [ ] Acceptance checkboxes in the issue ticked with evidence
- [ ] `docs/testing.md` updated if T-tests ran
- [ ] Design changed → `design-change` issue + entry in `docs/design-changelog.md`
- [ ] Review requested from the teammate
