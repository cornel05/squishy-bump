# ADR-0002: Host mode everywhere

**Status:** accepted · **Date:** 15/11/2026 · **Refs:** GDD 10.4, plan "most important technical rule"

## Context
GDD 10.4 makes the host authoritative: it simulates fighters, hazards, bots and rules at 60 Hz, and clients
send inputs. Writing Single Player as a separate offline path would mean two code paths and a rewrite in M2.

## Decision
- Every match runs inside Netcode for GameObjects with a host, **including Single Player** (host + bots,
  no remote clients).
- Gameplay code (Physics, Hazards, AI, Match) runs only where `IsServer`/host is true and consumes
  `InputCommand`s, whether they come from the local player, a remote client or a bot.
- Clients never write gameplay state; they send `InputCommand` and render snapshots/events.

## Consequences
- The networked path is exercised in every Single Player test from M1 onward.
- Small overhead of a local host in Single Player; acceptable for ≤ 6 fighters (GDD 10.6).
- Code that reads the keyboard inside gameplay modules is a review blocker.
