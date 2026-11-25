"""Team Creative's lobby kit (Drive "1 - Assets UX/UI", folders 7, 8 and 9) into Unity sprites. SB-77.

Usage: python tools/ui/import_lobby_ui.py <folder that holds "7 - giao diện lobby", "8 - giao dien chon phong",
       "giao dien info nhan vat">
Needs: pip install pillow
Output: Assets/_Project/Art/UI/Resources/UI/*.png (re-running overwrites in place, so .meta/GUID are kept).

Written here (all cropped to their visible bounds, so layout fractions in LobbyConsole / MainMenu stay stable):
  console_lobby   "7 - giao diện lobby/match setup/frame match.png": lobby console, baked tab/capacity/mode/password labels.
                  The baked capacity slider is erased: a live slider is drawn on top.
  console_rooms   "8 - giao dien chon phong/frame.png": Find Server console (START MATCH label wiped: it is JOIN there).
  lobby_row       one pill of "7 - giao diện lobby/map/map.png" without its baked text (rows of slots and maps).
  card_heavy|normal|light   the three class cards of "giao dien info nhan vat/info.png" without the baked numbers
                  (they are drawn from BalanceProfile, so they can never disagree with the balance data).
Not copied: the mockups with demo names (match setup.png, Artboard 3.png) and the D-pad variants (the console art
already has the D-pad).
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[2] / "Assets/_Project/Art/UI/Resources/UI"
MINT = (191, 255, 225, 255)

LOBBY = "7 - giao diện lobby"
ROOMS = "8 - giao dien chon phong"
INFO = "giao dien info nhan vat"

# Pixel boxes in the source files.
CAPACITY_TRACK = (885, 975, 1085, 1010)   # source coords of "frame match.png"
JOIN_CIRCLE = (325, 542, 110)             # centre and radius of the label disk, in the cropped console_rooms
ROW_BOX = (668, 404, 1708, 492)           # first map row of "map.png"
ROW_CLEAN_COLUMN, ROW_TEXT_SPAN = 700, (705, 1695)
CARD_VALUES = (988, 112, 1253, 338)       # band-relative, after cropping each card to its left edge


def load(src, rel):
    return Image.open(src / rel).convert("RGBA")


def crop_visible(im):
    return im.crop(im.getchannel("A").getbbox())


def fill_from(im, box, sample):
    """Paint `box` with the colour at `sample` (a clean pixel of the same surface)."""
    ImageDraw.Draw(im).rectangle(box, fill=im.getpixel(sample))


def console_lobby(src):
    im = load(src, f"{LOBBY}/match setup/frame match.png")
    fill_from(im, CAPACITY_TRACK, (CAPACITY_TRACK[0] - 8, CAPACITY_TRACK[1] - 8))
    return crop_visible(im)


def console_rooms(src):
    """Find Server console; the baked START MATCH label is wiped (the button is JOIN here, drawn live)."""
    im = crop_visible(load(src, f"{ROOMS}/frame.png"))
    cx, cy, r = JOIN_CIRCLE
    ImageDraw.Draw(im).ellipse((cx - r, cy - r, cx + r, cy + r), fill=MINT)
    return im


def lobby_row(src):
    row = load(src, f"{LOBBY}/map/map.png").crop(ROW_BOX)
    column = row.crop((ROW_CLEAN_COLUMN - ROW_BOX[0], 0, ROW_CLEAN_COLUMN - ROW_BOX[0] + 1, row.height))
    for x in range(ROW_TEXT_SPAN[0] - ROW_BOX[0], ROW_TEXT_SPAN[1] - ROW_BOX[0]):
        row.paste(column, (x, 0))
    return row


def cards(src):
    sheet = load(src, f"{INFO}/info.png")
    alpha = sheet.getchannel("A").point(lambda v: 255 if v > 10 else 0)
    column = list(alpha.resize((1, alpha.height), Image.BOX).get_flattened_data())
    bands, start = [], None
    for y, v in enumerate(column):
        if v > 0 and start is None:
            start = y
        if v == 0 and start is not None:
            bands.append((start, y))
            start = None
    if start is not None:
        bands.append((start, len(column)))
    assert len(bands) == 3, f"expected 3 class cards, found {bands}"
    out = {}
    for name, (y0, y1) in zip(("heavy", "normal", "light"), bands):
        card = sheet.crop((0, y0, sheet.width, y1))
        left = card.getchannel("A").getbbox()[0]
        card = card.crop((left, 0, card.width, card.height))
        card = card.crop(card.getchannel("A").getbbox())
        ImageDraw.Draw(card).rectangle(CARD_VALUES, fill=MINT)
        out[f"card_{name}"] = card
    return out


def main():
    src = Path(sys.argv[1])
    OUT.mkdir(parents=True, exist_ok=True)
    sprites = {
        "console_lobby": console_lobby(src),
        "console_rooms": console_rooms(src),
        "lobby_row": lobby_row(src),
        **cards(src),
    }
    for name, im in sprites.items():
        im.save(OUT / f"{name}.png", optimize=True)
    print(f"wrote {len(sprites)} sprites to {OUT}")


if __name__ == "__main__":
    main()
