"""Convert the Squishy Bump GDD PDF into one Markdown file per chapter/appendix.

Usage: python tools/gdd/pdf_to_md.py [pdf] [out_dir]
Needs pypdf. Uses pypdf's layout mode so table columns keep their x positions.
The output is a first pass: hand-fixes in docs/gdd/*.md are expected, so re-running
overwrites them (shortcut: one-shot bootstrap, diff before re-running).
"""
import re
import sys
from pathlib import Path

from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[2]
PDF = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/gdd/SquishyBump_GDD_EN_v1.3.pdf"
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / "docs/gdd"

HEADING = re.compile(r"^((?:\d{1,2}\.\d{1,2}(?:\.\d)?)|[A-E]\.\d)\s+(\S.{0,70})$")
CHAPTER = re.compile(r"^(?:(\d{1,2})|Appendix ([A-E])) (\S.*)$")
FIGURE = re.compile(r"^FIGURE (\d+)\s+(.*)$")
FRONT_HEADINGS = {"Document map", "At a glance", "Executive summary"}
NOISE = re.compile(r"S Q U I S H Y|^Squishy Bump GDD · v1\.3\s+\d+ / \d+$|^P A R T |^A P P E N D I C E S")


def clean(s):
    s = re.sub(r"\s+", " ", s).replace("*", r"\*").strip()
    # pypdf layout mode detaches hyphens/dashes: "take      -off", "2 –6"
    return re.sub(r"(\S) +([-–])(?=\S)", r"\1\2", s)


def join_lines(lines):
    out = ""
    for ln in (l.strip() for l in lines):
        if not ln:
            continue
        # a trailing hyphen is a compound split across lines (the PDF has no soft hyphens)
        out += ln if out.endswith("-") or not out else " " + ln
    return re.sub(r"(\S) +-$", r"\1-", out)


def split_cols(lines):
    """Column spans = runs of character positions occupied in at least one line."""
    width = max(len(l) for l in lines)
    occ = [any(i < len(l) and l[i] != " " for l in lines) for i in range(width)]
    spans, start, gap = [], None, 0
    for i, o in enumerate(occ + [False, False]):
        if o:
            if start is None:
                start = i
            gap = 0
        elif start is not None:
            gap += 1
            if gap == 2:
                spans.append((start, i - 1))
                start = None
    return spans


def parse_table(lines):
    lines = [l.rstrip() for l in lines]
    body = [l for l in lines if l.strip()]
    spans = split_cols(body)
    groups, cur = [], []
    for l in lines + [""]:
        if l.strip():
            cur.append(l)
        elif cur:
            groups.append(cur)
            cur = []
    rows = []
    for g in groups:
        cells = [[l[a:b].strip() if len(l) > a else "" for (a, b) in spans] for l in g]
        rows.append(cells)
    return rows  # list of row groups, each a list of per-line cell lists


def group_to_row(g):
    return [clean(join_lines([line[c] for line in g])) for c in range(len(g[0]))]


def page_blocks(text):
    lines = [l for l in text.split("\n") if not NOISE.search(l)]
    blocks, i = [], 0
    while i < len(lines):
        l = lines[i]
        s = l.strip()
        if not s:
            i += 1
            continue
        if s.startswith("IN THIS CHAPTER"):
            buf = [s[len("IN THIS CHAPTER"):]]
            i += 1
            while i < len(lines) and lines[i].strip() and lines[i].startswith("   "):
                buf.append(lines[i])
                i += 1
            blocks.append(("intro", clean(join_lines(buf))))
        elif s.startswith("▪"):
            buf = [s[1:]]
            i += 1
            while i < len(lines) and lines[i].startswith("      ") and lines[i].strip():
                buf.append(lines[i])
                i += 1
            blocks.append(("bullet", clean(join_lines(buf))))
        elif l.startswith(" ") and not l.startswith("  "):
            buf, blanks = [], 0
            while i < len(lines):
                t = lines[i]
                if not t.strip():
                    blanks += 1
                    if blanks >= 2:
                        break
                elif t[0] != " " or t.strip().startswith("▪"):
                    break
                else:
                    blanks = 0
                buf.append(t)
                i += 1
            blocks.append(("table", parse_table(buf)))
        elif l.startswith("  "):
            blocks.append(("formula", s))
            i += 1
        else:
            buf = [l]
            i += 1
            if FIGURE.match(s) or HEADING.match(s) or s in FRONT_HEADINGS:
                blocks.append(("line", s))
                continue
            if len(s) < 45 and not re.search(r"[.:;,]$", s) and i < len(lines) and lines[i].strip():
                blocks.append(("para", s))  # short unnumbered label ("Layout principles"), rendered bold
                continue
            while i < len(lines) and lines[i].strip() and lines[i][0] != " " and not HEADING.match(lines[i]):
                buf.append(lines[i])
                i += 1
            blocks.append(("para", clean(join_lines(buf))))
    return blocks


def table_md(groups, header=None):
    rows = [group_to_row(g) for g in groups]
    if header is None:
        # header row glued to the first data row (no blank line between them)
        g = groups[0]
        if len(g) > 1 and all(g[0]) and (all(g[1]) or len(g) > 2 and any(l[0] for l in g[1:])):
            rows = [group_to_row(g[:1]), group_to_row(g[1:])] + rows[1:]
        header = rows.pop(0)
    elif rows and all(r.startswith(h) for r, h in zip(rows[0], header)):
        stripped = [clean(r[len(h):]) for r, h in zip(rows[0], header)]
        rows[0:1] = [stripped] if any(stripped) else []
    return header, rows


def render_table(header, rows):
    esc = lambda c: c.replace("|", r"\|")
    out = ["| " + " | ".join(map(esc, header)) + " |", "|" + "---|" * len(header)]
    out += ["| " + " | ".join(map(esc, r + [""] * (len(header) - len(r)))) + " |" for r in rows]
    return "\n".join(out)


APPENDIX_SLUGS = {"A": "ai-behaviour", "B": "cutscenes", "C": "asset-ledger", "D": "change-log", "E": "playtest"}


def slug(s):
    s = re.sub(r"\b(and|the|of|a)\b", " ", s.lower())
    return "-".join(re.findall(r"[a-z0-9]+", s)[:3])


def main():
    reader = PdfReader(str(PDF))
    pages = [page_blocks(p.extract_text(extraction_mode="layout")) for p in reader.pages]
    files, cur, md = [], None, []
    last_table = None  # (header, ncols) for merging tables split across pages

    def flush():
        if cur:
            files.append((cur[0], "\n\n".join(md).strip() + "\n"))

    cur = ("00-front-matter.md", "Front matter: title, document map, at a glance and executive summary")
    md = ["# Front matter", "Squish, bump, be the last fruit standing."]
    for pi, blocks in enumerate(pages):
        for bi, (kind, val) in enumerate(blocks):
            nxt = blocks[bi + 1] if bi + 1 < len(blocks) else None
            if kind in ("para", "line") and nxt and nxt[0] == "intro" and CHAPTER.match(val):
                flush()
                num, app, title = CHAPTER.match(val).groups()
                name = f"{int(num):02d}-{slug(title)}.md" if num else f"appendix-{app.lower()}-{APPENDIX_SLUGS[app]}.md"
                cur = (name, title)
                md = [f"# {num}. {title}" if num else f"# Appendix {app}. {title}"]
                last_table = None
                continue
            if kind == "intro":
                md.append(f"> **In this chapter:** {val}")
            elif kind == "line" and HEADING.match(val):
                n, t = HEADING.match(val).groups()
                md.append(("###" if n.count(".") == 2 else "##") + f" {n} {t}")
            elif kind == "line" and val in FRONT_HEADINGS:
                md.append(f"## {val}")
            elif kind == "line":
                n, t = FIGURE.match(val).groups()
                md.append(f"*Figure {n}: {t}*")
            elif kind == "formula":
                md.append(f"```text\n{val}\n```")
            elif kind == "bullet":
                # consecutive bullets form one list
                if md and md[-1].startswith("- "):
                    md[-1] += f"\n- {val}"
                else:
                    md.append(f"- {val}")
            elif kind == "table":
                cont = bi == 0 and last_table and len(val[0][0]) == last_table[1]
                header, rows = table_md(val, last_table[0] if cont else None)
                if cont:
                    md[-1] += "\n" + render_table(header, rows).split("\n", 2)[2] if rows else ""
                else:
                    md.append(render_table(header, rows))
                last_table = (header, len(header))
                continue
            elif kind == "para":
                if bi == 0 and md and re.search(r"[a-z,]$", md[-1]) and not md[-1].startswith(("#", "|", ">", "*", "`")):
                    md[-1] += " " + val  # paragraph split across a page break
                elif len(val) < 50 and not re.search(r"[.:;,]$", val) and val[:1].isupper():
                    md.append(f"**{val}**")
                else:
                    md.append(val)
            last_table = None
    flush()

    OUT.mkdir(parents=True, exist_ok=True)
    for name, body in files:
        (OUT / name).write_text(body, encoding="utf-8")
        print(name, "-", body.splitlines()[0])


if __name__ == "__main__":
    assert clean("take      -off  2 –6") == "take-off 2–6"
    assert slug("Features and references") == "features-references"
    main()
