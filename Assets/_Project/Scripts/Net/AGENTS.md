# SquishyBump.Net

**Owner:** Sync: Đình Duy (`cornel05`) · Session: Minh Nhật (`ZteHahah`)  
**Assembly / namespace:** `SquishyBump.Net`

## Responsibility
Netcode for GameObjects 2.x over Unity Transport: session host/join on LAN (port 7777), input upload, snapshot download, hazard and match event replication, prediction, disconnects.

## Design refs
- GDD sections: 6.1 (network row), 10.4, 10.5 (`docs/gdd/`)
- IDs: T06, T16 (look up in `docs/gdd/ids.md`)

## Invariants
- Host-authoritative: the host simulates everything at 60 Hz; clients send `InputCommand` only.
- Host mode everywhere, even in Single Player (ADR-0002).
- Clients never write gameplay state. RPCs from clients carry input, never results.
- Sync code (Duy) and Session code (Nhật) live in separate subfolders `Sync/` and `Session/`.

- Room options (capacity 2-6, private/public, password) are decided by the host only. The password never travels as text (hash in the join payload) and never appears in `LobbyState` or the LAN beacon. Changing the wire format bumps `StubSessionService.Protocol`.

## Public API surface
`SessionManager` (Session), input/snapshot sync components (Sync).

## Related issues
SB-20 #18, SB-21 #19, SB-22 #20, SB-23 #21, SB-24 #22, SB-30 #28, SB-70 #52

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
