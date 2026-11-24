"""Copy the Drive folder "Assets cho UX/UI" (pre-cut 1x PNGs, 1920x1080 layout) into Unity sprites.

Usage: python tools/ui/import_uxui.py <folder with "1 - Giao dien Settings" ... "6 - Start Screen">
Needs: pip install pillow
Output: Assets/_Project/Art/UI/Resources/UI/*.png. Re-running overwrites them in place, so the .meta/GUID is kept.
Not copied: [Demo] mockups (their backgrounds include a watermarked third-party photo) and panels with baked names.
Avatars and medals stay from tools/ui/slice_kit.py (same art, higher resolution).
"""
import sys
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parents[2] / "Assets/_Project/Art/UI/Resources/UI"
MINT = (191, 255, 225, 255)

# sprite name -> path inside the Drive folder
COPY = {
    "bg_start": "6 - Start Screen/Start Screen.png",
    "logo": "6 - Start Screen/Title.png",
    "btn_exit_game": "6 - Start Screen/Asset 52.png",
    "btn_credit": "6 - Start Screen/Asset 53.png",
    "btn_settings": "6 - Start Screen/Asset 54.png",
    "btn_find_server": "6 - Start Screen/Asset 55.png",
    "btn_create_server": "6 - Start Screen/Asset 56.png",
    "console_settings": "1 - Giao diện Settings/Máy chơi game lẻ.png",
    "btn_back": "1 - Giao diện Settings/Nút Back.png",
    "btn_home": "1 - Giao diện Settings/Nút home.png",
    "btn_resume": "2 - Giao diện lúc ấn Pause/Nút Resume.png",
    "btn_next": "3 - Giao diện tổng điểm mỗi round/Nút Next.png",
    "ribbon_score": "3 - Giao diện tổng điểm mỗi round/Chỗ ghi điểm nv.png",
    "row_score": "3 - Giao diện tổng điểm mỗi round/Khung ghi điểm của người chơi.png",
    "frame_winner": "4 - Giao diện final sau khi chơi xong 5 round/Frame.png",
    "btn_next_big": "4 - Giao diện final sau khi chơi xong 5 round/nút Next.png",
    "hud_heart": "5 - HUD/Mạng chơi game.png",
    "hud_heart_lost": "5 - HUD/Mất một mạng trái tim chuyển xám.png",
    **{f"banner_round_{n}": f"3 - Giao diện tổng điểm mỗi round/Round {n}.png" for n in range(1, 6)},
}


def smear(im, src_x, x0, x1):
    """Copy one clean column over x0..x1: erases baked text on a horizontally uniform pill."""
    col = im.crop((src_x, 0, src_x + 1, im.height))
    for x in range(x0, x1 + 1):
        im.paste(col, (x, 0))
    return im


def derived(src):
    load = lambda p: Image.open(src / p).convert("RGBA")
    out = {}
    # A label-free pill for buttons without art (text is drawn by uGUI on top).
    out["btn_blank"] = smear(load("6 - Start Screen/Asset 53.png"), 55, 56, 243)
    # Clock pill without the sample "01:02".
    out["hud_timer"] = smear(load("5 - HUD/Đồng hồ đếm thời gian.png"), 100, 101, 262)
    # Weight frame without the sample avatar and "HEAVY 15KG".
    weight = load("5 - HUD/Khung hiển thị độ nặng ở góc trái.png")
    weight.paste(Image.new("RGBA", (238, 94), MINT), (8, 7))
    out["hud_weight"] = weight
    # Corner board without the sample names: keep only its translucent black shape.
    board = load("5 - HUD/Bảng xếp hạng nhỏ ở góc.png")
    alpha = board.getchannel("A").point(lambda a: min(a, 128))
    board = Image.new("RGBA", board.size, (0, 0, 0, 0))
    board.putalpha(alpha)
    out["hud_board"] = board
    # Pause console = the empty Settings console with the PAUSE title box from "Bang Pause".
    console = load("1 - Giao diện Settings/Máy chơi game lẻ.png")
    pause = load("2 - Giao diện lúc ấn Pause/Bảng Pause.png")
    pause = pause.crop(pause.getchannel("A").getbbox())
    title = (600, 15, 990, 135)
    console.paste(pause.crop(title), title[:2])
    out["console_pause"] = console
    # Closed name plate for the WINNER podiums: the score row is open on its right edge, so mirror its left end there.
    plate = load("3 - Giao diện tổng điểm mỗi round/Khung ghi điểm của người chơi.png")
    end = plate.crop((0, 0, 40, plate.height)).transpose(Image.FLIP_LEFT_RIGHT)
    plate.paste(end, (plate.width - 40, 0))
    out["plate"] = plate
    return out


def main():
    src = Path(sys.argv[1])
    OUT.mkdir(parents=True, exist_ok=True)
    for name, rel in COPY.items():
        Image.open(src / rel).convert("RGBA").save(OUT / f"{name}.png", optimize=True)
    for name, im in derived(src).items():
        im.save(OUT / f"{name}.png", optimize=True)
    print(f"wrote {len(COPY) + 6} sprites to {OUT}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
