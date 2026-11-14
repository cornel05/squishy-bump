---
name: design-change
description: Record a deviation from GDD v1.3 for Squishy Bump - open a GitHub issue labelled design-change and append the next D-number entry to docs/design-changelog.md. Use whenever implementation must differ from the GDD (values, rules, scope cuts). Do not use for bugs or for changes that stay within the GDD.
---

# design-change

Input: what changes, why, the GDD sections/IDs affected, and the issue/PR it belongs to.

1. Confirm it really deviates: look up the current GDD text (`gdd-lookup` skill). If the GDD already
   allows it, stop and say so.
2. Find the next D-number: highest `**Dnn` in `docs/design-changelog.md` (also note reserved numbers,
   e.g. "D33 reserved") + 1.
3. Open the issue (English title, body may be English or Vietnamese like the other issues):
   `gh issue create --title "[Dnn] <short change>" --label design-change --body-file body.md`
   Body: what changes, why, GDD scope (sections/IDs), impact on other issues, who must agree.
   Scope cuts from the plan's cut order need both members' agreement: say that in the body.
4. Append to `docs/design-changelog.md`, newest last, exactly in the header format:
   `**Dnn · dd/mm/yyyy** · <what changed and why> · Scope: GDD <sections> · Issue: #<new issue>`
5. Commit with the change it describes: `docs: Dnn <short change>` (or inside the SB commit), so the
   entry and the code land in the same PR.
6. If balance values change, update the ScriptableObject asset, not code constants.

Output: issue URL and the new D-entry line.
