# Lumo and the Crystal of Knowledge

**Lumo and the Crystal of Knowledge** is an offline-first, self-contained 2D educational adventure game built in **Unity 6 (`6000.6.3f1`)** for children aged **5–12**. Guided by **Lumo the Owl**, **King Arqam**, and **Princess Lexi**, young heroes sail across the **13 Wonder Islands** to recover the **12 shattered shards of the Crystal of Knowledge** and restore harmony to the kingdom.

---

## Documentation Index

All official project documentation lives in the [`Docs/`](file:///d:/Unity/lumooo/Docs) folder:

| Document | Purpose |
|---|---|
| [**Docs/STORYBOARD.md**](file:///d:/Unity/lumooo/Docs/STORYBOARD.md) | **Master Visual & Narrative Storyboard** — Cast profiles, 3-act narrative arc, 23 frame-by-frame screen wireframes, all 13 island chapter scripts, voice-over cues, and flowcharts. |
| [**Docs/GAME_DESIGN_DOCUMENT.md**](file:///d:/Unity/lumooo/Docs/GAME_DESIGN_DOCUMENT.md) | **Game Design Document (GDD)** — Core loop, all 12 puzzle module specifications, star/XP/badge economy, Timed vs. Calm modes, and UI screen specifications. |
| [**Docs/LEARNING_DESIGN_AND_CURRICULUM.md**](file:///d:/Unity/lumooo/Docs/LEARNING_DESIGN_AND_CURRICULUM.md) | **Learning Experience Design (LXD) & Curriculum** — 13 skills, 30 Intended Learning Outcomes (ILOs), 87 core + 110 adaptive variant challenges, age-band routing (`5–7`, `8–10`, `11–12`), mastery engine, and Parent Co-Play tools. |
| [**Docs/TECHNICAL_ARCHITECTURE.md**](file:///d:/Unity/lumooo/Docs/TECHNICAL_ARCHITECTURE.md) | **Technical & Engineering Architecture** — Zero-prefab procedural uGUI system (`GameRoot`, `UIManager`, `UIKit`), core managers, JSON schema, save migration (`v4`), and automated QA suites. |
| [**Docs/STYLE_GUIDE.md**](file:///d:/Unity/lumooo/Docs/STYLE_GUIDE.md) | **Crystal Storybook Visual & Audio Style Guide** — Color palette, typography (`Baloo 2 Bold`), 9-slice UI skin, texture dimension rules, 1:1 asset replacement guide, and CC0/OFL credits. |

> **Archived Pre-Production Docs:** Legacy build prompts and working notes have been moved to [`_old_docs/`](file:///d:/Unity/lumooo/_old_docs) and can be safely deleted at any time.

---

## Key Features at a Glance

- **13 Themed Wonder Islands:** 12 core islands (6 rounds each = 72 challenges) + the 15-round **Music Room — Crystal Finale** (87 core challenges + 110 adaptive easier/harder variants = **197 total challenges**).
- **12 Interactive Puzzle Modules:** `math-choice`, `word-picture`, `matching`, `spelling`, `potion`, `directions`, `sentence`, `sorting`, `choice-card`, `memory-match`, `pixel-art`, and `music-play`.
- **13 Tracked Curriculum Skills:** `addition`, `subtraction`, `twoStep`, `counting`, `vocabulary`, `spelling`, `sentence`, `directions`, `kindness`, `science`, `memory`, `art`, and `music`.
- **3 Cognitive Age Bands:**
  - **Ages 5–7 (Concrete & Visual):** Manipulative-first counting, smaller numbers, phonics, and visual matching.
  - **Ages 8–10 (Conceptual & Guided):** Place value, synonyms/rhymes, life cycles, habitats, and multi-step arithmetic.
  - **Ages 11–12 (Deductive & Multi-step):** Logic clues, elapsed time, fossils, physics/astronomy, and symmetry.
- **Calm / Untimed Mode & Practice Mode:** Default anxiety-free untimed play with accuracy-based stars (`1–3 ★`), optional 30-second Timed Mode (gated by first-try accuracy), and ungraded Practice Mode.
- **PIN-Protected Parents Area:** Live skill & ILO mastery breakdown, co-play scaffolding prompts (*While Playing* / *After Playing*), *My Words* 11-word picture book, daily time limits (`15 / 30 / 45 min`), and CSV session log export.

---

## Quick Start (Unity Editor)

1. Open the repository root (`d:\Unity\lumooo`) in **Unity 6 (`6000.6.3f1`)**.
2. Open the main scene: [`Assets/Scenes/LumoMain.unity`](file:///d:/Unity/lumooo/Assets/Scenes/LumoMain.unity).
3. Press **Play** in the Unity Editor. `GameRoot` automatically bootstraps all core managers, loads [`Assets/Resources/Data/levels.json`](file:///d:/Unity/lumooo/Assets/Resources/Data/levels.json), and opens the **Splash Screen**.

### Running Automated Verification

- **Editor Test Suite:** Run **`Lumo → Tests → Run All Day1`** from the Unity top menu bar to execute all 7 validation suites (`ValidateData`, `StarRules`, `AuditAssets`, `Phase1Checks`, `Phase2Checks`, `Phase3Checks`, `LxdChecks`).
- **Voice-Over Audit:** Run `python Tools/audit_vo.py` from the terminal to verify all **663** offline `.wav` voice clips against `levels.json`.

---

## Repository Structure

```text
lumooo/
├── Assets/
│   ├── Resources/
│   │   ├── Art/
│   │   │   ├── Backgrounds/   # 12 full-HD (1920x1080) storybook backdrops
│   │   │   ├── Characters/    # 7 character portraits (600x600 transparent PNGs)
│   │   │   ├── Objects/       # 20 puzzle & collectible sprites (256x256 PNGs)
│   │   │   └── UI/            # 33 9-slice panels, buttons, icons, & badges
│   │   ├── Audio/
│   │   │   ├── Music/         # 3 background loops (menu, gameplay, victory)
│   │   │   ├── SFX/           # 8 interface & reward sound effects
│   │   │   └── Voice/         # 663 offline WAV voice-over clips
│   │   ├── Data/
│   │   │   └── levels.json    # Complete question bank (13 levels, 30 ILOs, 197 challenges)
│   │   └── Fonts/             # Baloo 2 Bold TTF + baked TextMeshPro SDF assets
│   ├── Scenes/
│   │   └── LumoMain.unity     # Single bootstrap scene
│   ├── Scripts/
│   │   ├── Core/              # Game, Question, Adaptive, Score, Save, Audio, Timer managers
│   │   ├── Data/              # Serializable data models for levels.json and save files
│   │   ├── Editor/            # FontBaker and LumoSetup editor utilities
│   │   ├── Modules/           # 12 interactive puzzle modules + ModuleBase
│   │   └── UI/                # Procedural uGUI screens, overlays, Parents Area, and UIKit
│   └── Tests/
│       └── LumoTests.cs       # Editor verification suite
├── Docs/                      # Full game documentation & storyboard
├── Packages/                  # Unity Package Manager manifest
├── ProjectSettings/           # Unity project settings
└── Tools/                     # Python/PowerShell asset & VO generation/audit utilities
```
