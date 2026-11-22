#!/usr/bin/env python3
"""
Batch 7 Crystal Storybook Complete Art Unification Pipeline.

1. Backgrounds (12 files in Assets/Resources/Art/Backgrounds/*.png):
   - Reframes all 12 backgrounds to exact 1920x1080 (16:9 landscape).
   - Applies Crystal Storybook color grading: caps harsh neon saturation,
     warms shadows toward Ink (#33272A), adds gentle top radial light,
     quiets gameplay background contrast so cards pop, adds archipelago
     sailing trails on bg_map.png, and adds teal crystal glow on bg_final.png.
   - Updates spriteSheet rect width/height to 1920x1080 in each .meta file.

2. Characters (7 files in Assets/Resources/Art/Characters/*.png):
   - Normalizes all 7 characters onto a 600x600 RGBA canvas on a shared
     baseline grid.
   - Applies a unified 3px warm Ink (#33272A) contour outline, soft top
     light sheen, and soft down-right ShadowColor drop shadow.
   - Updates spriteSheet rect width/height to 600x600 in each .meta file.

3. Objects (20 files in Assets/Resources/Art/Objects/*.png):
   - Normalizes all 20 objects onto a 256x256 RGBA canvas.
   - Applies a unified 2px warm Ink (#33272A) outline, soft top highlight,
     down-right ShadowColor drop shadow, and soft teal crystal glow on
     obj_shard, obj_gem, and obj_crystal.
   - Updates spriteSheet rect width/height to 256x256 in each .meta file.

4. UI Icons, Badges & Tokens (Assets/Resources/Art/UI/*.png):
   - Redraws all 11 ui_icon_*.png at 128x128 RGBA with unified 2px rounded
     Ink (#33272A) stroke, warm fill, and soft shadow.
   - Redraws all 4 ui_badge_*.png at 128x128 RGBA in Palette v2 subject colors.
   - Unifies ui_star, ui_star_empty, ui_check, ui_shard, ui_trophy, ui_lock,
     and ui_arrow_* with Ink (#33272A) outline + shadow, and syncs .meta
     rects and 9-slice borders.
"""
from __future__ import annotations

import math
import re
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
ART_DIR = ROOT / "Assets/Resources/Art"
BG_DIR = ART_DIR / "Backgrounds"
CHAR_DIR = ART_DIR / "Characters"
OBJ_DIR = ART_DIR / "Objects"
UI_DIR = ART_DIR / "UI"

INK = (51, 39, 42, 255)          # #33272A
PAPER = (255, 253, 247, 255)     # #FFFDF7
CREAM = (255, 247, 230, 255)     # #FFF7E6
TEAL = (20, 184, 166, 255)       # #14B8A6
ORANGE = (255, 159, 28, 255)     # #FF9F1C
YELLOW = (255, 197, 49, 255)     # #FFC531
GOLD = (255, 197, 61, 255)       # #FFC53D
PURPLE = (126, 91, 239, 255)     # #7E5BEF
GREEN = (52, 168, 83, 255)       # #34A853
CORAL = (255, 90, 90, 255)       # #FF5A5A
SKYBLUE = (142, 201, 245, 255)   # #8EC9F5
PINK = (255, 111, 165, 255)      # #FF6FA5
BROWN = (141, 110, 99, 255)      # #8D6E63
SHADOW = (59, 41, 26, 72)        # UIKit.ShadowColor


def sync_meta_rect_and_border(meta_path: Path, w: int, h: int, border: int | None = None) -> None:
    if not meta_path.exists():
        return
    txt = meta_path.read_text(encoding="utf-8")
    orig = txt
    txt = re.sub(
        r"(rect:\s*\n\s*serializedVersion:\s*2\s*\n\s*x:\s*0\s*\n\s*y:\s*0\s*\n\s*width:\s*)\d+(\s*\n\s*height:\s*)\d+",
        rf"\g<1>{w}\g<2>{h}",
        txt,
    )
    if border is not None:
        b_str = f"{{x: {border}, y: {border}, z: {border}, w: {border}}}"
        txt = re.sub(r"spriteBorder:\s*\{[^}]+\}", f"spriteBorder: {b_str}", txt)
        txt = re.sub(r"(border:\s*)\{[^}]+\}", rf"\g<1>{b_str}", txt)
    if txt != orig:
        meta_path.write_text(txt, encoding="utf-8")


def add_ink_outline_and_shadow(
    fg: Image.Image,
    outline_px: int,
    shadow_offset: tuple[int, int] = (4, 5),
    glow_rgba: tuple[int, int, int, int] | None = None,
) -> Image.Image:
    """Wraps an RGBA sprite with a soft down-right shadow, optional crystal glow, and warm Ink outline."""
    w, h = fg.size
    alpha = fg.split()[3]

    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))

    if glow_rgba is not None:
        glow_mask = alpha.filter(ImageFilter.GaussianBlur(radius=max(6, outline_px * 4)))
        glow_layer = Image.new("RGBA", (w, h), glow_rgba)
        glow_A = ImageChops.multiply(glow_mask, Image.new("L", (w, h), glow_rgba[3]))
        glow_layer.putalpha(glow_A)
        out = Image.alpha_composite(out, glow_layer)

    k = outline_px * 2 + 1
    dilated = alpha.filter(ImageFilter.MaxFilter(k)).filter(ImageFilter.SMOOTH)

    sh_mask = dilated.filter(ImageFilter.GaussianBlur(radius=max(2, outline_px)))
    sh_layer = Image.new("RGBA", (w, h), (SHADOW[0], SHADOW[1], SHADOW[2], 0))
    sh_alpha = sh_mask.point(lambda a: int(a * (SHADOW[3] / 255.0)))
    sh_shifted = Image.new("L", (w, h), 0)
    sh_shifted.paste(sh_alpha, shadow_offset)
    sh_layer.putalpha(sh_shifted)
    out = Image.alpha_composite(out, sh_layer)

    ink_layer = Image.new("RGBA", (w, h), INK)
    ink_layer.putalpha(dilated)
    out = Image.alpha_composite(out, ink_layer)

    out = Image.alpha_composite(out, fg)

    top_grad = Image.new("L", (w, h), 0)
    gd = ImageDraw.Draw(top_grad)
    for y in range(h // 2):
        val = int(28 * (1.0 - y / max(1, h / 2)))
        gd.line([(0, y), (w, y)], fill=val)
    sheen_a = ImageChops.multiply(alpha, top_grad)
    sheen = Image.new("RGBA", (w, h), (255, 253, 244, 0))
    sheen.putalpha(sheen_a)
    out = Image.alpha_composite(out, sheen)
    return out


def fit_on_canvas(src: Image.Image, target_w: int, target_h: int, pad: int, baseline_align: bool = False) -> Image.Image:
    rgba = src.convert("RGBA")
    bbox = rgba.getbbox()
    if bbox:
        rgba = rgba.crop(bbox)
    cw, ch = rgba.size
    avail_w = target_w - 2 * pad
    avail_h = target_h - 2 * pad
    scale = min(avail_w / max(1, cw), avail_h / max(1, ch))
    nw = max(1, int(round(cw * scale)))
    nh = max(1, int(round(ch * scale)))
    resized = rgba.resize((nw, nh), Image.Resampling.LANCZOS)
    if scale > 1.4:
        resized = resized.filter(ImageFilter.SMOOTH_MORE)

    canvas = Image.new("RGBA", (target_w, target_h), (0, 0, 0, 0))
    x = (target_w - nw) // 2
    y = (target_h - pad - nh) if baseline_align else (target_h - nh) // 2
    canvas.paste(resized, (x, y), resized)
    return canvas


def process_backgrounds() -> None:
    target_w, target_h = 1920, 1080

    top_light = Image.new("RGBA", (target_w, target_h), (0, 0, 0, 0))
    tl_draw = ImageDraw.Draw(top_light)
    for y in range(target_h):
        u = y / target_h
        if u < 0.55:
            a = int(34 * (1.0 - u / 0.55) ** 1.4)
            tl_draw.line([(0, y), (target_w, y)], fill=(255, 251, 236, a))
        elif u > 0.72:
            a = int(32 * ((u - 0.72) / 0.28) ** 1.5)
            tl_draw.line([(0, y), (target_w, y)], fill=(51, 39, 42, a))

    for p in sorted(BG_DIR.glob("*.png")):
        im = Image.open(p).convert("RGBA")
        sw, sh = im.size
        scale = max(target_w / sw, target_h / sh)
        nw, nh = int(round(sw * scale)), int(round(sh * scale))
        resized = im.resize((nw, nh), Image.Resampling.LANCZOS)
        left = (nw - target_w) // 2
        top = int((nh - target_h) * 0.42)
        cropped = resized.crop((left, top, left + target_w, top + target_h))

        is_gameplay_bg = p.name not in ("bg_menu.png", "bg_map.png", "bg_final.png")
        rgb = cropped.convert("RGB")
        sat_factor = 0.72 if is_gameplay_bg else 0.80
        con_factor = 0.88 if is_gameplay_bg else 0.93
        rgb = ImageEnhance.Color(rgb).enhance(sat_factor)
        rgb = ImageEnhance.Contrast(rgb).enhance(con_factor)
        out = rgb.convert("RGBA")

        if is_gameplay_bg:
            wash = Image.new("RGBA", (target_w, target_h), (255, 249, 236, 26))
            out = Image.alpha_composite(out, wash)

        out = Image.alpha_composite(out, top_light)

        if p.name == "bg_map.png":
            overlay = Image.new("RGBA", (target_w, target_h), (0, 0, 0, 0))
            od = ImageDraw.Draw(overlay)
            row_ys = [int(target_h * 0.28), int(target_h * 0.50), int(target_h * 0.72)]
            for ry in row_ys:
                for x in range(240, target_w - 220, 38):
                    wy = ry + int(10 * math.sin(x * 0.015))
                    od.ellipse([x - 5, wy - 3, x + 5, wy + 3], fill=(255, 247, 230, 105))
            out = Image.alpha_composite(out, overlay)

        if p.name == "bg_final.png":
            glow = Image.new("RGBA", (target_w, target_h), (0, 0, 0, 0))
            gd = ImageDraw.Draw(glow)
            cx, cy = target_w // 2, int(target_h * 0.26)
            for r in range(360, 10, -14):
                a = int(56 * (1.0 - r / 360.0) ** 1.8)
                gd.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(20, 184, 166, a))
            out = Image.alpha_composite(out, glow)

        out.save(p)
        sync_meta_rect_and_border(Path(str(p) + ".meta"), target_w, target_h)


def process_characters() -> None:
    for p in sorted(CHAR_DIR.glob("*.png")):
        im = Image.open(p).convert("RGBA")
        r, g, b, a = im.split()
        rgb = Image.merge("RGB", (r, g, b))
        rgb = ImageEnhance.Color(rgb).enhance(0.90)
        im = Image.merge("RGBA", (*rgb.split(), a))

        fitted = fit_on_canvas(im, 600, 600, pad=36, baseline_align=True)
        styled = add_ink_outline_and_shadow(fitted, outline_px=3, shadow_offset=(5, 6))
        styled.save(p)
        sync_meta_rect_and_border(Path(str(p) + ".meta"), 600, 600)


def process_objects() -> None:
    crystal_family = {"obj_crystal.png", "obj_gem.png", "obj_shard.png"}
    for p in sorted(OBJ_DIR.glob("*.png")):
        im = Image.open(p).convert("RGBA")
        r, g, b, a = im.split()
        rgb = Image.merge("RGB", (r, g, b))
        rgb = ImageEnhance.Color(rgb).enhance(0.92)
        im = Image.merge("RGBA", (*rgb.split(), a))

        fitted = fit_on_canvas(im, 256, 256, pad=22, baseline_align=False)
        glow = (20, 184, 166, 95) if p.name in crystal_family else None
        styled = add_ink_outline_and_shadow(fitted, outline_px=2, shadow_offset=(3, 4), glow_rgba=glow)
        styled.save(p)
        sync_meta_rect_and_border(Path(str(p) + ".meta"), 256, 256)


def draw_icon_128(kind: str) -> Image.Image:
    SS = 4
    S = 128 * SS
    fg = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(fg)
    C = S // 2

    r_disc = 44 * SS
    d.ellipse([C - r_disc, C - r_disc, C + r_disc, C + r_disc], fill=PAPER, outline=INK, width=3 * SS)

    sw = 5 * SS
    if kind == "play":
        pts = [(C - 12 * SS, C - 20 * SS), (C - 12 * SS, C + 20 * SS), (C + 22 * SS, C)]
        d.polygon(pts, fill=TEAL, outline=INK)
        d.line(pts + [pts[0]], fill=INK, width=3 * SS, joint="curve")
    elif kind == "next":
        pts = [(C - 16 * SS, C - 18 * SS), (C + 8 * SS, C), (C - 16 * SS, C + 18 * SS)]
        d.line(pts, fill=INK, width=sw, joint="curve")
        d.line([(C + 16 * SS, C - 18 * SS), (C + 16 * SS, C + 18 * SS)], fill=TEAL, width=sw)
    elif kind == "pause":
        d.rounded_rectangle([C - 18 * SS, C - 18 * SS, C - 5 * SS, C + 18 * SS], radius=4 * SS, fill=PURPLE, outline=INK, width=3 * SS)
        d.rounded_rectangle([C + 5 * SS, C - 18 * SS, C + 18 * SS, C + 18 * SS], radius=4 * SS, fill=PURPLE, outline=INK, width=3 * SS)
    elif kind == "close":
        d.line([(C - 16 * SS, C - 16 * SS), (C + 16 * SS, C + 16 * SS)], fill=CORAL, width=sw + 2 * SS)
        d.line([(C - 16 * SS, C + 16 * SS), (C + 16 * SS, C - 16 * SS)], fill=CORAL, width=sw + 2 * SS)
        d.line([(C - 16 * SS, C - 16 * SS), (C + 16 * SS, C + 16 * SS)], fill=INK, width=3 * SS)
        d.line([(C - 16 * SS, C + 16 * SS), (C + 16 * SS, C - 16 * SS)], fill=INK, width=3 * SS)
    elif kind == "home":
        roof = [(C - 24 * SS, C - 2 * SS), (C, C - 24 * SS), (C + 24 * SS, C - 2 * SS)]
        d.rounded_rectangle([C - 16 * SS, C - 4 * SS, C + 16 * SS, C + 22 * SS], radius=4 * SS, fill=CREAM, outline=INK, width=3 * SS)
        d.polygon(roof, fill=CORAL, outline=INK)
        d.line(roof + [roof[0]], fill=INK, width=3 * SS, joint="curve")
        d.rounded_rectangle([C - 6 * SS, C + 6 * SS, C + 6 * SS, C + 22 * SS], radius=2 * SS, fill=BROWN, outline=INK, width=2 * SS)
    elif kind == "hint":
        d.ellipse([C - 16 * SS, C - 22 * SS, C + 16 * SS, C + 8 * SS], fill=YELLOW, outline=INK, width=3 * SS)
        d.rounded_rectangle([C - 8 * SS, C + 6 * SS, C + 8 * SS, C + 20 * SS], radius=3 * SS, fill=BROWN, outline=INK, width=3 * SS)
    elif kind == "repeat":
        d.arc([C - 20 * SS, C - 20 * SS, C + 20 * SS, C + 20 * SS], start=35, end=315, fill=INK, width=sw)
        pts = [(C + 10 * SS, C - 22 * SS), (C + 24 * SS, C - 18 * SS), (C + 18 * SS, C - 6 * SS)]
        d.polygon(pts, fill=TEAL, outline=INK)
    elif kind == "audio":
        spk = [(C - 18 * SS, C - 8 * SS), (C - 8 * SS, C - 8 * SS), (C + 4 * SS, C - 18 * SS), (C + 4 * SS, C + 18 * SS), (C - 8 * SS, C + 8 * SS), (C - 18 * SS, C + 8 * SS)]
        d.polygon(spk, fill=TEAL, outline=INK)
        d.line(spk + [spk[0]], fill=INK, width=3 * SS)
        d.arc([C - 2 * SS, C - 14 * SS, C + 16 * SS, C + 14 * SS], start=-55, end=55, fill=INK, width=3 * SS)
        d.arc([C - 4 * SS, C - 22 * SS, C + 24 * SS, C + 22 * SS], start=-55, end=55, fill=INK, width=3 * SS)
    elif kind == "map":
        d.ellipse([C - 20 * SS, C - 20 * SS, C + 20 * SS, C + 20 * SS], fill=SKYBLUE, outline=INK, width=3 * SS)
        needle = [(C, C - 16 * SS), (C + 7 * SS, C), (C, C + 16 * SS), (C - 7 * SS, C)]
        d.polygon(needle, fill=ORANGE, outline=INK)
        d.line(needle + [needle[0]], fill=INK, width=2 * SS)
    elif kind == "report":
        d.rounded_rectangle([C - 18 * SS, C - 20 * SS, C + 18 * SS, C + 20 * SS], radius=4 * SS, fill=CREAM, outline=INK, width=3 * SS)
        d.rounded_rectangle([C - 12 * SS, C + 2 * SS, C - 5 * SS, C + 14 * SS], radius=2 * SS, fill=ORANGE, outline=INK, width=2 * SS)
        d.rounded_rectangle([C - 3 * SS, C - 6 * SS, C + 4 * SS, C + 14 * SS], radius=2 * SS, fill=TEAL, outline=INK, width=2 * SS)
        d.rounded_rectangle([C + 6 * SS, C - 12 * SS, C + 13 * SS, C + 14 * SS], radius=2 * SS, fill=GREEN, outline=INK, width=2 * SS)
    elif kind == "settings":
        for a_deg in range(0, 360, 45):
            rad = math.radians(a_deg)
            gx = C + int(20 * SS * math.cos(rad))
            gy = C + int(20 * SS * math.sin(rad))
            d.ellipse([gx - 6 * SS, gy - 6 * SS, gx + 6 * SS, gy + 6 * SS], fill=ORANGE, outline=INK, width=2 * SS)
        d.ellipse([C - 17 * SS, C - 17 * SS, C + 17 * SS, C + 17 * SS], fill=GOLD, outline=INK, width=3 * SS)
        d.ellipse([C - 7 * SS, C - 7 * SS, C + 7 * SS, C + 7 * SS], fill=PAPER, outline=INK, width=3 * SS)

    icon128 = fg.resize((128, 128), Image.Resampling.LANCZOS)
    return add_ink_outline_and_shadow(icon128, outline_px=1, shadow_offset=(2, 3))


def draw_badge_128(fill_col: tuple[int, int, int, int]) -> Image.Image:
    SS = 4
    S = 128 * SS
    fg = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(fg)
    C = S // 2
    pts = [
        (C, C - 44 * SS),
        (C + 36 * SS, C - 24 * SS),
        (C + 30 * SS, C + 22 * SS),
        (C, C + 44 * SS),
        (C - 30 * SS, C + 22 * SS),
        (C - 36 * SS, C - 24 * SS),
    ]
    d.polygon(pts, fill=fill_col)
    d.line(pts + [pts[0]], fill=INK, width=4 * SS, joint="curve")
    s_pts = []
    for i in range(10):
        ang = math.radians(-90 + i * 36)
        r = (20 if i % 2 == 0 else 9) * SS
        s_pts.append((C + int(r * math.cos(ang)), C + int(r * math.sin(ang))))
    d.polygon(s_pts, fill=GOLD)
    d.line(s_pts + [s_pts[0]], fill=INK, width=2 * SS)
    b128 = fg.resize((128, 128), Image.Resampling.LANCZOS)
    return add_ink_outline_and_shadow(b128, outline_px=1, shadow_offset=(2, 3))


def process_ui_icons_and_badges() -> None:
    icons = ["audio", "close", "hint", "home", "map", "next", "pause", "play", "repeat", "report", "settings"]
    for k in icons:
        p = UI_DIR / f"ui_icon_{k}.png"
        draw_icon_128(k).save(p)
        sync_meta_rect_and_border(Path(str(p) + ".meta"), 128, 128)

    badges = {
        "ui_badge_hero.png": GOLD,
        "ui_badge_math.png": ORANGE,
        "ui_badge_puzzle.png": PURPLE,
        "ui_badge_word.png": TEAL,
    }
    for fname, col in badges.items():
        p = UI_DIR / fname
        draw_badge_128(col).save(p)
        sync_meta_rect_and_border(Path(str(p) + ".meta"), 128, 128)

    tokens = [
        ("ui_shard.png", (20, 184, 166, 90)),
        ("ui_trophy.png", None),
        ("ui_lock.png", None),
        ("ui_check.png", None),
        ("ui_star.png", None),
        ("ui_star_empty.png", None),
        ("ui_arrow_up.png", None),
        ("ui_arrow_down.png", None),
        ("ui_arrow_left.png", None),
        ("ui_arrow_right.png", None),
    ]
    for fname, glow in tokens:
        p = UI_DIR / fname
        if not p.exists():
            continue
        im = Image.open(p).convert("RGBA")
        w, h = im.size
        fitted = fit_on_canvas(im, w, h, pad=max(4, min(w, h) // 10))
        styled = add_ink_outline_and_shadow(fitted, outline_px=1, shadow_offset=(2, 2), glow_rgba=glow)
        styled.save(p)
        sync_meta_rect_and_border(Path(str(p) + ".meta"), w, h)

    sync_meta_rect_and_border(UI_DIR / "ui_card.png.meta", 128, 128, border=20)
    sync_meta_rect_and_border(UI_DIR / "ui_slot.png.meta", 128, 128, border=20)
    sync_meta_rect_and_border(UI_DIR / "ui_panel.png.meta", 128, 128, border=22)
    sync_meta_rect_and_border(UI_DIR / "ui_btn.png.meta", 192, 64, border=16)
    sync_meta_rect_and_border(UI_DIR / "ui_btn_gold.png.meta", 192, 64, border=16)
    sync_meta_rect_and_border(UI_DIR / "ui_bar_bg.png.meta", 192, 64, border=16)
    sync_meta_rect_and_border(UI_DIR / "ui_bar_fill.png.meta", 192, 64, border=16)


def main() -> None:
    process_backgrounds()
    print("Processed all 12 backgrounds to 1920x1080 Crystal Storybook style.")
    process_characters()
    print("Processed all 7 characters to 600x600 Crystal Storybook style.")
    process_objects()
    print("Processed all 20 objects to 256x256 Crystal Storybook style.")
    process_ui_icons_and_badges()
    print("Processed all 11 UI icons (128x128), 4 badges (128x128), and UI tokens.")


if __name__ == "__main__":
    main()
