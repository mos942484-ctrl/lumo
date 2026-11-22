"""LXD Phase B authoring: inject per-level ILOs and per-challenge ilo indices
into Assets/Resources/Data/levels.json. Idempotent (overwrites both fields).
Run:  python -I Tools/inject_ilos.py
"""
import json, io

PATH = "Assets/Resources/Data/levels.json"

# level id -> list of ILO dicts (order = validator index; see Docs/LXD_ILO_MAP.md)
ILOS = {
    1: [
        {"text": "Understand that adding means combining two groups and counting all of them.",
         "kidText": "I can put two groups together and count them all!",
         "verb": "understand", "assess": "understanding", "skills": ["addition"]},
        {"text": "Recall sums within 10 with objects, then without.",
         "kidText": "I can add small numbers all by myself!",
         "verb": "recall", "assess": "knowledge", "skills": ["addition"]},
        {"text": "Apply counting objects one by one to find how many (up to 10).",
         "kidText": "I can count things one by one!",
         "verb": "apply", "assess": "knowledge", "skills": ["counting"]},
    ],
    2: [
        {"text": "Recognise common English words by their sound, picture and written form.",
         "kidText": "I can find the word when I hear it!",
         "verb": "recognise", "assess": "understanding", "skills": ["vocabulary"]},
        {"text": "Identify color words (red, blue, green, yellow) and match them to objects.",
         "kidText": "I know my color words!",
         "verb": "identify", "assess": "knowledge", "skills": ["vocabulary"]},
    ],
    3: [
        {"text": "Understand that numerals, picture quantities and number words show the same amount.",
         "kidText": "Three, 3 and three apples all mean the same!",
         "verb": "understand", "assess": "understanding", "skills": ["counting"]},
        {"text": "Recall where hidden pairs are after the cards are covered again.",
         "kidText": "I can remember where things are hiding!",
         "verb": "recall", "assess": "understanding", "skills": ["memory"]},
    ],
    4: [
        {"text": "Understand subtraction as taking away objects and counting what is left.",
         "kidText": "I can take away and see what is left!",
         "verb": "understand", "assess": "understanding", "skills": ["subtraction"]},
        {"text": "Recall subtraction facts within 10.",
         "kidText": "I know take-away answers super fast!",
         "verb": "recall", "assess": "knowledge", "skills": ["subtraction", "counting"]},
        {"text": "Apply two-step problems: finish one operation, then use its result in the next.",
         "kidText": "I can do one sum, then another, and finish the quest!",
         "verb": "apply", "assess": "understanding", "skills": ["twoStep"]},
    ],
    5: [
        {"text": "Construct a target word by placing its letters in the correct order.",
         "kidText": "I can build words letter by letter!",
         "verb": "construct", "assess": "knowledge", "skills": ["spelling"]},
        {"text": "Produce a picture on a pixel grid by naming colors and copying square by square.",
         "kidText": "I can paint a picture on a tiny grid!",
         "verb": "produce", "assess": "understanding", "skills": ["art"]},
    ],
    6: [
        {"text": "Apply adding and taking away to reach an exact total of potion ingredients.",
         "kidText": "I can add and remove to get the right number!",
         "verb": "apply", "assess": "understanding", "skills": ["addition", "subtraction"]},
        {"text": "Classify ingredients by color and count each colored group.",
         "kidText": "I can sort by color and count!",
         "verb": "classify", "assess": "knowledge", "skills": ["science"]},
    ],
    7: [
        {"text": "Apply addition and subtraction with larger numbers on the way up the mountain.",
         "kidText": "I can do big adds and take-aways!",
         "verb": "apply", "assess": "understanding", "skills": ["addition", "subtraction"]},
        {"text": "Understand a short science fact and use the numbers inside it to solve.",
         "kidText": "I can use science clues to help me solve!",
         "verb": "understand", "assess": "understanding", "skills": ["science"]},
    ],
    8: [
        {"text": "Identify everyday science words from pictures and clues.",
         "kidText": "I can find science words I know!",
         "verb": "identify", "assess": "knowledge", "skills": ["vocabulary", "science"]},
        {"text": "Recall the simple science facts met on the journey.",
         "kidText": "I remember the fun facts!",
         "verb": "recall", "assess": "knowledge", "skills": ["science"]},
    ],
    9: [
        {"text": "Apply step-by-step directions (left, right, up, down) to reach a target on a grid.",
         "kidText": "I can follow the map steps to the treasure!",
         "verb": "apply", "assess": "understanding", "skills": ["directions"]},
        {"text": "Apply counting to answer how many keys or steps the map needs.",
         "kidText": "I can count my keys and steps!",
         "verb": "apply", "assess": "knowledge", "skills": ["counting"]},
    ],
    10: [
        {"text": "Deduce the answer to two-step royal missions by chaining two operations.",
         "kidText": "I can do two sums and find the royal answer!",
         "verb": "deduce", "assess": "understanding", "skills": ["twoStep"]},
        {"text": "Recall the science facts used in the royal missions.",
         "kidText": "I remember the facts the King asked about!",
         "verb": "recall", "assess": "knowledge", "skills": ["science"]},
    ],
    11: [
        {"text": "Construct a full sentence by ordering word cards from left to right.",
         "kidText": "I can build a whole sentence!",
         "verb": "construct", "assess": "knowledge", "skills": ["sentence"]},
        {"text": "Choose kind responses when a friend feels sad, frustrated, or left out.",
         "kidText": "I can help a friend who is having a hard feeling.",
         "verb": "choose", "assess": "attitude", "skills": ["kindness"]},
        {"text": "Recognise the feelings named in the kindness scenarios.",
         "kidText": "I can tell how a friend is feeling!",
         "verb": "recognise", "assess": "understanding", "skills": ["kindness"]},
    ],
    12: [
        {"text": "Apply adding and removing to sort and count workshop items into groups.",
         "kidText": "I can sort, add and take away in the workshop!",
         "verb": "apply", "assess": "understanding", "skills": ["addition", "subtraction", "counting"]},
        {"text": "Classify items by color and kind before counting each group.",
         "kidText": "I can put things in the right box!",
         "verb": "classify", "assess": "understanding", "skills": ["science"]},
    ],
    13: [
        {"text": "Apply the kingdom's number skills: count, add, subtract and chain two steps.",
         "kidText": "I can use all my number powers!",
         "verb": "apply", "assess": "understanding",
         "skills": ["addition", "counting", "subtraction", "twoStep"]},
        {"text": "Produce words, sentences and routes from the earlier islands without picture help.",
         "kidText": "I can build words, sentences and find the way!",
         "verb": "produce", "assess": "understanding",
         "skills": ["spelling", "sentence", "vocabulary", "directions"]},
        {"text": "Perform 3-5 note melodies by listening and playing them back.",
         "kidText": "I can listen and play the tune back!",
         "verb": "perform", "assess": "understanding", "skills": ["music"]},
    ],
}

# challenge id -> ilo index (core challenges only; variants never carry ilo)
ILO_INDEX = {
    "L1R1": 0, "L1R2": 0, "L1R3": 0, "P2_L1R4": 2, "P2_L1R5": 1, "P2_L1R6": 1,
    "L2R1": 0, "L2R2": 0, "L2R3": 1, "P2_L2R4": 0, "P2_L2R5": 0, "P2_L2R6": 0,
    "P2_L3R4": 1, "P2_L3R5": 1, "P2_L3R6": 1, "L3R1": 0, "L3R2": 0, "L3R3": 0,
    "P2_L4R4": 1, "P2_L4R5": 2, "P2_L4R6": 2, "L4R1": 0, "L4R2": 0, "L4R3": 0,
    "P2_L5R4": 1, "P2_L5R5": 1, "P2_L5R6": 1, "L5R1": 0, "L5R2": 0, "L5R3": 0,
    "P2_L6R4": 1, "P2_L6R5": 1, "P2_L6R6": 1, "L6R1": 0, "L6R2": 0, "L6R3": 0,
    "P2_L7R4": 1, "P2_L7R5": 1, "P2_L7R6": 1, "L7R1": 0, "L7R2": 0, "L7R3": 0,
    "P2_L8R4": 0, "P2_L8R5": 1, "P2_L8R6": 1, "L8R1": 0, "L8R2": 0, "L8R3": 0,
    "P2_L9R4": 0, "P2_L9R5": 0, "P2_L9R6": 0, "L9R1": 0, "L9R2": 0, "L9R3": 1,
    "P2_L10R4": 1, "P2_L10R5": 1, "P2_L10R6": 1, "L10R1": 0, "L10R2": 0, "L10R3": 0,
    "P2_L11R4": 2, "P2_L11R5": 1, "P2_L11R6": 1, "L11R1": 0, "L11R2": 0, "L11R3": 0,
    "P2_L12R4": 1, "P2_L12R5": 1, "P2_L12R6": 1, "L12R1": 0, "L12R2": 0, "L12R3": 0,
    "P2_L13R13": 2, "P2_L13R14": 2, "P2_L13R15": 2,
    "F1": 0, "F2": 0, "F3": 0, "F4": 1, "F5": 1, "F6": 1, "F7": 0,
    "F8": 0, "F9": 1, "F10": 0, "F11": 0, "F12": 0,
}


def main():
    with io.open(PATH, encoding="utf-8") as f:
        bank = json.load(f)

    problems = []
    for lv in bank["levels"]:
        lid = lv["id"]
        if lid not in ILOS:
            problems.append("L%d: no ILO authoring" % lid)
            continue
        lv["ilos"] = ILOS[lid]
        # coverage self-check: union of ILO skills must cover the level's skills
        covered = set()
        for ilo in lv["ilos"]:
            covered.update(ilo["skills"])
        used = set(c["skill"] for c in lv["challenges"])
        missing = used - covered
        if missing:
            problems.append("L%d: ILO skills do not cover %s" % (lid, sorted(missing)))
        for c in lv["challenges"]:
            cid = c["id"]
            if cid not in ILO_INDEX:
                problems.append("%s: no ilo index mapping" % cid)
                continue
            idx = ILO_INDEX[cid]
            if idx >= len(lv["ilos"]):
                problems.append("%s: ilo index %d out of range" % (cid, idx))
            else:
                c["ilo"] = idx

    if problems:
        for p in problems:
            print("PROBLEM:", p)
        raise SystemExit(1)

    with io.open(PATH, "w", encoding="utf-8", newline="\n") as f:
        json.dump(bank, f, indent=1, ensure_ascii=False)
        f.write("\n")
    print("OK: %d levels authored, %d core challenges indexed"
          % (len(bank["levels"]), sum(len(lv["challenges"]) for lv in bank["levels"])))


if __name__ == "__main__":
    main()
