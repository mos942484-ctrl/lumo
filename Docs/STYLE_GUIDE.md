# Crystal Storybook Visual & Audio Style Guide — Lumo and the Crystal of Knowledge

All art, UI, typography, and audio in **Lumo and the Crystal of Knowledge** follow a unified **Crystal Storybook** visual language designed for clarity, warmth, and accessibility for ages **5–12**.

---

## 1. Color Palette (`UIKit.cs`)

| Token Name | Hex Code | Usage |
|---|---|---|
| **DeepNavy** | `#182B49` | Header plaques, top HUD bar, modal headers |
| **Parchment** | `#FDF6E4` | Main storybook cards, dialogue panels, puzzle stages |
| **Cream** | `#FFF9EC` | Inner card wells, unselected toggle buttons, subtitle text |
| **SoftGold** | `#F9E4A0` | Character halos, active toggles, summary strips, age-band pills |
| **Gold** | `#FFC53D` | Primary display titles, earned stars, trophies, selected borders |
| **Orange** | `#FF9A2E` | Primary Call-to-Action buttons (`Play`, `Let's Start!`, `Continue`) |
| **Teal** | `#1CA7A0` | Secondary navigation buttons (`Main Menu`, `Play Again`, `Close`) |
| **Green** | `#34A853` | Positive confirmation (`I'm ready!`, `So fun!`, matched cards, correct banner) |
| **Coral** | `#F26A5A` | Back/Home navigation buttons |
| **Purple** | `#7E5BEF` | Settings button, reflective choices (`Tricky but I did it!`), Science Lab accent |
| **WarmWood** | `#734F36` | Parents Area button, Skip button |
| **Ink** | `#281E18` | Primary body copy and universal storybook outline stroke |

---

## 2. Typography & Readability Standards

- **Primary Font:** **Baloo 2 Bold** (`Assets/Resources/Fonts/font_main.ttf`, SIL Open Font License 1.1), baked to a TextMeshPro SDF asset (`font_main.asset`).
- **Display Titles (`UIKit.StyleTitle`):** Gold (`#FFC53D`) or Cream (`#FFF9EC`) fill with a `0.22` warm-ink (`#281E18`) SDF outline and soft drop underlay so headers remain legible over any illustrated backdrop.
- **Body & Card Copy:** Dark warm ink (`#281E18`) on high-contrast Parchment (`#FDF6E4`) or Cream (`#FFF9EC`) cards, with `enableAutoSizing` enabled on multi-line containers to prevent clipping across aspect ratios.

---

## 3. Unified Asset Specifications & 1:1 Swap Guide

Every runtime asset is loaded by fixed filename from `Assets/Resources/` via [`GameAssets.cs`](file:///d:/Unity/lumooo/Assets/Scripts/Core/GameAssets.cs). Replacing any file at the same path swaps it into the game with **zero code changes** (run **`Lumo → Setup → Fix Sprite Imports`** after adding new PNGs outside Unity).

### 3.1 Texture Dimension Standards (Enforced by `LumoTests.Phase3Checks`)

| Category | Directory | Standard Resolution | Count | Format & Rules |
|---|---|---|---|---|
| **Backgrounds** | `Assets/Resources/Art/Backgrounds/` | `1920 × 1080 px` | `12` | Full-HD landscape PNG (`bg_menu`, `bg_kingdom`, `bg_village`, `bg_garden`, `bg_bridge`, `bg_castle`, `bg_library`, `bg_lab`, `bg_mountain`, `bg_map`, `bg_workshop`, `bg_final`). |
| **Characters** | `Assets/Resources/Art/Characters/` | `600 × 600 px` | `7` | Transparent PNG with `#281E18` ink outline and soft ground shadow (`char_lumo`, `char_king`, `char_lexi`, `char_hero_a`, `char_hero_b`, `char_shadow`, `char_spark`). |
| **Objects** | `Assets/Resources/Art/Objects/` | `256 × 256 px` | `20` | Transparent PNG (`obj_apple`, `obj_banana`, `obj_cat`, `obj_dog`, `obj_bird`, `obj_fish`, `obj_sun`, `obj_star`, `obj_gem`, `obj_key`, `obj_ball`, `obj_tree`, `obj_house`, `obj_chest`, `obj_cauldron`, `obj_crystal`, `obj_shard`, `obj_basket`, `obj_coin`, `obj_crown`). Note: `obj_star`, `obj_gem`, `obj_ball`, `obj_tree`, and `obj_house` use light/neutral bases so code color-tinting reads cleanly. |
| **UI Panels & Pills** | `Assets/Resources/Art/UI/` | `192×64` / `128×128` / `160×160` | `8` | White-base tintable 9-slice sprites (`ui_btn`, `ui_btn_gold`, `ui_bar_bg`, `ui_bar_fill` at `192×64`, border `16`; `ui_panel` at `128×128`, border `22`; `ui_card` at `128×128`, border `32`; `ui_node` at `160×160`). |
| **UI Icons & Badges** | `Assets/Resources/Art/UI/` | `128×128` / `100×100` | `25` | Transparent PNG icons (`ui_icon_*`, `ui_badge_*` at `128×128`; `ui_arrow_*`, `ui_check`, `ui_lock`, `ui_trophy` at `100×100`). |

### 3.2 Audio & Voice-Over Pipeline (`Assets/Resources/Audio/`)
- **Music (`Audio/Music/`):** `music_menu.wav`, `music_game.ogg`, `music_victory.wav` (CC0 loops/fanfare).
- **Sound Effects (`Audio/SFX/`):** `sfx_click`, `sfx_correct`, `sfx_wrong`, `sfx_timer`, `sfx_collect`, `sfx_drop`, `sfx_star`, `sfx_complete` (CC0 Kenney interface/jingle sounds).
- **Voice-Over (`Audio/Voice/`):** `663` offline WAV clips (`16 kHz`, 16-bit mono) covering all level intros, core prompts, adaptive variant prompts, words, sentences, and positive/retry feedback lines. Audited via `python Tools/audit_vo.py`.

---

## 4. License & Attribution Summary

- **Code & Curriculum (`levels.json`):** Original work created for **Lumo and the Crystal of Knowledge**.
- **Typography:** *Baloo 2 Bold* — SIL Open Font License 1.1 (`Assets/Resources/Fonts/OFL-Baloo2.txt`).
- **Sprites, UI Skin, SFX, & Music:** CC0 1.0 Public Domain (Kenney.nl, OpenGameArt CC0 packs, FreeSVG CC0 vectors, and project-generated vector sprites via `Tools/gen_ui_skin.py` and `Tools/gen_crystal_storybook_art.py`).
