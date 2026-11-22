#!/usr/bin/env python3
"""
Generates Cozy Island Storybook 9-slice UI PNG textures and updates 9-slice
borders in ui_card.png.meta and ui_slot.png.meta.
"""
from __future__ import annotations

import re
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
UI_DIR = ROOT / "Assets/Resources/Art/UI"
SS = 4  # 4x supersampling for crisp anti-aliased edges


def new_ss(w: int, h: int) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def downsample(img: Image.Image, w: int, h: int) -> Image.Image:
    return img.resize((w, h), Image.Resampling.LANCZOS)


def make_pill_button(w: int, h: int, gold: bool = False) -> Image.Image:
    img, d = new_ss(w, h)
    W, H = w * SS, h * SS
    r = (h // 2 - 2) * SS
    pad = 2 * SS

    outer = (92, 58, 24, 255) if gold else (58, 42, 34, 235)
    lip = (208, 138, 24, 255) if gold else (210, 204, 194, 255)
    body_top = (255, 224, 112, 255) if gold else (255, 255, 255, 255)
    body_bot = (255, 188, 52, 255) if gold else (244, 240, 233, 255)
    gloss = (255, 250, 220, 150) if gold else (255, 255, 255, 165)

    # Outer dark contour
    d.rounded_rectangle([pad, pad, W - pad - 1, H - pad - 1], radius=r, fill=outer)

    # 3D bottom lip fill
    bw = 2 * SS
    d.rounded_rectangle(
        [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - 1],
        radius=max(4, r - bw),
        fill=lip,
    )

    # Main button face (leaves a 4.5px bottom 3D lip)
    lip_h = int(4.5 * SS)
    face_box = [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - lip_h - 1]
    d.rounded_rectangle(face_box, radius=max(4, r - bw), fill=body_bot)

    # Upper half warm highlight blend
    mid_y = (face_box[1] + face_box[3]) // 2 + 2 * SS
    d.rounded_rectangle(
        [face_box[0], face_box[1], face_box[2], mid_y],
        radius=max(4, r - bw),
        fill=body_top,
    )

    # Top inner specular gloss pill
    g_pad_x = 12 * SS
    g_top = face_box[1] + 3 * SS
    g_bot = g_top + 7 * SS
    d.rounded_rectangle(
        [face_box[0] + g_pad_x, g_top, face_box[2] - g_pad_x, g_bot],
        radius=3 * SS,
        fill=gloss,
    )
    return downsample(img, w, h)


def make_card(w: int, h: int) -> Image.Image:
    """Cozy storybook card (128x128, 9-slice border 20)."""
    img, d = new_ss(w, h)
    W, H = w * SS, h * SS
    r = 18 * SS
    pad = 2 * SS
    bw = 2 * SS

    # Warm cocoa contour (semi-transparent so tinting stays natural)
    d.rounded_rectangle([pad, pad, W - pad - 1, H - pad - 1], radius=r, fill=(88, 66, 52, 220))
    # Subtle lower bevel
    d.rounded_rectangle(
        [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - 1],
        radius=r - bw,
        fill=(228, 220, 208, 255),
    )
    # Neutral white tintable card face
    lip = 3 * SS
    d.rounded_rectangle(
        [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - lip - 1],
        radius=r - bw,
        fill=(255, 255, 255, 255),
    )
    # Soft inner highlight rim
    in_pad = pad + bw + 3 * SS
    d.rounded_rectangle(
        [in_pad, in_pad, W - in_pad - 1, H - in_pad - lip - 1],
        radius=max(4, r - 5 * SS),
        outline=(255, 255, 255, 190),
        width=2 * SS,
    )
    return downsample(img, w, h)


def make_panel(w: int, h: int) -> Image.Image:
    """Storybook double-framed dialog/panel (128x128, 9-slice border 22)."""
    img, d = new_ss(w, h)
    W, H = w * SS, h * SS
    r = 20 * SS
    pad = 2 * SS
    bw = 2 * SS

    # Outer warm contour
    d.rounded_rectangle([pad, pad, W - pad - 1, H - pad - 1], radius=r, fill=(76, 54, 42, 235))
    # Lower rim depth
    d.rounded_rectangle(
        [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - 1],
        radius=r - bw,
        fill=(224, 214, 198, 255),
    )
    # Main neutral white face (tints cleanly with UIKit colors)
    lip = 4 * SS
    face = [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - lip - 1]
    d.rounded_rectangle(face, radius=r - bw, fill=(255, 255, 255, 255))

    # Inner storybook frame inset line
    inset = pad + bw + 5 * SS
    d.rounded_rectangle(
        [inset, inset, W - inset - 1, H - inset - lip - 1],
        radius=max(4, r - 7 * SS),
        outline=(218, 204, 184, 210),
        width=int(1.5 * SS),
    )
    return downsample(img, w, h)


def make_slot(w: int, h: int) -> Image.Image:
    """Recessed target socket/well (128x128, 9-slice border 20)."""
    img, d = new_ss(w, h)
    W, H = w * SS, h * SS
    r = 18 * SS
    pad = 2 * SS
    bw = 2 * SS

    # Outer highlight rim (bottom bright, top dark for concave socket feel)
    d.rounded_rectangle([pad, pad, W - pad - 1, H - pad - 1], radius=r, fill=(170, 156, 138, 240))
    # Inner top shadow
    d.rounded_rectangle(
        [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - 1],
        radius=r - bw,
        fill=(212, 202, 186, 255),
    )
    # Socket interior (neutral warm white so slot color tints cleanly)
    top_shadow = 3 * SS
    d.rounded_rectangle(
        [pad + bw, pad + bw + top_shadow, W - pad - bw - 1, H - pad - bw - 1],
        radius=max(4, r - bw - SS),
        fill=(248, 244, 236, 255),
    )
    # Subtle inner ring
    inset = pad + bw + 5 * SS
    d.rounded_rectangle(
        [inset, inset + SS, W - inset - 1, H - inset - 1],
        radius=max(4, r - 7 * SS),
        outline=(188, 174, 154, 180),
        width=int(1.5 * SS),
    )
    return downsample(img, w, h)


def make_bar(w: int, h: int, is_fill: bool) -> Image.Image:
    img, d = new_ss(w, h)
    W, H = w * SS, h * SS
    r = (h // 2 - 2) * SS
    pad = 2 * SS
    if not is_fill:
        d.rounded_rectangle([pad, pad, W - pad - 1, H - pad - 1], radius=r, fill=(76, 56, 44, 210))
        bw = 2 * SS
        d.rounded_rectangle(
            [pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - 1],
            radius=r - bw,
            fill=(238, 232, 220, 255),
        )
    else:
        d.rounded_rectangle([pad, pad, W - pad - 1, H - pad - 1], radius=r, fill=(255, 255, 255, 255))
        g_pad = 10 * SS
        d.rounded_rectangle(
            [pad + g_pad, pad + 3 * SS, W - pad - g_pad, pad + 9 * SS],
            radius=3 * SS,
            fill=(255, 255, 255, 190),
        )
    return downsample(img, w, h)


def make_node(w: int, h: int) -> Image.Image:
    """160x160 circular island compass medallion."""
    img, d = new_ss(w, h)
    W, H = w * SS, h * SS
    pad = 4 * SS

    # Outer dark cocoa/gold contour
    d.ellipse([pad, pad, W - pad - 1, H - pad - 1], fill=(58, 42, 32, 245))
    # Lower 3D rim
    bw = 3 * SS
    d.ellipse([pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - 1], fill=(212, 204, 192, 255))
    # Main medallion disc (white base for accent tinting)
    lip = 5 * SS
    d.ellipse([pad + bw, pad + bw, W - pad - bw - 1, H - pad - bw - lip - 1], fill=(255, 255, 255, 255))
    # Inner compass ring
    ring = pad + bw + 7 * SS
    d.ellipse(
        [ring, ring, W - ring - 1, H - ring - lip - 1],
        outline=(255, 255, 255, 195),
        width=int(2.5 * SS),
    )
    # Top crescent highlight
    c_pad = pad + bw + 16 * SS
    d.ellipse(
        [c_pad, pad + bw + 7 * SS, W - c_pad - 1, pad + bw + 34 * SS],
        fill=(255, 255, 255, 95),
    )
    return downsample(img, w, h)


def update_meta_border(meta_path: Path, border: int) -> None:
    text = meta_path.read_text(encoding="utf-8")
    new_text = re.sub(
        r"spriteBorder:\s*\{x:\s*0,\s*y:\s*0,\s*z:\s*0,\s*w:\s*0\}",
        f"spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}",
        text,
    )
    if new_text != text:
        meta_path.write_text(new_text, encoding="utf-8")
        print(f"Updated 9-slice border={border} in {meta_path.name}")


def main() -> None:
    make_pill_button(192, 64, gold=False).save(UI_DIR / "ui_btn.png")
    make_pill_button(192, 64, gold=True).save(UI_DIR / "ui_btn_gold.png")
    make_card(128, 128).save(UI_DIR / "ui_card.png")
    make_panel(128, 128).save(UI_DIR / "ui_panel.png")
    make_slot(128, 128).save(UI_DIR / "ui_slot.png")
    make_bar(192, 64, is_fill=False).save(UI_DIR / "ui_bar_bg.png")
    make_bar(192, 64, is_fill=True).save(UI_DIR / "ui_bar_fill.png")
    make_node(160, 160).save(UI_DIR / "ui_node.png")

    update_meta_border(UI_DIR / "ui_card.png.meta", 20)
    update_meta_border(UI_DIR / "ui_slot.png.meta", 20)
    print("Generated all 8 Cozy Island Storybook UI PNG skins successfully.")


if __name__ == "__main__":
    main()
