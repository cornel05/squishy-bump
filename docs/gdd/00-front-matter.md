# Front matter

**GAME DESIGN DOCUMENT**

Squish, bump, be the last fruit standing.

CO3045 Game Programming · Assignment 01 · v1.3 (final) · 11 Nov 2026

- **Group**: 18
- **Class**: CC01
- **Members and student IDs**: Ngo Tran Dinh Duy (2352179), Bui Doan Minh Nhat (2352855)
- **Lecturer**: Dr. Phan Tran Minh Khue

About this document. This merged version combines the chapters of both members. The document map on the next page lists every chapter and its owner; all AI-generated material is declared in Appendix C.

## Document map

| Chapter | Part | Owner |
|---|---|---|
| At a glance and executive summary | Front | Minh Nhật (draft: Đình Duy) |
| 1 The pitch | VISION | Đình Duy |
| 2 Features and references | VISION | Đình Duy |
| 3 How to play | PLAY | Đình Duy |
| 4 The fighter | PLAY | Đình Duy |
| 5 Design framework | DESIGN | Đình Duy |
| 6 Systems, physics and balance | DESIGN | Đình Duy |
| 7 Living Arena hazards | DESIGN | Đình Duy |
| 8 World and characters | DESIGN | Minh Nhật |
| 9 Levels and content | DESIGN | Minh Nhật |
| 10 Technology and architecture | BUILD | Đình Duy |
| 11 Interface | BUILD | Minh Nhật (draft: Đình Duy) |
| 12 Art and audio | BUILD | Đình Duy |
| 13 Technical plan and production | BUILD | Minh Nhật |
| 14 Validation and rubric map | PROOF | Đình Duy |
| 15 References and authorship | PROOF | Minh Nhật |
| Appendix A AI behaviour specification | APPENDICES | Đình Duy |
| Appendix B Cutscene script and triggers | APPENDICES | Minh Nhật |
| Appendix C Asset and AI-generation ledger | APPENDICES | Đình Duy |
| Appendix D Change log | APPENDICES | Minh Nhật |
| Appendix E Playtest plan and forms | APPENDICES | Minh Nhật |

## At a glance

| Item | Squishy Bump |
|---|---|
| Genre | 3D physics party brawler (ring-out) |
| Platform | Windows 10/11 PC, keyboard and mouse |
| Players | Single Player: 1 human + 5 bots · Multiplayer: 2–6 humans |
| Session rules | Survival (one life) · Rank (three lives, ring-out points), in both modes |
| MVP (A02) | All five levels, both rules, Single Player and LAN Multiplayer; full scope adds Relay and cutscenes |
| Levels | 5 arenas played in order from L1, one new mechanic each |
| Match length | 60–100 s typical, 150 s time limit |
| Characters | Six squishy jelly fruits in three weight classes, picked in a walkable 3D lobby |
| Unique selling points | Fruit weight classes · Living Arena |
| Engine | Unity 6 LTS · URP · C# |
| Network | Netcode for GameObjects, host-authoritative; LAN first, Unity Relay second |
| Physics | Custom impulse collisions, Wobble knockback, traction model, ballistics |
| AI | FSM + utility + danger map + grid A\*, tiers D1–D5 |

## Executive summary

Squishy Bump is a 3D physics party brawler for Windows PC. After closing time in a toy store, six squishy jelly fruits roll, jump, dash and bump each other off play mats and toy tables. There is no health bar and no weapon: a fruit loses only by falling off the arena. Matches last 60–100 seconds, so the game suits short, social sessions for players aged 10 and older.

Two ideas set the game apart. Fruit weight classes give the six fruits three physical profiles: Watermelon and Pineapple are Heavy, Apple and Mangosteen are Normal, Peach and Blueberry are Light. The fruit is picked in a walkable 3D lobby and kept for the whole session. Living Arena makes each of the five levels act by itself: rings that drop away, a reversing turntable inside a counter-rotating ring of obstacle boxes, air and fire cannons around the rim, a frozen lake of cracking ice tiles, and a final arena that combines them. Every hazard is telegraphed and simulated by the host, so the arena is fair for everyone.

Players choose Single Player (one human against five AI bots with five difficulty tiers, always all five levels) or Multiplayer (two to six humans over LAN, then over the Internet through Unity Relay, with the host choosing L1 to Lk). Both modes offer two rules: Survival, one life and the last fruit standing wins, and Rank, three lives with respawns and a point for every ring-out. The story frames the game as the yearly Squishy Cup, in which six toy-store squishies compete for the Golden Squish trophy on the top shelf.

The game is built with Unity 6 LTS, C#, URP and Netcode for GameObjects in a host-authoritative architecture. The team writes the gameplay physics (impulse collisions, Wobble knockback, traction, ballistics, sweep forces) and the AI module (state machine, utility targeting, danger map and grid A\*). The MVP for Assignment 02 covers all five levels, both rules, Single Player and LAN Multiplayer; Internet play and the cutscenes complete the full scope (Section 13.4). All AI-generated material is declared in Appendix C.

Drafted by Đình Duy for review by Minh Nhật.
