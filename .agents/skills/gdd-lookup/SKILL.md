---
name: gdd-lookup
description: Find a Squishy Bump GDD v1.3 fact by ID (PF03, H04, T09, AI-2, O07, LV02, LD05, F03, B04) or keyword in docs/gdd/ and cite it as file#heading. Use whenever a value, rule or definition from the design is needed. Do not use for code questions or for the implementation plan (GitHub issues, docs/plan.md).
---

# gdd-lookup

1. **By ID:** open `docs/gdd/ids.md`, find the row (`| PF03 |`), follow its link `file.md#anchor`
   and read that heading's text/table. ID families: F (session stages), O (objects), PF (fighter
   properties), H (hazards), T (acceptance tests), AI-n (AI tests), LV (content), LD (level validation),
   B (cutscene triggers).
2. **By section number:** search for the heading `## 6.1`, `## 10.4`, `## A.4` in `docs/gdd/*.md`.
3. **By keyword:** search `docs/gdd/*.md` (e.g. `rg -n -i "cooldown" docs/gdd`), then read the
   surrounding heading. Balance numbers live in `06-systems-physics-balance.md` §6.1.
4. Answer with the exact value/wording and a citation: `docs/gdd/<file>.md#<anchor>` (GDD §x.y, ID).
   Quote numbers exactly; never round or invent. If the Markdown looks garbled, check the original
   `docs/gdd/SquishyBump_GDD_EN_v1.3.pdf` page and say so.
5. If the fact was changed during A02, `docs/design-changelog.md` overrides the GDD: check it for the
   ID/section before answering.
6. `docs/gdd/ids.md` is generated (`python tools/gdd/gen_ids.py`); never hand-edit it.
