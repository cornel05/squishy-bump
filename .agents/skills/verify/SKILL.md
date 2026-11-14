---
name: verify
description: Compile the Squishy Bump Unity project and run its tests via tools/verify.ps1, then summarise failures with file:line. Use after any C# or asset change and before committing or opening a PR. Do not use to build the player (use tools/build-windows.ps1) or to check repo rules only (python tools/check_repo.py).
---

# verify

1. Run from the repo root:
   `powershell -ExecutionPolicy Bypass -File tools/verify.ps1` (add `-PlayMode` when the change touches
   runtime behaviour that has PlayMode tests, or when asked).
   - Editor open (`Temp/UnityLockfile` exists): it uses the live Editor (`unity recompile --strict`, `unity test`).
   - Editor closed: batchmode; first run can take a few minutes while Unity imports.
2. Read the result. The last line is `VERIFY: PASS` or `VERIFY: FAIL`.
3. On failure, report compactly:
   - Compile errors/warnings: `path:line: CSxxxx message` (from the printed lines, or grep
     `TestResults/EditMode.log` for `error CS|warning CS`).
   - Test failures: `FAIL <full test name> <file:line>` + the assertion message (printed by the script;
     full detail in `TestResults/<Mode>.xml`).
   - Then fix the cause, not the test, unless the test is wrong by the GDD.
4. Warnings from `SquishyBump.*` code count as failures for the definition of done.
5. Also run `python tools/check_repo.py --quick` and report its result on one line.

If Unity cannot start (licence, missing Editor), print the exact error and the fix from
`tools/unity-common.ps1` (`unity install <version>`, `unity license activate --personal`) and stop.
