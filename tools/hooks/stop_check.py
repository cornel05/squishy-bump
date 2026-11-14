#!/usr/bin/env python3
"""Claude Code Stop hook: run `check_repo.py --quick` and hand problems back to Claude.
Never blocks twice in a row (respects stop_hook_active)."""
import json
import subprocess
import sys
from pathlib import Path

data = json.load(sys.stdin)
if data.get("stop_hook_active"):
    sys.exit(0)
root = Path(__file__).resolve().parents[2]
r = subprocess.run([sys.executable, str(root / "tools/check_repo.py"), "--quick"], cwd=root, capture_output=True, text=True)
if r.returncode != 0:
    print(json.dumps({"hookSpecificOutput": {"hookEventName": "Stop",
          "additionalContext": "tools/check_repo.py --quick failed. Fix these or tell the user why not:\n" + r.stdout[-3000:]}}))
