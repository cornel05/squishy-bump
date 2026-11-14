# 10. Technology and architecture

> **In this chapter:** Engine choice, component ownership, network authority and performance targets.

## 10.1 Form, view, platform and device

| Template field | Specification |
|---|---|
| Technical Form | Real-time 3D, cute low-poly art with flat colours and soft lighting; physics-driven characters. |
| View | Third-person follow camera per player: distance 9 m, height 6 m, pitch 35 degrees, orbit with the mouse. The camera never rolls with the fighter. Eliminated players switch to a spectator camera. |
| Platform | Windows 10 or 11 x64 downloadable executable. |
| Language | C# on Unity 6 LTS. In-game text in English with a Vietnamese option. |
| Device | One PC per player with keyboard and mouse. Gamepad support is a stretch goal. Multiplayer needs the PCs on one LAN or hotspot, or Internet access for the relay. |

## 10.2 Engine and implementation rationale

The project uses Unity 6 LTS (6000.x; the exact patch is pinned when the project is created and recorded in the README) with the Universal Render Pipeline. Unity Personal is free for the team, and every package listed below is free at course scale.

| Package | Use in Squishy Bump |
|---|---|
| Netcode for GameObjects 2.x | Session roles (host, client), RPCs, network objects and the custom snapshot channel. |
| Unity Transport 2.x | UDP transport for LAN (direct IP, default port 7777) and relay connections; its network simulator is used for adverse-condition tests. |
| Multiplayer Services (Sessions + Relay) | Internet play through a short join code, without port forwarding. Implemented after LAN is stable. |
| Input System | Rebindable actions for move, jump, dash, camera and menus. |
| Cinemachine 3 | Follow, orbit and spectator cameras, impulse-based screen shake. |
| Multiplayer Play Mode | Up to four players on one machine during development; final tests use separate PCs. |

Unity was chosen because it combines a mature 3D physics engine (PhysX), first-party networking with relay support, large learning resources for a team without prior game experience, and a direct Windows build pipeline. Physics that matters for grading (fighter collisions, knockback, weight-class effects, hazard forces) is written by the team on top of PhysX, as described in Section 6.

## 10.3 Components and data ownership

*Figure 9: Component architecture and network authority*

| Component | Responsibility | Authoritative data |
|---|---|---|
| SessionManager | Host, join (LAN or relay), 3D lobby slots, fruit picks, ready state, version check, disconnects | Session members, chosen fruits and phase (host) |
| MatchDirector | Match state machine: Intro, Countdown, Playing, Result, with the time limit; lives and respawns under the Rank rule | Match clock, alive list, lives, elimination order (host) |
| PlayerMotor | Traction-limited movement, jump, dash, air control | Fighter position and velocity (host) |
| FighterProfileApplier | Applies the chosen fruit's weight-class profile (mass, radius, top speed, traction) at spawn; fixed for the session | Fruit and class per player (host) |
| CollisionResolver | Custom sphere-sphere detection, impulse, knockback scaling, positional correction | Wobble meter, stun timer (host) |
| HazardDirector | Schedules and simulates ring drops, cannons, tiles, the turntable, the obstacle ring and pusher blocks | Hazard states, seeded schedule (host) |
| BotBrain, BotSensors | FSM, utility targeting, danger map and grid A\* (Appendix A) | Bot input commands (host) |
| ScoreService | Placements, Rank ring-out points with last-touch credit, tie rules, session ranking, local best records | Session score table (host) |
| InputRouter | Reads local input and sends sequenced commands | Local only |
| SnapshotInterpolator | Buffers snapshots, interpolates remote fighters, reconciles the local fighter | Presentation only |
| RollVisual, CameraRig | Rolling rotation, squash and stretch, camera follow and shake | Presentation only |
| UI, AudioDirector | HUD, menus, results and sound (Chapters 11 and 12) | Local settings |

All tunable numbers live in ScriptableObject assets: BalanceProfile (Section 6.1), ClassProfile ×3, FruitDefinition ×6 (class, colour, mesh), LevelDefinition ×5 (arena, hazards, time limits) and BotProfile ×5 (Appendix A). Code reads these assets instead of hard-coding values, so a playtest change is a data change recorded in the change log.

## 10.4 Host authority and message design

The host is the single source of truth. It runs the physics for every fighter, every hazard and every bot at a fixed 60 Hz step. Clients send input; they never send positions or results. This prevents desynchronised collisions and cheating, and it matches Netcode for GameObjects, where networked rigidbodies are simulated by the server and are kinematic on clients.

| Message | Direction | Content, rate and validation |
|---|---|---|
| Input command | Client to host | Sequence number, move vector (clamped to length 1), jump and dash buttons, camera yaw. 60 Hz, unreliable; each packet repeats the last three commands to survive loss. |
| World snapshot | Host to all | Host tick, and for each fighter: position, velocity, Wobble, lives, flags, last processed input. 30 Hz, unreliable. About 200 bytes per snapshot. |
| Hazard event | Host to all | Ring-drop warning, tile warning, cannon aim and fire, turntable reversal, pusher departure, with the host tick at which they take effect. Reliable. |
| Match event | Host to all | Countdown, ring-out (fighter, credited fruit, tick), respawn, elimination (fighter, rank, tick), match result, score table. Reliable and idempotent by event ID. |
| Lobby state | Host to all | Slots, names, chosen fruits, ready flags, rule, session length, build version. Reliable. |

Local feel. A client predicts its own fighter with the same PlayerMotor code, so movement, jumps and dashes respond on the same frame. When a snapshot arrives, the client replays unacknowledged inputs from the host state; if the error is above 0.3 m it corrects the visual over 0.1 s, and above 1.5 m it snaps. Collisions with other fighters and hazards are resolved only on the host, so a client sees a bump one round trip after it happens. Remote fighters are interpolated with a 100 ms buffer.

Bandwidth. A snapshot of six fighters is about 200 bytes. At 30 Hz this is about 6 KB/s per client, or about 30 KB/s of host upload for five clients, which is small for LAN and relay.

Disconnects. A client that disconnects during a match is eliminated at that tick and keeps its placement and Rank points; its fruit becomes free again. It can rejoin the lobby between matches. If the host leaves, the session ends and clients return to the main menu with a message; host migration is out of scope.

## 10.5 Randomness, timing and saves

Every random decision (tile selection, cannon targets, bot rosters, bot fruit picks, bot aim noise, respawn point choice) uses a pseudo-random generator seeded by the host at match start. The same seed reproduces the same hazard schedule, which makes bugs reproducible and keeps a Survival retry fair. Hazard timing is measured in host ticks, never in client wall-clock time, so network delay cannot change when a ring drops or a tile falls.

Local saves (JSON in the user data folder) store settings, key bindings, the last chosen fruit, the best Survival run and the best Rank score. Match state is never saved: a match is short and restarts cleanly.

## 10.6 Performance and test environment

| Target | Value |
|---|---|
| Reference PC | Windows 11, 32 GB RAM, NVIDIA GeForce RTX 4050 Laptop GPU (6 GB), Intel Core i7-12700H CPU. |
| Frame rate | 60 FPS median at 1920×1080; 95th-percentile frame time at most 25 ms |
| Physics | Fixed step 60 Hz; at most 6 fighters, 61 tiles, 4 cannons and 4 projectiles active |
| Network gate | A full 6-player session on separate PCs over one LAN; adverse test at 100 ms RTT and 2% packet loss with the Unity Transport simulator |
| Relay gate (after LAN) | A 4-player session through a join code between two different networks |
