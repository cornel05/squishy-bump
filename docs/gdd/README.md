# Squishy Bump GDD

Game design document for Squishy Bump (CO3045 Assignment 01, v1.3), a 3D physics party brawler where six jelly fruits bump each other off living arenas.

`SquishyBump_GDD_EN_v1.3.pdf` is the original. The `.md` files are the agent-readable source: one file per chapter, converted with `python tools/gdd/pdf_to_md.py` and then hand-fixed (tables, page-break artefacts, the Figure 2 stage table in 5.3). Re-running the converter overwrites those fixes. When the two disagree, the PDF wins.

`ids.md` indexes every ID (F, O, PF, H, T, AI, LV, LD, B) to the table that defines it. Regenerate it with `python tools/gdd/gen_ids.py`.

## Contents

| File | Chapter |
|---|---|
| [00-front-matter.md](00-front-matter.md) | Front matter: document map, at a glance, executive summary |
| [01-pitch.md](01-pitch.md) | 1. The pitch |
| [02-features-references.md](02-features-references.md) | 2. Features and references |
| [03-how-to-play.md](03-how-to-play.md) | 3. How to play |
| [04-fighter.md](04-fighter.md) | 4. The fighter |
| [05-design-framework.md](05-design-framework.md) | 5. Design framework |
| [06-systems-physics-balance.md](06-systems-physics-balance.md) | 6. Systems, physics and balance |
| [07-living-arena-hazards.md](07-living-arena-hazards.md) | 7. Living Arena hazards |
| [08-world-characters.md](08-world-characters.md) | 8. World and characters |
| [09-levels-content.md](09-levels-content.md) | 9. Levels and content |
| [10-technology-architecture.md](10-technology-architecture.md) | 10. Technology and architecture |
| [11-interface.md](11-interface.md) | 11. Interface |
| [12-art-audio.md](12-art-audio.md) | 12. Art and audio |
| [13-technical-plan-production.md](13-technical-plan-production.md) | 13. Technical plan and production |
| [14-validation-rubric-map.md](14-validation-rubric-map.md) | 14. Validation and rubric map |
| [15-references-authorship.md](15-references-authorship.md) | 15. References and authorship |
| [appendix-a-ai-behaviour.md](appendix-a-ai-behaviour.md) | Appendix A. AI behaviour specification |
| [appendix-b-cutscenes.md](appendix-b-cutscenes.md) | Appendix B. Cutscene script and triggers |
| [appendix-c-asset-ledger.md](appendix-c-asset-ledger.md) | Appendix C. Asset and AI-generation ledger |
| [appendix-d-change-log.md](appendix-d-change-log.md) | Appendix D. Change log |
| [appendix-e-playtest.md](appendix-e-playtest.md) | Appendix E. Playtest plan and forms |
| [ids.md](ids.md) | Generated ID index |

Figures are not reproduced; their captions are kept in place as `*Figure N: ...*`.
