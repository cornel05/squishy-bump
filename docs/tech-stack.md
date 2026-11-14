# Tech stack

Pinned in `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json`. Change a version only in a PR
that updates this table. Rule for picking: newest stable release that supports Unity 6000.3 (checked on
packages.unity.com on 15/11/2026); boring and proven over new, because the deadline is 25/11/2026.

| Item | Version | Reason |
|---|---|---|
| Unity Editor | **6000.3.28f1** (6.3 LTS) | GDD 10.2: Unity 6 LTS, patch pinned at project creation. Newest 6000.3 LTS patch available |
| Universal RP | 17.3.0 | Ships with 6000.3; GDD 10.2 URP |
| Netcode for GameObjects | 2.14.0 | GDD 10.2 "NGO 2.x". Newest 2.x; 3.x needs Unity 6000.7 |
| Unity Transport | 2.7.5 | GDD 10.2 "Unity Transport 2.x": UDP, LAN port 7777, network simulator |
| Input System | 1.20.1 | GDD 10.2 / 3.4 rebindable actions. Template version, newest |
| Cinemachine | 3.1.7 | GDD 10.2 "Cinemachine 3": follow, orbit, spectator, impulse |
| Multiplayer Play Mode | 2.0.2 | GDD 10.2: up to four players on one machine during development |
| Multiplayer Tools | 2.2.12 | Network profiler / runtime stats for T14–T15 and the network simulator |
| Unity Pipeline | 1.0.0 | Lets agents drive a live Editor (`unity recompile`, `unity test`, `unity command`) |
| Test Framework | 1.6.0 | EditMode/PlayMode tests (`tools/verify.ps1`) |
| Timeline | 1.8.13 | Cutscenes (SB-71, Appendix B) |
| uGUI (incl. TextMeshPro) | 2.0.0 | Menus and HUD (GDD 11) |
| IDE support | Visual Studio 2.0.26, Rider 3.1.0 | Each dev's IDE; no runtime effect |

Removed from the URP template: Visual Scripting, Collab Proxy, Multiplayer Center, AI Navigation
(bots use the custom grid A* of GDD Appendix A.4, not NavMesh), plus the tutorial and sample scene assets.

Not yet added: Multiplayer Services (Sessions + Relay) for Internet play. GDD 10.2 says it comes after LAN is
stable; it is added in SB-70.

Tooling: Python 3.10+ (repo checks, GDD scripts), Git LFS, GitHub CLI, Unity CLI (`winget install Unity.CLI`).
