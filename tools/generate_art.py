"""Generates the game's pixel art (16x16 tiles) into src/GravityShift.Web/wwwroot/textures.

    python3 tools/generate_art.py

Everything is drawn in code from one palette, so it's reproducible and licence-free; tweak a
colour or a shape here and re-run. The PNGs are committed, so building the game doesn't need
Python. Swap any PNG for hand-made art of the same size and layout and the game picks it up.
"""
import os
from PIL import Image

T = 16  # pixels per tile
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "src", "GravityShift.Web", "wwwroot", "textures")

# Night-shift sorting office palette.
BG_DARK = (17, 19, 31)
BG_MID = (24, 27, 43)
BG_HOLE = (11, 12, 20)
WALL = (70, 78, 104)
WALL_LIGHT = (108, 118, 152)
WALL_DARK = (42, 47, 66)
WALL_SEAM = (60, 67, 90)
RIVET = (132, 142, 178)
BOX = (198, 142, 84)
BOX_LIGHT = (224, 174, 112)
BOX_DARK = (148, 98, 56)
OUTLINE = (52, 34, 22)
TAPE = (236, 214, 164)
LABEL = (240, 236, 222)
INK = (40, 30, 26)
YELLOW = (255, 214, 92)
YELLOW_DARK = (196, 150, 40)
BLACK = (28, 26, 24)
GLOW = (255, 244, 190)
RED = (222, 62, 72)
RED_DARK = (140, 30, 42)
STEEL = (178, 184, 198)
STEEL_DARK = (92, 98, 116)
BELT = (40, 82, 90)
BELT_LIGHT = (72, 134, 144)
BELT_DARK = (22, 48, 54)
LOCK = (150, 92, 232)


def new(w=T, h=T, fill=(0, 0, 0, 0)):
    return Image.new("RGBA", (w, h), fill)


def rect(img, x0, y0, x1, y1, c):
    """Fill the inclusive pixel rectangle (x0, y0)-(x1, y1)."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            img.putpixel((x, y), c if len(c) == 4 else c + (255,))


def px(img, x, y, c):
    img.putpixel((x, y), c if len(c) == 4 else c + (255,))


# ---------- walls: 16 auto-tile variants, index = open sides (up=1, right=2, down=4, left=8) ----

def wall_tile(open_sides):
    img = new(fill=WALL + (255,))
    # Subtle panel seams so large wall areas aren't flat.
    for x in range(T):
        px(img, x, 7, WALL_SEAM)
    for y in range(8, T):
        px(img, 7, y, WALL_SEAM)
    for y in range(0, 7):
        px(img, 11, y, WALL_SEAM)
    up, right, down, left = (open_sides & 1, open_sides & 2, open_sides & 4, open_sides & 8)
    # Light from the top-left: lit top/left edges, shaded bottom/right edges.
    if up:
        rect(img, 0, 0, T - 1, 1, WALL_LIGHT)
    if left:
        rect(img, 0, 0, 1, T - 1, WALL_LIGHT)
    if down:
        rect(img, 0, T - 2, T - 1, T - 1, WALL_DARK)
    if right:
        rect(img, T - 2, 0, T - 1, T - 1, WALL_DARK)
    # Rivets near exposed edges.
    if up:
        px(img, 3, 3, RIVET); px(img, 12, 3, RIVET)
    if down:
        px(img, 3, 12, RIVET); px(img, 12, 12, RIVET)
    return img


def walls():
    sheet = new(4 * T, 4 * T)
    for i in range(16):
        sheet.paste(wall_tile(i), ((i % 4) * T, (i // 4) * T))
    return sheet


# ---------- background: pegboard, two variants ----------

def background():
    sheet = new(2 * T, T)
    for v in range(2):
        tile = new(fill=BG_MID + (255,))
        for x in range(T):
            px(tile, x, T - 1, BG_DARK)
        for y in range(T):
            px(tile, T - 1, y, BG_DARK)
        holes = [(4, 4), (11, 11)] if v == 0 else [(4, 11), (11, 4)]
        for hx, hy in holes:
            rect(tile, hx, hy, hx + 1, hy + 1, BG_HOLE)
        sheet.paste(tile, (v * T, 0))
    return sheet


# ---------- parcel ----------

def parcel():
    img = new()
    rect(img, 1, 2, 14, 14, BOX)          # body
    rect(img, 1, 2, 14, 4, BOX_LIGHT)     # lid
    rect(img, 1, 13, 14, 14, BOX_DARK)    # shadowed base
    rect(img, 13, 2, 14, 14, BOX_DARK)    # shadowed side
    rect(img, 7, 2, 8, 14, TAPE)          # tape
    # outline
    for x in range(1, 15):
        px(img, x, 1, OUTLINE); px(img, x, 15, OUTLINE)
    for y in range(2, 15):
        px(img, 0, y, OUTLINE); px(img, 15, y, OUTLINE)
    # eyes (with a glint), looking slightly up: a hopeful parcel
    for ex in (4, 10):
        rect(img, ex, 6, ex + 1, 8, INK)
        px(img, ex, 6, LABEL)
    # address label
    rect(img, 3, 10, 6, 12, LABEL)
    px(img, 4, 11, INK); px(img, 5, 11, INK)
    return img


# ---------- chute (goal): glowing mail slot with hazard border ----------

def chute():
    img = new()
    for y in range(T):
        for x in range(T):
            edge = x < 2 or y < 2 or x > T - 3 or y > T - 3
            if edge:
                px(img, x, y, YELLOW if ((x + y) // 3) % 2 == 0 else BLACK)
            else:
                # glow strongest in the middle
                d = max(abs(x - 7.5), abs(y - 7.5)) / 6
                c = tuple(int(GLOW[i] * (1 - d) + YELLOW_DARK[i] * d) for i in range(3))
                px(img, x, y, c)
    return img


# ---------- shredder (hazard): steel with red teeth ----------

def shredder():
    img = new(fill=STEEL_DARK + (255,))
    rect(img, 0, 0, T - 1, 1, STEEL)
    rect(img, 0, T - 2, T - 1, T - 1, STEEL)
    for i in range(4):  # two rows of interlocking teeth
        x0 = i * 4
        for k in range(4):
            for x in range(x0 + k // 2, x0 + 4 - k // 2):
                if x < T:
                    px(img, x, 2 + k, RED if k < 3 else RED_DARK)
                    px(img, x, T - 3 - k, RED if k < 3 else RED_DARK)
    for x in range(0, T, 4):  # bolts
        px(img, x + 1, 7, STEEL); px(img, x + 3, 8, STEEL)
    return img


# ---------- conveyor belt: horizontal (index 0) and vertical (index 1) ----------

def belt_horizontal():
    img = new(fill=BELT + (255,))
    rect(img, 0, 0, T - 1, 1, BELT_LIGHT)
    rect(img, 0, T - 2, T - 1, T - 1, BELT_DARK)
    for cx in (3, 11):  # rollers
        rect(img, cx - 2, 5, cx + 2, 10, BELT_DARK)
        rect(img, cx - 1, 6, cx + 1, 9, BELT_LIGHT)
        px(img, cx, 7, BELT_DARK)
    return img


def belts():
    h = belt_horizontal()
    sheet = new(2 * T, T)
    sheet.paste(h, (0, 0))
    sheet.paste(h.rotate(90), (T, 0))
    return sheet


# ---------- gravity lock: translucent hatch that tiles seamlessly ----------

def lock():
    img = new()
    for y in range(T):
        for x in range(T):
            stripe = (x + y) % 8 < 2
            # Drawn strong; the game tints it down to a translucent zone and back up to flash.
            px(img, x, y, LOCK + ((230,) if stripe else (120,)))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    art = {
        "walls.png": walls(),
        "background.png": background(),
        "parcel.png": parcel(),
        "chute.png": chute(),
        "shredder.png": shredder(),
        "belts.png": belts(),
        "lock.png": lock(),
    }
    for name, img in art.items():
        img.save(os.path.join(OUT, name))
        print(f"wrote textures/{name} ({img.width}x{img.height})")


if __name__ == "__main__":
    main()
