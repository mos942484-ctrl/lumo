# Learning Experience Design (LXD) & Curriculum Reference

**Lumo and the Crystal of Knowledge** is engineered around formal **Learning Experience Design (LXD)** principles and the **Learning Mechanics–Game Mechanics (LM-GM)** framework. Every challenge maps to an explicit **Intended Learning Outcome (ILO)**, adapts across three developmental age bands (`5–7`, `8–10`, `11–12`), and provides non-punitive scaffolding.

---

## 1. The 13 Tracked Curriculum Skills

Defined in [`levels.json`](file:///d:/Unity/lumooo/Assets/Resources/Data/levels.json) and tracked per-child in [`SaveData.skills`](file:///d:/Unity/lumooo/Assets/Scripts/Data/SaveData.cs):

| # | Skill ID | Domain | Primary Islands | Description |
|---|---|---|---|---|
| 1 | `addition` | Mathematics | L1, L6, L7, L12, L13 | Combining groups, number-line jumps up, place value (`tens/ones`), and elapsed time. |
| 2 | `subtraction` | Mathematics | L4, L6, L7, L12, L13 | Taking away from a group, counting what remains, and number-line jumps down. |
| 3 | `twoStep` | Mathematics & Logic | L4, L10, L13 | Chaining two operations (`(a + b) - c`), skip-counting patterns, and deductive ordering clues. |
| 4 | `counting` | Mathematics | L1, L3, L4, L9, L12, L13 | One-to-one correspondence up to 15, numeral-word-quantity equivalence, and geometric sides. |
| 5 | `vocabulary` | English Literacy | L2, L8, L13 | Sight words, phonics blends (`tr-`), rhymes, synonyms, colors, and size modifiers (`big`/`small`). |
| 6 | `spelling` | English Literacy | L5, L13 | Constructing 3- and 4-letter words (`CAT`, `SUN`, `DOG`, `STAR`, `SHIP`, `TREE`, `BOOK`, `CROWN`) letter by letter. |
| 7 | `sentence` | English Literacy | L11, L13 | Left-to-right syntactic word ordering to construct complete English sentences. |
| 8 | `directions` | Spatial Reasoning | L9, L13 | Step-by-step cardinal/grid navigation (`UP`, `DOWN`, `LEFT`, `RIGHT`) and compass sequences. |
| 9 | `kindness` | Social-Emotional (SEL) | L11 | Identifying emotions, choosing empathetic responses, gratitude, and fair turn-taking. |
| 10 | `science` | Natural & Earth Science | L6, L7, L8, L10, L12 | Plant life cycles, habitats, states of matter, weather/light, marine biology, astronomy, and fossils. |
| 11 | `memory` | Executive Function | L3 | Visual-spatial working memory pairing pictures, words, numerals, and opposites. |
| 12 | `art` | Visual Arts & Geometry | L5 | Grid coordinates, color naming, mirror symmetry, and repeating tile patterns. |
| 13 | `music` | Auditory & Music | L13 | Pitch discrimination, scale progression (`C4..A4`), and echoing 3–5 note melodies. |

---

## 2. Complete Map of All 30 Intended Learning Outcomes (ILOs)

Every level defines `2–3` ILOs in `levels.json` with an educator statement (`text`), a kid-facing goal (`kidText` displayed on `LevelIntroScreen`), a Bloom's taxonomy `verb`, and an `assess` category (`knowledge`, `understanding`, `attitude`).

| Island | ILO # | Verb / Assess | Educator ILO Statement | Kid-Facing Goal (`"I can..."`) |
|---|---|---|---|---|
| **L1: Math Mountain** | `1.0` | `understand` / `understanding` | Understand that adding means combining two groups and counting all of them. | *"I can put two groups together and count them all!"* |
| | `1.1` | `recall` / `knowledge` | Recall sums within 10 with objects, then without. | *"I can add small numbers all by myself!"* |
| | `1.2` | `apply` / `knowledge` | Apply counting objects one by one to find how many (up to 10). | *"I can count things one by one!"* |
| **L2: Word Forest** | `2.0` | `recognise` / `understanding` | Recognise common English words by their sound, picture and written form. | *"I can find the word when I hear it!"* |
| | `2.1` | `identify` / `knowledge` | Identify color words (red, blue, green, yellow) and match them to objects. | *"I know my color words!"* |
| **L3: Memory Garden** | `3.0` | `understand` / `understanding` | Understand that numerals, picture quantities and number words show the same amount. | *"Three, 3 and three apples all mean the same!"* |
| | `3.1` | `recall` / `understanding` | Recall where hidden pairs are after the cards are covered again. | *"I can remember where things are hiding!"* |
| **L4: Learning Island** | `4.0` | `understand` / `understanding` | Understand subtraction as taking away objects and counting what is left. | *"I can take away and see what is left!"* |
| | `4.1` | `recall` / `knowledge` | Recall subtraction facts within 10. | *"I know take-away answers super fast!"* |
| | `4.2` | `apply` / `understanding` | Apply two-step problems: finish one operation, then use its result in the next. | *"I can do one sum, then another, and finish the quest!"* |
| **L5: Art Studio** | `5.0` | `construct` / `knowledge` | Construct a target word by placing its letters in the correct order. | *"I can build words letter by letter!"* |
| | `5.1` | `produce` / `understanding` | Produce a picture on a pixel grid by naming colors and copying square by square. | *"I can paint a picture on a tiny grid!"* |
| **L6: Science Lab** | `6.0` | `apply` / `understanding` | Apply adding and taking away to reach an exact total of potion ingredients. | *"I can add and remove to get the right number!"* |
| | `6.1` | `classify` / `knowledge` | Classify ingredients by color and count each colored group. | *"I can sort by color and count!"* |
| **L7: Sky Kingdom** | `7.0` | `apply` / `understanding` | Apply addition and subtraction with larger numbers on the way up the mountain. | *"I can do big adds and take-aways!"* |
| | `7.1` | `understand` / `understanding` | Understand a short science fact and use the numbers inside it to solve. | *"I can use science clues to help me solve!"* |
| **L8: Ocean Reef** | `8.0` | `identify` / `knowledge` | Identify everyday science words from pictures and clues. | *"I can find science words I know!"* |
| | `8.1` | `recall` / `knowledge` | Recall the simple science facts met on the journey. | *"I remember the fun facts!"* |
| **L9: Puzzle Park** | `9.0` | `apply` / `understanding` | Apply step-by-step directions (left, right, up, down) to reach a target on a grid. | *"I can follow the map steps to the treasure!"* |
| | `9.1` | `apply` / `knowledge` | Apply counting to answer how many keys or steps the map needs. | *"I can count my keys and steps!"* |
| **L10: Space Station** | `10.0` | `deduce` / `understanding` | Deduce the answer to two-step royal missions by chaining two operations. | *"I can do two sums and find the royal answer!"* |
| | `10.1` | `recall` / `knowledge` | Recall the science facts used in the royal missions. | *"I remember the facts the King asked about!"* |
| **L11: Kindness Corner** | `11.0` | `construct` / `knowledge` | Construct a full sentence by ordering word cards from left to right. | *"I can build a whole sentence!"* |
| | `11.1` | `choose` / `attitude` | Choose kind responses when a friend feels sad, frustrated, or left out. | *"I can help a friend who is having a hard feeling."* |
| | `11.2` | `recognise` / `understanding` | Recognise the feelings named in the kindness scenarios. | *"I can tell how a friend is feeling!"* |
| **L12: Dino Valley** | `12.0` | `apply` / `understanding` | Apply adding and removing to sort and count workshop items into groups. | *"I can sort, add and take away in the workshop!"* |
| | `12.1` | `classify` / `understanding` | Classify items by color and kind before counting each group. | *"I can put things in the right box!"* |
| **L13: Music Room** | `13.0` | `apply` / `understanding` | Apply the kingdom's number skills: count, add, subtract and chain two steps. | *"I can use all my number powers!"* |
| | `13.1` | `produce` / `understanding` | Produce words, sentences and routes from the earlier islands without picture help. | *"I can build words, sentences and find the way!"* |
| | `13.2` | `perform` / `understanding` | Perform 3-5 note melodies by listening and playing them back. | *"I can listen and play the tune back!"* |

---

## 3. Adaptive Difficulty, Scaffolding, & Mastery Engine

Managed by [`AdaptiveManager.cs`](file:///d:/Unity/lumooo/Assets/Scripts/Core/AdaptiveManager.cs) and [`GameManager.cs`](file:///d:/Unity/lumooo/Assets/Scripts/Core/GameManager.cs).

### 3.1 Three-Tier Age-Band Routing
Parents can switch the child's age band at any time in the **Parents Area**:
- **Band 0 (`Ages 5–7: Concrete & Visual`):** Routes challenges with a `band5to7` variant ID to concrete, visual, lower-number variants (or pulls from the skill's `easier` variant pool).
- **Band 1 (`Ages 8–10: Conceptual & Guided`):** Default core challenge bank (`87` challenges), adapting dynamically up or down based on performance streaks.
- **Band 2 (`Ages 11–12: Deductive & Multi-step`):** Routes challenges with a `band11to12` variant ID to multi-step reasoning, larger arithmetic, or deeper science variants (or pulls from the skill's `harder` variant pool).

### 3.2 Dynamic Streak Promotion & Remediation
Each of the 13 skills maintains its own [`SkillStats`](file:///d:/Unity/lumooo/Assets/Scripts/Data/SaveData.cs) (`attempts`, `firstTryCorrect`, `hintsUsed`, `firstTryStreak`, `failStreak`, `mastery 0..3`):
- **3 Consecutive First-Try Solves (`firstTryStreak >= 3`):**
  - Advances skill mastery (`0: Learning → 1: Practicing → 2: Good → 3: Mastered`).
  - Queues a `pendingHarder` variant for the next round in that skill.
  - Triggers a celebratory toast (`"3 in a row! Skill leveled up!"`) and awards a permanent **Mastery Medal** upon reaching level 3 (`Mastered`).
- **2 Consecutive Struggles (`failStreak >= 2`):**
  - Enters `remediation = true` for that skill and offers the child a **Learner-Agency Struggle Choice** banner (`"Want to try an easier one?"` → `[Try easier]` / `[Same again]`) once per level.

### 3.3 Graduated 3-Stage Mistake Scaffolding
1. **1st Mistake:** Displays a strategy-specific retry banner (`feedback.retry`) with voice-over—never generic phrases like *"Try again!"* or negative words (*"Wrong"*, *"Fail"*).
2. **2nd Mistake on Same Round:** Automatically opens Lumo's **Hint Panel** (`feedback.hint`) and pauses the round timer while the hint is open.
3. **3rd Mistake on Same Round:** Automatically opens Lumo's **Step-by-Step Solution** (`feedback.solution` + `feedback.explain`) showing the underlying reasoning.

### 3.4 Spaced Vocabulary Review & 2-Island Mastery Rule
- Words encountered on `word-picture`, `spelling`, and `sentence` rounds are tracked in `SaveData.wordsLearned`.
- A word is only marked **Mastered** (`AdaptiveManager.IsWordMastered`) after the child solves it cleanly on the first try across **at least 2 different islands** (`wordCleanLevels`).
- At the start of any new island (L2–L12), `GameManager.TryStartWithReview()` checks if the child has any unmastered vocabulary words (prioritizing words the child marked as *"Still learning"* in **My Words**) and injects a warm-up review round.

---

## 4. Parent Co-Play & Analytics Tools (`ParentsArea.cs`)

- **Co-Play Scaffolding Prompts (`ParentTipsPanel`):** Provides tailored **"While Playing"** (open-ended questioning) and **"After Playing"** (real-world transfer) conversation prompts for all 13 skills.
- **My Words Picture Book (`MyWordsPanel`):** Interactive 11-word visual flashcard book (`apple`, `banana`, `cat`, `dog`, `bird`, `fish`, `red`, `blue`, `green`, `tree`, `house`) where children can tap `[Hear Word]` and toggle `[I know it!]` vs. `[Still learning]` to steer spaced review.
- **PlayLog Telemetry & CSV Export (`PlayLog.cs`, `SessionReport.cs`):** Records a 500-entry ring buffer of round attempts, hints, and median time-to-correct, exportable to `session_report.csv` from the Parents Area.
