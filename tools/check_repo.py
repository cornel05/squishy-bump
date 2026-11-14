#!/usr/bin/env python3
"""Repo guard rails for Squishy Bump. Exit 1 on any failure.

Usage: python tools/check_repo.py [--quick]
  --quick  skip the full-history LFS size scan (only staged files); runs in < 2 s.
"""
import json
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPTS = "Assets/_Project/Scripts"
FORBIDDEN = re.compile(r"^(Library|Temp|Logs|UserSettings|Builds|Build|obj|TestResults)/|\.(csproj|sln)$|(^|/)settings\.local\.json$|(^|/)CLAUDE\.local\.md$")
FREE_ASMDEFS = {"SquishyBump.App", "SquishyBump.Editor", "SquishyBump.Tests.EditMode", "SquishyBump.Tests.PlayMode"}
MAX_BIN = 1024 * 1024
errors: list[str] = []


def git(*args: str, stdin: str | None = None) -> list[str]:
    out = subprocess.run(["git", *args], cwd=ROOT, input=stdin, capture_output=True, text=True, encoding="utf-8", check=True).stdout
    return [line for line in out.splitlines() if line]


def unity_visible(path: str) -> bool:
    # Unity skips dotfiles/dirs and names ending in '~' when importing.
    return not any(p.startswith(".") or p.endswith("~") for p in path.split("/"))


def check_metas(files: list[str]) -> None:
    assets = [f for f in files if f.startswith("Assets/") and unity_visible(f)]
    metas = {f[:-5] for f in assets if f.endswith(".meta")}
    items = {f for f in assets if not f.endswith(".meta")}
    for f in list(items):  # every parent folder below Assets/ needs a meta too
        parts = f.split("/")
        items.update("/".join(parts[:i]) for i in range(2, len(parts)))
    items = {i for i in items if (ROOT / i).exists()}
    for i in sorted(items - metas):
        errors.append(f"missing .meta: {i}  (open the project in Unity to generate it; never hand-write GUIDs)")
    for m in sorted(metas - items):
        if not (ROOT / m).exists():
            errors.append(f"orphan .meta: {m}.meta  (delete it, or restore the asset)")


def allowed_exceptions() -> set[tuple[str, str]]:
    # An ADR may allow one module->module reference with a line: "Allowed-Reference: A -> B"
    out = set()
    for adr in (ROOT / "docs/decisions").glob("*.md"):
        for a, b in re.findall(r"Allowed-Reference:\s*(\S+)\s*->\s*(\S+)", adr.read_text(encoding="utf-8")):
            out.add((a, b))
    return out


def check_asmdefs() -> None:
    allowed = allowed_exceptions()
    for path in (ROOT / "Assets").rglob("*.asmdef"):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        name = data.get("name", "")
        if not name.startswith("SquishyBump."):
            continue
        if data.get("rootNamespace") != name:
            errors.append(f"{path.relative_to(ROOT)}: rootNamespace must equal the assembly name '{name}'")
        if name in FREE_ASMDEFS:
            continue
        for ref in data.get("references", []):
            if ref.startswith("GUID:"):
                errors.append(f"{name}: reference by name, not GUID ({ref})")
            elif ref.startswith("SquishyBump.") and (name == "SquishyBump.Core" or ref != "SquishyBump.Core") and (name, ref) not in allowed:
                errors.append(f"{name} -> {ref}: modules may reference only SquishyBump.Core (ADR-0003; exceptions need an ADR)")


def check_forbidden(tracked: list[str]) -> None:
    for f in tracked:
        if FORBIDDEN.search(f):
            errors.append(f"forbidden path tracked by git: {f}")


def check_lfs(candidates: list[str]) -> None:
    big = [f for f in candidates if (ROOT / f).is_file() and (ROOT / f).stat().st_size > MAX_BIN]
    if not big:
        return
    for line in git("check-attr", "filter", "--stdin", stdin="\n".join(big) + "\n"):
        path, _, value = line.rsplit(": ", 2)
        if value != "lfs":
            errors.append(f"binary > 1 MB not in LFS: {path}  (add its extension to .gitattributes)")


def check_agent_docs() -> None:
    lines = len((ROOT / "AGENTS.md").read_text(encoding="utf-8").splitlines()) if (ROOT / "AGENTS.md").exists() else 0
    if not lines:
        errors.append("AGENTS.md missing at repo root")
    elif lines > 150:
        errors.append(f"AGENTS.md has {lines} lines (max 150): move detail to module AGENTS.md or docs/")
    for asm in (ROOT / SCRIPTS).glob("*/*.asmdef"):
        d = asm.parent
        for f in ("AGENTS.md", "CLAUDE.md"):
            if not (d / f).exists():
                errors.append(f"module {d.name} is missing {f}")
        if (d / "AGENTS.md").exists() and len((d / "AGENTS.md").read_text(encoding="utf-8").splitlines()) > 40:
            errors.append(f"{d.name}/AGENTS.md is over 40 lines")
    for skill in (ROOT / ".agents/skills").glob("*/SKILL.md"):
        text = skill.read_text(encoding="utf-8")
        if len(text.splitlines()) > 60:
            errors.append(f"{skill.relative_to(ROOT)} is over 60 lines")
        if not re.search(r"^description:\s*\S", text, re.M):
            errors.append(f"{skill.relative_to(ROOT)} has no description frontmatter")
        if not (ROOT / ".claude/skills" / skill.parent.name / "SKILL.md").exists():
            errors.append(f"missing Claude wrapper .claude/skills/{skill.parent.name}/SKILL.md")
    for third in (ROOT / "Assets/ThirdParty").iterdir() if (ROOT / "Assets/ThirdParty").exists() else []:
        if third.is_dir() and not any(third.glob(p) for p in ("LICENSE*", "CREDITS*")):
            errors.append(f"Assets/ThirdParty/{third.name} has no LICENSE*/CREDITS* file")


def main() -> int:
    quick = "--quick" in sys.argv
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # Windows consoles default to cp1252
    tracked = git("ls-files")
    working = sorted(set(tracked) | set(git("ls-files", "--others", "--exclude-standard")))
    check_metas([f for f in working if (ROOT / f).exists() or f.endswith(".meta")])
    check_asmdefs()
    check_forbidden(git("ls-files", "--cached"))
    check_lfs(git("diff", "--cached", "--name-only", "--diff-filter=AM") if quick else tracked)
    check_agent_docs()
    for e in errors:
        print(f"FAIL {e}")
    print(f"check_repo{' --quick' if quick else ''}: {'FAIL' if errors else 'PASS'} ({len(errors)} problems)")
    return 1 if errors else 0


if __name__ == "__main__":
    os.chdir(ROOT)
    sys.exit(main())
