"""Slice the Drive UI kit (.ai files are PDF-compatible) into PNG sprites for Unity.

Usage: python tools/ui/slice_kit.py <Buttons.ai> <icons.ai> [--list]
Needs: pip install pymupdf numpy scipy pillow
Output: Assets/_Project/Art/UI/Resources/UI/*.png (re-run overwrites; Unity writes the .meta).
"""
import sys
from pathlib import Path

import numpy as np
import pymupdf as fitz
from PIL import Image
from scipy import ndimage as ndi

SCALE = 4          # render at 4x
OUT = Path(__file__).resolve().parents[2] / "Assets/_Project/Art/UI/Resources/UI"

# name -> box (x0, y0, x1, y1) in PDF points, taken from `--list`. The crop is trimmed to alpha.
BUTTONS = {  # Buttons.ai page 0
    "btn_create_server": (22, 32, 406, 139), "btn_find_server": (22, 157, 348, 264),
    "btn_settings": (22, 282, 348, 389), "btn_credit": (22, 407, 348, 514),
    "btn_exit_game": (22, 532, 348, 639), "btn_resume": (448, 32, 772, 139),
    "btn_next": (446, 410, 680, 517), "btn_back": (448, 538, 650, 629),
    "title_settings": (814, 35, 1156, 124), "slider_track": (810, 382, 1210, 425),
    "slider_fill": (812, 428, 1206, 468), "slider_knob": (988, 484, 1016, 512),
    "toggle": (1512, 328, 1584, 368), "panel_box": (26, 912, 668, 1054),
    "medal_1": (34, 670, 182, 871), "medal_2": (217, 699, 374, 864), "medal_3": (391, 708, 532, 857),
    "avatar_0": (736, 710, 842, 816), "avatar_1": (852, 710, 958, 816), "avatar_2": (964, 710, 1070, 816),
    "avatar_3": (738, 824, 844, 930), "avatar_4": (850, 824, 956, 930), "avatar_5": (960, 824, 1066, 930),
}
ICONS = {"arrow_next": (340, 49, 528, 181), "hud_lives": (76, 241, 404, 356), "hud_timer": (63, 408, 390, 523)}  # icons.ai page 0


def render(path, remove_text=False):
    doc = fitz.open(path)
    page = doc[0]
    page.clean_contents()
    stream = page.read_contents().decode("latin1")
    w, h = page.rect.width, page.rect.height
    # hide the grey artboard background: turn its fill into a no-op path
    stream = stream.replace(f"0 0 {w:g} {h:g} re f", f"0 0 {w:g} {h:g} re n", 1)
    doc.update_stream(page.get_contents()[0], stream.encode("latin1"))
    if remove_text:  # HUD numbers are live text: the code draws them instead
        for b in page.get_text("blocks"):
            page.add_redact_annot(fitz.Rect(b[:4]))
        page.apply_redactions(images=0, graphics=0)
    pm = page.get_pixmap(matrix=fitz.Matrix(SCALE, SCALE), alpha=True)
    return np.frombuffer(pm.samples, np.uint8).reshape(pm.h, pm.w, 4).copy()


def crop(img, box):
    x0, y0, x1, y1 = (int(v * SCALE) for v in box)
    sub = img[y0:y1, x0:x1]
    ys, xs = np.where(sub[:, :, 3] > 8)
    return Image.fromarray(sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1])


def components(img):
    lab, _ = ndi.label(ndi.binary_dilation(img[:, :, 3] > 8, iterations=6))
    for i, (y, x) in enumerate(ndi.find_objects(lab), 1):
        print(i, [round(v / SCALE) for v in (x.start, y.start, x.stop, y.stop)])


def symmetric(im):
    """The kit panel is cut on the right edge: mirror its left half to get a closed 9-sliceable panel."""
    half = im.crop((0, 0, 400, im.height))
    out = Image.new("RGBA", (800, im.height))
    out.paste(half, (0, 0))
    out.paste(half.transpose(Image.FLIP_LEFT_RIGHT), (400, 0))
    return out


def main():
    buttons, icons = sys.argv[1], sys.argv[2]
    a, b = render(buttons), render(icons, remove_text=True)
    if "--list" in sys.argv:
        print("Buttons.ai"); components(a); print("icons.ai"); components(b); return
    OUT.mkdir(parents=True, exist_ok=True)
    for name, box in BUTTONS.items():
        crop(a, box).save(OUT / f"{name}.png")
    for name, box in ICONS.items():
        crop(b, box).save(OUT / f"{name}.png")
    panel = symmetric(Image.open(OUT / "panel_box.png"))
    panel.save(OUT / "panel_box.png")
    panel.save(OUT / "row_result.png")
    print("wrote", len(list(OUT.glob("*.png"))), "pngs to", OUT)


if __name__ == "__main__":
    main()
