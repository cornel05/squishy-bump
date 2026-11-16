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

**D35 · 16/11/2026** · Client-side prediction and reconciliation (SB-22) cut from A02: a joined player sees their own fruit through the same 100 ms snapshot interpolation as the others. Reason: 9 days to the deadline, issue #20 was the first cut candidate, LAN latency is low. · Scope: GDD 10.4 · Issue: #81

**D36 · 16/11/2026** · Internet play through Unity Relay (SB-70, T16) cut from A02: LAN only, port 7777/UDP. Reason: 9 days left, GDD 10.2 already puts Relay after LAN, T16 is P1. · Scope: GDD 10.2, 10.6, 14.3 T16 · Issue: #82

**D37 · 16/11/2026** · Key rebinding screen (SB-72, T17) cut from A02: default keys of GDD 3.4; binding overrides are still saved and loaded (SB-51) so the screen can come later. Reason: 9 days left, T17 is P1. · Scope: GDD 3.1, 14.3 T17 · Issue: #83
