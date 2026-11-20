# Design change log (A02 vs GDD v1.3)

Every deviation from GDD v1.3 made during A02 is one entry here, continuing the numbering of GDD
Appendix D (D01–D32 are in [`gdd/appendix-d-change-log.md`](gdd/appendix-d-change-log.md)).

Format, one entry per line block, newest last:

> **Dnn · dd/mm/yyyy** · what changed and why (one or two sentences) · Scope: GDD sections · Issue: #n

How to add one: run the `design-change` skill (`.agents/skills/design-change/SKILL.md`). It opens an issue
labelled `design-change` and appends the next D-number here in the same PR as the change.

---

**D33 · 14/11/2026** · Chốt hạn Assignment 02 ngày 25/11/2026, thay mốc dự kiến của
GDD 13.5; lập kế hoạch M0–M5 và chia lại phần triển khai giữa hai thành viên.
M0 setup 15/11, M1 core loop 17/11, M2 network/rules 19/11, M3 hazards/bots 21/11,
M4 freeze/playtest 23/11, M5 submission 25/11 (23:59). Phân công hệ thống theo
[`IMPLEMENTATION_PLAN_A02.md`](IMPLEMENTATION_PLAN_A02.md), mục Vai trò, và assignee trên GitHub:
Đình Duy phụ trách physics/presentation, hazard H01–H05/H07, sync, menu, L2/L3, build/README;
Minh Nhật phụ trách AI, Match/Rank/score/knockout, session/match events/disconnect, H06,
HUD/lobby/results/tutorial, L1/L4/L5 và hồ sơ nộp; Core/App làm chung.
Lý do: lịch A02 được chốt và cân lại tải triển khai. Nguồn: issue [#1](https://github.com/cornel05/squishy-bump/issues/1)
đã đóng, đủ ba tiêu chí, gồm cả hai cùng đọc và đồng ý; kế hoạch trong repo.
Mốc milestone là kế hoạch, không phải xác nhận đã đạt. · Scope: GDD 13.5, 15.3, App. D · Issue: #1

**D34 · 15/11/2026** · SB-02 and SB-03 implemented by Đình Duy during the shared M0 setup; Minh Nhật reviews.
Reason: one atomic, agent-driven repo bootstrap avoids a half-configured repo. · Scope: GDD 13.5, 15.3 · Issue: #3, #4

**D35 · 16/11/2026** · Client-side prediction and reconciliation (SB-22) cut from A02: a joined player sees their own fruit through the same 100 ms snapshot interpolation as the others. Reason: 9 days to the deadline, issue #20 was the first cut candidate, LAN latency is low. · Scope: GDD 10.4 · Issue: #81

**D36 · 16/11/2026** · Internet play through Unity Relay (SB-70, T16) cut from A02: LAN only, port 7777/UDP. Reason: 9 days left, GDD 10.2 already puts Relay after LAN, T16 is P1. · Scope: GDD 10.2, 10.6, 14.3 T16 · Issue: #82

**D37 · 16/11/2026** · Key rebinding screen (SB-72, T17) cut from A02: default keys of GDD 3.4; binding overrides are still saved and loaded (SB-51) so the screen can come later. Reason: 9 days left, T17 is P1. · Scope: GDD 3.1, 14.3 T17 · Issue: #83

## Đối chiếu issue `design-change` ngày 20/11/2026 (SB-62)

Tìm trên GitHub: `repo:cornel05/squishy-bump label:design-change`; kết quả #1, #81, #82, #83.
D34 là phân công setup được ghi sẵn tại main. Không tạo D-number mới cho việc bổ sung hồ sơ này.

| Quyết định | Issue / bằng chứng trong main | Tình trạng hồ sơ |
| --- | --- | --- |
| D33 | #1 đóng, đủ 3/3 tiêu chí; `docs/IMPLEMENTATION_PLAN_A02.md`, `docs/plan.md`, `AGENTS.md` | Khôi phục nội dung ở trên, thay chỗ giữ số trống. |
| D35 | #81; `docs/design-changelog.md`, README Known limits; `MatchClientView` dùng nội suy 100 ms | Đã áp dụng trong repo. Đình Duy đồng ý (body #81); Minh Nhật xác nhận ngày 20/11/2026 (báo qua Đình Duy). Cả hai đã đồng ý. |
| D36 | #82; manifest không có Relay; `docs/testing.md` ghi T16 cut; PR #84 đã merge | Đã ghi cắt A02. Body #82 vẫn chờ Minh Nhật xác nhận (D35, D37 đã xác nhận 20/11/2026; D36 chưa). Relay vẫn thuộc full scope GDD. |
| D37 | #83; README ghi chưa có UI đổi phím; T17 cut; PR #84 đã merge | Đã ghi cắt A02, vẫn lưu binding overrides. Minh Nhật xác nhận ngày 20/11/2026 (báo qua Đình Duy). |

Các mục chưa triển khai/kiểm chứng (L4/L5, Rank, LAN nhiều PC, playtest, tutorial, cutscene,
font Baloo 2/VI) là khoảng thiếu triển khai, không tự coi là quyết định cắt scope.
3D lobby và chiến thuật bot chưa bị cắt bởi các entry hiện có. Không ghi pass test hoặc
hoàn thành feature từ trạng thái issue/PR đơn thuần. Hồ sơ này cần đối chiếu tiếp khi main thay đổi.
