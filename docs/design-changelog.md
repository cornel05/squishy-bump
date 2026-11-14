# Design change log (A02 vs GDD v1.3)

Every deviation from GDD v1.3 made during A02 is one entry here, continuing the numbering of GDD
Appendix D (D01–D32 are in [`gdd/appendix-d-change-log.md`](gdd/appendix-d-change-log.md)).

Format, one entry per line block, newest last:

> **Dnn · dd/mm/yyyy** · what changed and why (one or two sentences) · Scope: GDD sections · Issue: #n

How to add one: run the `design-change` skill (`.agents/skills/design-change/SKILL.md`). It opens an issue
labelled `design-change` and appends the next D-number here in the same PR as the change.

---

**D33** · reserved for SB-00 (Minh Nhật).

**D34 · 15/11/2026** · SB-02 and SB-03 implemented by Đình Duy during the shared M0 setup; Minh Nhật reviews.
Reason: one atomic, agent-driven repo bootstrap avoids a half-configured repo. · Scope: GDD 13.5, 15.3 · Issue: #3, #4
