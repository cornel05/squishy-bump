# Squishy Bump: kế hoạch implement A02

Lập 14/11/2026. Deadline A02: **25/11/2026**. Điểm xuất phát: chưa có code. MVP lấy theo GDD v1.3, mục 13.4.

## Milestone

| Milestone | Hạn (23:59) | Exit condition |
|---|---|---|
| M0 Setup | 15/11 | Repo + Unity project + LFS/YAMLMerge, ScriptableObject data, interface chung |
| M1 Core loop | 17/11 | 1 trận L1 từ countdown tới kết quả (1 người + 5 bot D1) |
| M2 Network & rules | 19/11 | 2+ máy LAN chơi hết trận L1, mọi máy cùng kết quả, rớt mạng không crash; Rank chạy |
| M3 Hazards L2–L5 & bots | 21/11 | Đủ 5 level; T09 pass cho H01–H05 + H07; T10 pass; bot D2–D5 |
| M4 Freeze & playtest | 23/11 | Chạy hết P0 test, có ghi lại; playtest xong; không còn bug chặn |
| M5 Submission | 25/11 | Build chạy theo README; credits, change log, test record, playtest report |

Nguyên tắc kỹ thuật quan trọng nhất: **mọi gameplay chạy trong NGO host mode ngay từ M1**, kể cả Single Player. Đỡ phải viết lại khi thêm network ở M2.

## Chia vai (đã chia lại cho cân code, phải ghi D33)

| | Đình Duy | Minh Nhật |
|---|---|---|
| Hệ thống chính | PlayerMotor, CollisionResolver/Wobble, FighterProfile, RollVisual/Camera | MatchDirector, luật Survival/Rank, ScoreService, knockout |
| Network | Đồng bộ host-authoritative (input/snapshot), hazard events, prediction | SessionManager (host/join LAN), match events, disconnect |
| Hazard/level | H01, H02, H07, H03, H04, H05; scene L2, L3 | H06 ice tile (BFS), scene L1, L4, L5 |
| AI | — | BotBrain FSM, danger map, A*, D1–D5 |
| UI | Menu, settings, arena card, banner, audio, save | HUD, 3D lobby, kết quả/scoreboard/spectator, tutorial |
| Nộp bài | Build + README | Credits, change log, test record, playtest report |

Số issue code: Duy 23, Nhật 22. Mỗi tối thứ 2/4/6 lọc board theo assignee, lệch thì chuyển bớt.

## Tier và thứ tự cắt khi trễ

- **tier:A-gate**: giữ gate "build chạy theo README" và core loop. Không cắt.
- **tier:B-mvp**: phần còn lại của MVP 13.4.
- **tier:C-full**: full scope (Relay, cutscene, rebinding, tiếng Việt) + ứng viên cắt.

Nếu đến 21/11 vẫn trễ, đề xuất cắt theo thứ tự này. Cả hai phải đồng ý, và mỗi cắt giảm là một entry trong change log:
1. Client prediction (SB-22): trên LAN latency thấp nên ít ảnh hưởng.
2. 3D lobby đi bộ → lobby 2D (SB-25).
3. Chiến thuật theo class của bot D3–D5 (một phần SB-47).
4. Giảm playtest còn 1 vòng (SB-53).

## Board và quy trình

- Cột Status: Todo → In Progress → Done. WIP tối đa 2 việc In Progress mỗi người.
- Workflow có sẵn: Item added → Todo; Item closed → Done; PR merged → Done.
- Phụ thuộc cứng gắn bằng "blocked by" của GitHub (badge Blocked). Phụ thuộc mềm ghi trong body issue, kèm cách làm stub.
- Mỗi issue một branch `feat/SB-xx-ten`, PR ghi `Closes #n`, squash merge (người còn lại review là tuỳ chọn).
- 4 rule: interface trước (SB-06); "xong" = đã merge vào main; mỗi người một scene test riêng; kẹt quá nửa buổi thì gắn `blocked`, báo người kia và chuyển issue. Sync 10 phút mỗi tối.
- Phải làm khác GDD thì mở issue label `design-change` và ghi vào `docs/design-changelog.md`.

## Thay đổi sau khi lập kế hoạch

- SB-02 và SB-03 do Duy làm trong buổi setup M0 chung (bootstrap repo một lần bằng agent), Nhật review. Ghi là D34.
- Duy dùng Claude Code, Nhật dùng Codex (cả hai trên Windows). Quy tắc agent nằm trong `AGENTS.md`; `CLAUDE.md` chỉ import nó. Skill dùng chung ở `.agents/skills/`.

## Bản nháp D33 cho change log

> **D33 · 14/11/2026** · A02 deadline confirmed as 25/11/2026; milestones re-planned as M0–M5 (15, 17, 19, 21, 23, 25/11). Implementation roles rebalanced so that both members own core code: Đình Duy – movement, collisions, hazards H01–H05 and H07, host simulation and snapshot sync, build; Minh Nhật – match rules and scoring, session and lobby networking, bot AI, ice tiles H06 and L4–L5. Tasks are tracked as GitHub issues with acceptance criteria. · Scope: Sections 13.5, 15.3
