#!/usr/bin/env python3
"""
Phase 2 Wonder Islands content bank generator and validator.

Idempotently strips any prior Phase 2 entries (P2_* / V_P2_*), preserves all
original 36 core challenges, 12 Level 13 tasks, and 32 adaptive variants, then
appends 3 new activities to every level (1..13) plus 78 redesigned Concrete (5-7)
and Deductive (11-12) age-band variants.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_BANK_PATH = REPO_ROOT / "Assets/Resources/Data/levels.json"
OBJECTS_DIR = REPO_ROOT / "Assets/Resources/Art/Objects"
BACKGROUNDS_DIR = REPO_ROOT / "Assets/Resources/Art/Backgrounds"

PHASE2_SKILLS = [
    "addition", "subtraction", "twoStep", "counting",
    "vocabulary", "spelling", "sentence", "directions",
    "kindness", "science", "memory", "art", "music",
]

VALID_TYPES = {
    "math-choice", "word-picture", "matching", "spelling",
    "potion", "directions", "sentence", "sorting",
    "choice-card", "memory-match", "pixel-art", "music-play",
}

VALID_NOTES = {"C", "D", "E", "F", "G", "A", "C5"}

REVIEWABLE_WORDS = {
    "cat", "dog", "bird", "fish", "ball", "tree", "house", "star", "sun", "key",
    "apple", "banana", "gem", "chest", "coin", "crown", "basket", "crystal", "shard",
    "red", "blue", "green", "yellow",
}

HEX_RE = re.compile(r"^#[0-9A-Fa-f]{6}$")
VO_RE = re.compile(r"^vo_[A-Za-z0-9_]+$")


def is_ascii(text: str) -> bool:
    return all(ord(ch) < 128 for ch in text)


def make_challenge(
    cid: str,
    level: int,
    rnd: int,
    part: int,
    ctype: str,
    skill: str,
    difficulty: str,
    prompt: str,
    vo: str,
    media: dict[str, Any],
    correct: str,
    options: list[str],
    hints: list[str],
    success: str,
    retry: str,
    words: list[str] | None = None,
    easier: str | None = None,
    harder: str | None = None,
    band5to7: str | None = None,
    band11to12: str | None = None,
) -> dict[str, Any]:
    return {
        "id": cid,
        "level": level,
        "round": rnd,
        "part": part,
        "type": ctype,
        "skill": skill,
        "difficulty": difficulty,
        "timer": 30,
        "prompt": prompt,
        "vo": vo,
        "media": media,
        "correctAnswer": correct,
        "options": options,
        "hints": hints,
        "feedback": {"success": success, "retry": retry},
        "words": words or [],
        "easier": easier,
        "harder": harder,
        "band5to7": band5to7,
        "band11to12": band11to12,
    }


def pair(
    pid: str,
    a_label: str = "",
    a_sprite: str = "",
    a_tint: str | None = None,
    b_label: str = "",
    b_sprite: str = "",
    b_tint: str | None = None,
) -> dict[str, Any]:
    return {
        "id": pid,
        "aLabel": a_label,
        "aSprite": a_sprite,
        "aTint": a_tint,
        "bLabel": b_label,
        "bSprite": b_sprite,
        "bTint": b_tint,
    }


def build_phase2_entries() -> tuple[dict[int, list[dict[str, Any]]], list[dict[str, Any]]]:
    """Builds 39 base challenges (3 per level 1..13) and 78 redesigned 5-7 / 11-12 variants."""
    by_level: dict[int, list[dict[str, Any]]] = {lv: [] for lv in range(1, 14)}
    variants: list[dict[str, Any]] = []

    def register(
        level: int,
        rnd: int,
        base_spec: dict[str, Any],
        v57_spec: dict[str, Any],
        v1112_spec: dict[str, Any],
    ) -> None:
        base_id = f"P2_L{level}R{rnd}"
        v57_id = f"V_P2_L{level}R{rnd}_57"
        v1112_id = f"V_P2_L{level}R{rnd}_1112"
        part = 5 if level == 13 else 0

        base = make_challenge(
            cid=base_id,
            level=level,
            rnd=rnd,
            part=part,
            vo=f"vo_p2_l{level}r{rnd}",
            easier=v57_id,
            harder=v1112_id,
            band5to7=v57_id,
            band11to12=v1112_id,
            difficulty="base",
            **base_spec,
        )
        v57 = make_challenge(
            cid=v57_id,
            level=0,
            rnd=0,
            part=0,
            vo=f"vo_var_{v57_id}",
            difficulty="easier",
            **v57_spec,
        )
        v1112 = make_challenge(
            cid=v1112_id,
            level=0,
            rnd=0,
            part=0,
            vo=f"vo_var_{v1112_id}",
            difficulty="harder",
            **v1112_spec,
        )
        by_level[level].append(base)
        variants.extend([v57, v1112])

    # ---- Level 1: Math Mountain (counting / place-value / time) ----
    register(
        1, 4,
        {
            "ctype": "math-choice", "skill": "counting",
            "prompt": "Count the mountain stars carefully! How many stars are there?",
            "media": {"obj": "obj_star", "a": 12, "b": 0, "op": "+", "demo": "none", "countOnly": True},
            "correct": "12", "options": ["10", "11", "12", "13"],
            "hints": ["Count row by row from top to bottom.", "There are two rows of 5 stars plus 2 more: 12 stars!"],
            "success": "Great counting! There are 12 mountain stars!",
            "retry": "Take your time and count each star one by one!",
            "words": ["star"],
        },
        {
            "ctype": "math-choice", "skill": "counting",
            "prompt": "Count the red apples! How many do you see?",
            "media": {"obj": "obj_apple", "a": 4, "b": 0, "op": "+", "demo": "none", "countOnly": True},
            "correct": "4", "options": ["3", "4", "5", "6"],
            "hints": ["Touch each apple as you count from 1.", "Count them together: 1, 2, 3, 4 apples!"],
            "success": "Awesome! You counted 4 apples!",
            "retry": "Try again and point to each apple!",
            "words": ["apple"],
        },
        {
            "ctype": "choice-card", "skill": "counting",
            "prompt": "Solve the mountain number clues!",
            "media": {
                "subtitle": "Number Detective",
                "obj": "obj_star",
                "scenario": "Use all three clues to find the secret peak number:",
                "clues": [
                    "It is an even number between 21 and 29.",
                    "Its ones digit is twice its tens digit.",
                    "What is the secret number?",
                ],
            },
            "correct": "24", "options": ["24", "42", "26", "22"],
            "hints": ["Between 21 and 29 means the tens digit is 2.", "Twice 2 is 4, so 2 tens and 4 ones makes 24!"],
            "success": "Brilliant deduction! 2 tens and 4 ones gives 24!",
            "retry": "Check the tens digit first, then double it for the ones!",
            "words": ["star"],
        },
    )

    register(
        1, 5,
        {
            "ctype": "choice-card", "skill": "addition",
            "prompt": "Find the number with 3 tens and 5 ones!",
            "media": {
                "subtitle": "Place Value",
                "obj": "obj_coin",
                "scenario": "A mountain chest holds 3 bags of 10 coins and 5 loose coins. What is the total?",
                "sequence": ["30", "+", "5", "=", "?"],
            },
            "correct": "35", "options": ["35", "53", "8", "30"],
            "hints": ["3 bags of 10 coins make 30.", "30 plus 5 more coins equals 35!"],
            "success": "Spot on! 3 tens and 5 ones equals 35!",
            "retry": "Remember tens go on the left and ones go on the right!",
            "words": ["coin"],
        },
        {
            "ctype": "choice-card", "skill": "addition",
            "prompt": "Count 10 coins and 2 more coins!",
            "media": {
                "subtitle": "Tens and Ones",
                "obj": "obj_coin",
                "scenario": "One full stack has 10 coins, and 2 extra coins sit beside it. How many coins in all?",
                "sequence": ["10", "+", "2", "=", "?"],
            },
            "correct": "12", "options": ["12", "10", "21"],
            "hints": ["Start at 10 and count 2 more.", "10... 11, 12! There are 12 coins!"],
            "success": "Nice job! 10 plus 2 makes 12!",
            "retry": "Count on 2 steps from 10!",
            "words": ["coin"],
        },
        {
            "ctype": "choice-card", "skill": "addition",
            "prompt": "Crack the three-digit place-value code!",
            "media": {
                "subtitle": "Place Value Code",
                "obj": "obj_coin",
                "scenario": "Deduce the vault code from the mountain clues:",
                "clues": [
                    "The hundreds digit is 4.",
                    "The tens digit is 3 less than the hundreds digit.",
                    "All three digits add up to 10.",
                ],
            },
            "correct": "415", "options": ["415", "451", "406", "316"],
            "hints": ["Subtract 3 from 4 to find the tens digit (1).", "Now 4 + 1 + ones = 10, so the ones digit is 5: 415!"],
            "success": "Master codebreaker! 415 fits every clue!",
            "retry": "Find the tens digit first (4 - 3), then make the sum equal 10!",
            "words": ["coin"],
        },
    )

    register(
        1, 6,
        {
            "ctype": "choice-card", "skill": "addition",
            "prompt": "What time will the mountain clock show in 2 hours?",
            "media": {
                "subtitle": "Mountain Clock",
                "obj": "obj_sun",
                "scenario": "The clock shows 3:00 right now. What time will it be 2 hours later?",
                "sequence": ["3:00", "4:00", "?"],
            },
            "correct": "5:00", "options": ["5:00", "4:00", "6:00", "1:00"],
            "hints": ["Count forward 2 hours from 3 o'clock.", "3:00 plus 1 hour is 4:00, plus 1 more hour is 5:00!"],
            "success": "Right on time! Two hours after 3:00 is 5:00!",
            "retry": "Count forward 2 hours from 3:00!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "addition",
            "prompt": "Find the clock that shows 12 o'clock noon!",
            "media": {
                "subtitle": "Daylight Clock",
                "obj": "obj_sun",
                "scenario": "The sun is high in the sky at noon. Which digital time says 12 o'clock?",
            },
            "correct": "12:00", "options": ["12:00", "3:00", "6:00"],
            "hints": ["Look for the number 12 before the colon.", "12:00 means twelve o'clock noon!"],
            "success": "Great job! 12:00 is twelve o'clock!",
            "retry": "Check the hour number on the left of the dots!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "addition",
            "prompt": "Calculate the climbers' finish time!",
            "media": {
                "subtitle": "Summit Schedule",
                "obj": "obj_sun",
                "scenario": "Hikers leave base camp at 8:15 AM:",
                "clues": [
                    "The steep trail takes 45 minutes to reach the ridge.",
                    "They rest for 20 minutes at the ridge.",
                    "What time do they start climbing again?",
                ],
            },
            "correct": "9:20", "options": ["9:20", "9:00", "8:55", "9:35"],
            "hints": ["8:15 plus 45 minutes brings them to 9:00.", "Add 20 rest minutes to 9:00 to get 9:20!"],
            "success": "Exact timing! 8:15 + 45 min + 20 min = 9:20!",
            "retry": "Add 45 minutes to 8:15 first to reach the next hour!",
            "words": ["sun"],
        },
    )

    # ---- Level 2: Word Forest (phonics / rhymes / synonyms) ----
    register(
        2, 4,
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Which word starts with the same 'tr' blend as TREE?",
            "media": {
                "subtitle": "Phonics",
                "obj": "obj_tree",
                "scenario": "Say TREE out loud. Which word begins with the same 'tr' sound?",
            },
            "correct": "TRAIN", "options": ["TRAIN", "CAT", "FISH", "BALL"],
            "hints": ["Listen to the first two letters: T-R.", "TR-ee and TR-ain both start with 'tr'!"],
            "success": "Great ear! TREE and TRAIN share the 'tr' sound!",
            "retry": "Say each word aloud and listen to the starting blend!",
            "words": ["tree"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Which letter starts the word BIRD?",
            "media": {
                "subtitle": "First Sound",
                "obj": "obj_bird",
                "scenario": "Say BIRD slowly. Which letter makes the 'b' sound at the beginning?",
            },
            "correct": "B", "options": ["B", "C", "S"],
            "hints": ["Listen to the very first sound: b-ird.", "The letter B makes the 'b' sound in BIRD!"],
            "success": "Yes! B is for BIRD!",
            "retry": "Say b-b-bird and match the first letter!",
            "words": ["bird"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Solve the Word Forest phonics clues!",
            "media": {
                "subtitle": "Word Clue",
                "obj": "obj_sun",
                "scenario": "Which forest word fits every clue?",
                "clues": [
                    "It rhymes with RUN and FUN.",
                    "It starts with the 's' sound.",
                    "It warms the forest canopy each day.",
                ],
            },
            "correct": "SUN", "options": ["SUN", "SON", "BUN", "FAN"],
            "hints": ["Start with S and end with -UN.", "S + UN spells SUN!"],
            "success": "Well deduced! SUN fits the sound and meaning!",
            "retry": "Check both the starting sound and the ending rhyme!",
            "words": ["sun"],
        },
    )

    register(
        2, 5,
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Which word rhymes with STAR?",
            "media": {
                "subtitle": "Rhyme Time",
                "obj": "obj_star",
                "scenario": "Words that rhyme have the same ending sound. Which word rhymes with STAR?",
            },
            "correct": "CAR", "options": ["CAR", "TREE", "DOG", "FISH"],
            "hints": ["Listen to the '-ar' ending in STAR.", "ST-ar and C-ar end with the same sound!"],
            "success": "Spot on! STAR and CAR rhyme!",
            "retry": "Say STAR with each option to hear the matching ending!",
            "words": ["star"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Which word rhymes with CAT?",
            "media": {
                "subtitle": "Rhyme Pair",
                "obj": "obj_cat",
                "scenario": "CAT ends with '-at'. Which word rhymes with CAT?",
            },
            "correct": "HAT", "options": ["HAT", "DOG", "SUN"],
            "hints": ["Look for the '-at' ending.", "C-at and H-at rhyme together!"],
            "success": "Great job! CAT and HAT rhyme!",
            "retry": "Listen for the '-at' sound at the end!",
            "words": ["cat"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Solve the forest rhyme riddle!",
            "media": {
                "subtitle": "Rhyme Riddle",
                "obj": "obj_tree",
                "scenario": "Read the clues and choose the matching word:",
                "clues": [
                    "It rhymes with TREE and SEE.",
                    "It buzzes near flowers and makes sweet honey.",
                    "Which word fits both clues?",
                ],
            },
            "correct": "BEE", "options": ["BEE", "BIRD", "KEY", "LEAF"],
            "hints": ["It ends with the long 'ee' sound like TREE.", "A honey-making insect ending in -ee is a BEE!"],
            "success": "Clever! BEE rhymes with TREE and makes honey!",
            "retry": "Check which choice BOTH rhymes with TREE and makes honey!",
            "words": ["tree"],
        },
    )

    register(
        2, 6,
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Which word means almost the same as BIG?",
            "media": {
                "subtitle": "Word Twins",
                "obj": "obj_house",
                "scenario": "Synonyms are words with similar meanings. Which word is a synonym for BIG?",
            },
            "correct": "LARGE", "options": ["LARGE", "TINY", "COLD", "SLOW"],
            "hints": ["Think of a word that describes a giant castle or house.", "LARGE means big in size!"],
            "success": "Wonderful! BIG and LARGE are synonyms!",
            "retry": "Watch out for opposites like TINY; pick the word that means BIG!",
            "words": ["house"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Which word means the same as HAPPY?",
            "media": {
                "subtitle": "Same Meaning",
                "obj": "obj_sun",
                "scenario": "Pick the word that means the same as feeling HAPPY:",
            },
            "correct": "GLAD", "options": ["GLAD", "SAD", "WET"],
            "hints": ["When you smile and feel cheerful, you feel...", "GLAD means happy!"],
            "success": "Yes! HAPPY and GLAD mean the same thing!",
            "retry": "Look for a cheerful feeling word!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Deduce the synonym for SWIFT!",
            "media": {
                "subtitle": "Context Clues",
                "obj": "obj_fish",
                "scenario": "Use the forest note to replace SWIFT with a synonym:",
                "clues": [
                    "The silver fish made a SWIFT dart past the rocks.",
                    "SWIFT describes moving with great speed.",
                    "Which word has the closest meaning?",
                ],
            },
            "correct": "FAST", "options": ["FAST", "HEAVY", "QUIET", "TIRED"],
            "hints": ["Moving with great speed means quick.", "FAST means the same as SWIFT!"],
            "success": "Great vocabulary! SWIFT means FAST!",
            "retry": "Look at the speed clue in the sentence!",
            "words": ["fish"],
        },
    )

    # ---- Level 3: Memory Garden (memory-match) ----
    register(
        3, 4,
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Match each garden animal picture with its name card!",
            "media": {
                "pairs": [
                    pair("cat", a_sprite="obj_cat", b_label="CAT"),
                    pair("dog", a_sprite="obj_dog", b_label="DOG"),
                    pair("bird", a_sprite="obj_bird", b_label="BIRD"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Flip two cards and remember where each animal and word sits.", "Match the cat picture to CAT, dog to DOG, and bird to BIRD!"],
            "success": "Sharp memory! You matched all garden animal pairs!",
            "retry": "Remember the cards you saw and try another pair!",
            "words": ["cat", "dog", "bird"],
        },
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Find the matching picture pairs in the garden!",
            "media": {
                "pairs": [
                    pair("apple", a_label="APPLE", a_sprite="obj_apple"),
                    pair("star", a_label="STAR", a_sprite="obj_star"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Tap two cards to flip them over.", "Find the two APPLE cards and the two STAR cards!"],
            "success": "Awesome memory! You found both pairs!",
            "retry": "Keep trying - remember where you saw each picture!",
            "words": ["apple", "star"],
        },
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Match each garden word with its riddle clue!",
            "media": {
                "pairs": [
                    pair("cat", a_label="CAT", b_label="Whiskers and meow"),
                    pair("fish", a_label="FISH", b_label="Swims with fins"),
                    pair("tree", a_label="TREE", b_label="Trunk and leaves"),
                    pair("sun", a_label="SUN", b_label="Bright daytime star"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Read each clue card and picture its matching word.", "Pair CAT with Whiskers and meow, FISH with Swims with fins, and so on!"],
            "success": "Fantastic deduction and memory!",
            "retry": "Hold each card's position and clue in mind!",
            "words": ["cat", "fish", "tree", "sun"],
        },
    )

    register(
        3, 5,
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Match every digit card with its number word!",
            "media": {
                "pairs": [
                    pair("one", a_label="1", b_label="ONE"),
                    pair("two", a_label="2", b_label="TWO"),
                    pair("three", a_label="3", b_label="THREE"),
                    pair("four", a_label="4", b_label="FOUR"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Watch for both numbers and number words.", "Pair 1 with ONE, 2 with TWO, 3 with THREE, and 4 with FOUR!"],
            "success": "Brilliant! Every number matched its word!",
            "retry": "Try remembering one pair at a time!",
        },
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Match the colored balls by memory!",
            "media": {
                "pairs": [
                    pair("red", a_label="RED", a_sprite="obj_ball", a_tint="#E53935"),
                    pair("blue", a_label="BLUE", a_sprite="obj_ball", a_tint="#1E88E5"),
                    pair("yellow", a_label="YELLOW", a_sprite="obj_ball", a_tint="#FDD835"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Look closely at the ball colors when cards flip.", "Match red with red, blue with blue, and yellow with yellow!"],
            "success": "Great eye! All three color pairs matched!",
            "retry": "Watch each color carefully and try again!",
            "words": ["red", "blue", "yellow"],
        },
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Match each math expression with its exact value!",
            "media": {
                "pairs": [
                    pair("eq7", a_label="2 + 5", b_label="7"),
                    pair("eq5", a_label="9 - 4", b_label="5"),
                    pair("eq9", a_label="3 + 6", b_label="9"),
                    pair("eq8", a_label="12 - 4", b_label="8"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Solve each card silently as you reveal it.", "Match 2+5=7, 9-4=5, 3+6=9, and 12-4=8!"],
            "success": "Sharp math and memory combined!",
            "retry": "Compute the card value first, then hunt its match!",
        },
    )

    register(
        3, 6,
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Match the opposite word pairs in the garden!",
            "media": {
                "pairs": [
                    pair("temp", a_label="HOT", b_label="COLD"),
                    pair("size", a_label="BIG", b_label="SMALL"),
                    pair("time", a_label="DAY", b_label="NIGHT"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Think of opposites: HOT goes with COLD.", "Match HOT/COLD, BIG/SMALL, and DAY/NIGHT!"],
            "success": "Wonderful! All opposite pairs matched!",
            "retry": "Look for the word with the opposite meaning!",
        },
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Find the matching garden treasure pairs!",
            "media": {
                "pairs": [
                    pair("crown", a_label="CROWN", a_sprite="obj_crown"),
                    pair("gem", a_label="GEM", a_sprite="obj_gem"),
                    pair("key", a_label="KEY", a_sprite="obj_key"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Flip two cards to see which treasures hide beneath.", "Match the crowns, gems, and keys!"],
            "success": "Treasure hunter! Every pair matched!",
            "retry": "Remember the spots you already flipped!",
            "words": ["crown", "gem", "key"],
        },
        {
            "ctype": "memory-match", "skill": "memory",
            "prompt": "Pair each word with its synonym card!",
            "media": {
                "pairs": [
                    pair("fast", a_label="FAST", b_label="QUICK"),
                    pair("happy", a_label="HAPPY", b_label="JOYFUL"),
                    pair("tiny", a_label="TINY", b_label="SMALL"),
                    pair("smart", a_label="SMART", b_label="CLEVER"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Look for pairs of words that share the same meaning.", "FAST=QUICK, HAPPY=JOYFUL, TINY=SMALL, SMART=CLEVER!"],
            "success": "Brilliant! Every synonym pair is locked in!",
            "retry": "Match words that mean the same thing!",
        },
    )

    # ---- Level 4: Learning Island (shapes / patterns / first-logic) ----
    register(
        4, 4,
        {
            "ctype": "choice-card", "skill": "counting",
            "prompt": "Count the total sides of a triangle and a square!",
            "media": {
                "subtitle": "Shape Count",
                "obj": "obj_house",
                "scenario": "A triangle has 3 straight sides and a square has 4 straight sides. How many sides altogether?",
                "sequence": ["Triangle (3)", "+", "Square (4)", "=", "?"],
            },
            "correct": "7", "options": ["7", "6", "8", "5"],
            "hints": ["Add the 3 triangle sides and 4 square sides.", "3 plus 4 equals 7 sides!"],
            "success": "Correct! 3 + 4 = 7 sides in total!",
            "retry": "Count 3 sides, then add 4 more!",
            "words": ["house"],
        },
        {
            "ctype": "choice-card", "skill": "counting",
            "prompt": "How many sides does a square have?",
            "media": {
                "subtitle": "Count Sides",
                "obj": "obj_house",
                "scenario": "Trace around a square window with your finger. How many straight sides does it have?",
                "sequence": ["Top", "Right", "Bottom", "Left"],
            },
            "correct": "4", "options": ["4", "3", "5"],
            "hints": ["Count the 4 edges shown on the strip.", "Top, right, bottom, left makes 4 sides!"],
            "success": "Yes! Every square has 4 equal sides!",
            "retry": "Count the four edges: top, right, bottom, left!",
            "words": ["house"],
        },
        {
            "ctype": "choice-card", "skill": "counting",
            "prompt": "Solve the polygon sides puzzle!",
            "media": {
                "subtitle": "Shape Logic",
                "obj": "obj_gem",
                "scenario": "Deduce the total sides of two hidden island shapes:",
                "clues": [
                    "Shape A has half as many sides as an octagon (8 sides).",
                    "Shape B has 1 fewer side than a hexagon (6 sides).",
                    "What is the sum of Shape A's sides and Shape B's sides?",
                ],
            },
            "correct": "9", "options": ["9", "8", "10", "11"],
            "hints": ["Half of 8 is 4, and 1 less than 6 is 5.", "Add 4 + 5 to get 9 sides!"],
            "success": "Sharp geometry! 4 + 5 = 9 sides!",
            "retry": "Find Shape A (8 / 2) and Shape B (6 - 1), then add them!",
            "words": ["gem"],
        },
    )

    register(
        4, 5,
        {
            "ctype": "choice-card", "skill": "twoStep",
            "prompt": "What number comes next in the skip-counting pattern?",
            "media": {
                "subtitle": "Pattern Path",
                "scenario": "Look at how each number grows along the stones. Which number replaces '?'?",
                "sequence": ["2", "4", "6", "8", "?"],
            },
            "correct": "10", "options": ["10", "9", "12", "11"],
            "hints": ["Each step adds 2 to the previous number.", "8 plus 2 equals 10!"],
            "success": "Great pattern sense! 2, 4, 6, 8, 10!",
            "retry": "Check how much is added at every step!",
        },
        {
            "ctype": "choice-card", "skill": "twoStep",
            "prompt": "Which color comes next in the bead pattern?",
            "media": {
                "subtitle": "Color Pattern",
                "obj": "obj_ball",
                "scenario": "Follow the repeating bead colors. What goes in the '?' spot?",
                "sequence": ["RED", "BLUE", "RED", "BLUE", "?"],
            },
            "correct": "RED", "options": ["RED", "BLUE", "GREEN"],
            "hints": ["Say the pattern aloud: red, blue, red, blue...", "After BLUE comes RED!"],
            "success": "Awesome! RED comes next in the pattern!",
            "retry": "Repeat RED, BLUE and see what follows BLUE!",
            "words": ["red", "blue"],
        },
        {
            "ctype": "choice-card", "skill": "twoStep",
            "prompt": "Discover the growing step rule and find '?'!",
            "media": {
                "subtitle": "Number Rule",
                "scenario": "Look at how the jump between numbers grows each time:",
                "sequence": ["1", "2", "4", "7", "11", "?"],
                "clues": ["Compare each jump: +1, +2, +3, +4... What is the next jump?"],
            },
            "correct": "16", "options": ["16", "15", "14", "17"],
            "hints": ["After +4 comes +5.", "11 plus 5 equals 16!"],
            "success": "Master pattern solver! 11 + 5 = 16!",
            "retry": "Check the difference between neighbors: 1, 2, 3, 4, then 5!",
        },
    )

    register(
        4, 6,
        {
            "ctype": "choice-card", "skill": "twoStep",
            "prompt": "Use the clues to find who finished FIRST!",
            "media": {
                "subtitle": "First Logic",
                "obj": "obj_crown",
                "scenario": "Three friends ran across Learning Island:",
                "clues": [
                    "Sam crossed the bridge before Mia.",
                    "Leo crossed the bridge after Mia.",
                    "Who finished FIRST?",
                ],
            },
            "correct": "Sam", "options": ["Sam", "Mia", "Leo", "Tie"],
            "hints": ["Put Sam ahead of Mia, and Leo behind Mia.", "The order is Sam, Mia, Leo - so Sam was first!"],
            "success": "Logical thinking! Sam finished first!",
            "retry": "Line up the three names from first to last!",
            "words": ["crown"],
        },
        {
            "ctype": "choice-card", "skill": "twoStep",
            "prompt": "Which container holds MORE apples?",
            "media": {
                "subtitle": "Compare Groups",
                "obj": "obj_basket",
                "scenario": "A basket holds 5 apples. A small box holds 2 apples. Which one has MORE apples?",
            },
            "correct": "Basket (5)", "options": ["Basket (5)", "Box (2)", "Same"],
            "hints": ["Compare the numbers 5 and 2.", "5 is bigger than 2, so the basket holds more!"],
            "success": "Nice! 5 apples is more than 2 apples!",
            "retry": "Pick the group with the larger number of apples!",
            "words": ["basket", "apple"],
        },
        {
            "ctype": "choice-card", "skill": "twoStep",
            "prompt": "Deduce which gem sits in the SECOND spot!",
            "media": {
                "subtitle": "Island Logic",
                "obj": "obj_gem",
                "scenario": "Four gems sit in a row from left to right:",
                "clues": [
                    "Red is somewhere to the left of Blue.",
                    "Green sits directly between Red and Blue.",
                    "Gold sits at the far right end.",
                    "Which gem is in the SECOND spot from the left?",
                ],
            },
            "correct": "Green", "options": ["Green", "Red", "Blue", "Gold"],
            "hints": ["Gold is 4th on the right, so Red, Green, Blue fill spots 1, 2, 3.", "The row is Red, Green, Blue, Gold - Green is second!"],
            "success": "Spot-on deduction! Red, Green, Blue, Gold puts Green second!",
            "retry": "Place Gold at the far right first, then fit Red-Green-Blue in order!",
            "words": ["gem"],
        },
    )

    # ---- Level 5: Art Studio (pixel-art: copy, symmetry, pattern) ----
    register(
        5, 4,
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Copy the glowing Gold Frame onto your canvas!",
            "media": {
                "gw": 4, "gh": 4, "artMode": "copy",
                "palette": ["#FF5A5A", "#FFC531"],
                "targetGrid": [
                    -1, 1, 1, -1,
                     1, 0, 0,  1,
                     1, 1, 1,  1,
                    -1, 1, 1, -1,
                ],
                "startGrid": [-1] * 16,
            },
            "correct": "done", "options": [],
            "hints": ["Pick yellow or red from Colors, then tap squares to match Goal.", "Copy row by row: outer gold ring with two red squares in row 2!"],
            "success": "Beautiful pixel art! Your copy matches the Goal!",
            "retry": "Check the glowing squares against the Goal grid!",
        },
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Copy the little Flower into the 3x3 grid!",
            "media": {
                "gw": 3, "gh": 3, "artMode": "copy",
                "palette": ["#1E88E5", "#FFC531"],
                "targetGrid": [
                    -1, 1, -1,
                     1, 0,  1,
                    -1, 1, -1,
                ],
                "startGrid": [
                    -1, -1, -1,
                    -1,  0, -1,
                    -1, -1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Select the yellow Color 2 swatch.", "Paint the 4 yellow petals around the blue center!"],
            "success": "Lovely flower! Every petal is in the right spot!",
            "retry": "Look at the Goal on the right and check the glowing squares!",
        },
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Recreate the 5x5 Crystal Emblem from the Goal!",
            "media": {
                "gw": 5, "gh": 5, "artMode": "copy",
                "palette": ["#7E5BEF", "#14B8A6", "#FFC531"],
                "targetGrid": [
                    -1, -1, 2, -1, -1,
                    -1,  0, 1,  0, -1,
                     0,  1, 2,  1,  0,
                    -1,  0, 1,  0, -1,
                    -1, -1, 2, -1, -1,
                ],
                "startGrid": [
                    -1, -1, 2, -1, -1,
                    -1, -1, -1, -1, -1,
                    -1, -1, 2, -1, -1,
                    -1, -1, -1, -1, -1,
                    -1, -1, 2, -1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Notice the diamond uses purple on the outer edge and teal inside.", "Keep white squares blank with Erase if you make a mistake!"],
            "success": "Masterpiece! That 5x5 crystal emblem is spotless!",
            "retry": "Compare row by row with the Goal card!",
        },
    )

    register(
        5, 5,
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Mirror the left side across the center line!",
            "media": {
                "gw": 4, "gh": 4, "artMode": "symmetry",
                "palette": ["#14B8A6", "#FF9F1C"],
                "targetGrid": [
                     0,  1,  1,  0,
                     1, -1, -1,  1,
                     0,  0,  0,  0,
                    -1,  1,  1, -1,
                ],
                "startGrid": [
                     0,  1, -1, -1,
                     1, -1, -1, -1,
                     0,  0, -1, -1,
                    -1,  1, -1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Column 3 mirrors Column 2, and Column 4 mirrors Column 1.", "Row 1 becomes teal, orange, orange, teal - mirror every row!"],
            "success": "Perfect mirror symmetry!",
            "retry": "Check the glowing squares to make left and right match as mirrors!",
        },
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Finish the mirror column on the right!",
            "media": {
                "gw": 3, "gh": 3, "artMode": "symmetry",
                "palette": ["#34A853", "#FF5A5A"],
                "targetGrid": [
                    0,  1, 0,
                    1, -1, 1,
                    0,  1, 0,
                ],
                "startGrid": [
                    0,  1, -1,
                    1, -1, -1,
                    0,  1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Look at the left column in each row.", "Copy the left column's color into the empty right column!"],
            "success": "Great mirror work! Both sides match!",
            "retry": "Match each right square to the square on the left side!",
        },
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Complete the 5x5 symmetric mosaic across the center spine!",
            "media": {
                "gw": 5, "gh": 5, "artMode": "symmetry",
                "palette": ["#1E88E5", "#FF6FA5", "#FFC531"],
                "targetGrid": [
                     0, -1, 2, -1,  0,
                     1,  0, 1,  0,  1,
                    -1,  2, 0,  2, -1,
                     1,  0, 1,  0,  1,
                     0, -1, 2, -1,  0,
                ],
                "startGrid": [
                     0, -1, 2, -1, -1,
                     1,  0, 1, -1, -1,
                    -1,  2, 0, -1, -1,
                     1,  0, 1, -1, -1,
                     0, -1, 2, -1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Column 4 mirrors Column 2; Column 5 mirrors Column 1.", "Leave blanks where the left side is blank!"],
            "success": "Precision symmetry! Gorgeous 5x5 mosaic!",
            "retry": "Check each pair of mirror columns around the middle column!",
        },
    )

    register(
        5, 6,
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Continue the checkerboard tile pattern!",
            "media": {
                "gw": 4, "gh": 4, "artMode": "pattern",
                "palette": ["#7E5BEF", "#FFC531"],
                "targetGrid": [
                    0, 1, 0, 1,
                    1, 0, 1, 0,
                    0, 1, 0, 1,
                    1, 0, 1, 0,
                ],
                "startGrid": [
                    0, 1, 0, 1,
                    1, 0, 1, 0,
                    0, 1, -1, -1,
                    1, -1, -1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Colors alternate purple-yellow-purple-yellow across rows and columns.", "No two touching squares share the same color!"],
            "success": "Awesome checkerboard! Pattern complete!",
            "retry": "Check the glowing squares so purple and yellow always alternate!",
        },
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Finish the striped color rows!",
            "media": {
                "gw": 3, "gh": 3, "artMode": "pattern",
                "palette": ["#1E88E5", "#FF9F1C"],
                "targetGrid": [
                    0, 0, 0,
                    1, 1, 1,
                    0, 0, 0,
                ],
                "startGrid": [
                    0, 0, 0,
                    1, 1, -1,
                    0, -1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Row 2 is all orange, and Row 3 is all blue.", "Fill the empty squares to finish each solid color stripe!"],
            "success": "Bright stripes! Well done!",
            "retry": "Match each square to the color already started on its row!",
        },
        {
            "ctype": "pixel-art", "skill": "art",
            "prompt": "Deduce the 3-color diagonal rule and finish the 5x5 grid!",
            "media": {
                "gw": 5, "gh": 5, "artMode": "pattern",
                "palette": ["#14B8A6", "#7E5BEF", "#FFC531"],
                "targetGrid": [
                    0, 1, 2, 0, 1,
                    1, 2, 0, 1, 2,
                    2, 0, 1, 2, 0,
                    0, 1, 2, 0, 1,
                    1, 2, 0, 1, 2,
                ],
                "startGrid": [
                    0, 1, 2, 0, 1,
                    1, 2, 0, 1, 2,
                    2, 0, 1, -1, -1,
                    0, 1, -1, -1, -1,
                    1, -1, -1, -1, -1,
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Look along diagonals going down-left: each diagonal uses one color!", "Across every row the cycle repeats: Teal -> Purple -> Gold!"],
            "success": "Brilliant pattern deduction! Every diagonal lines up!",
            "retry": "Follow the Teal -> Purple -> Gold cycle across each row!",
        },
    )

    # ---- Level 6: Science Lab (life-cycle / habitats / plants) ----
    register(
        6, 4,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What stage comes between a sprout and a full tree?",
            "media": {
                "subtitle": "Life Cycle",
                "obj": "obj_tree",
                "scenario": "Follow the plant life cycle in order. Which stage fills the '?' slot?",
                "sequence": ["Seed", "Sprout", "?", "Full Tree"],
            },
            "correct": "Young plant", "options": ["Young plant", "River pebble", "Snowflake", "Metal key"],
            "hints": ["After a tiny sprout grows leaves, it becomes a small growing plant.", "Seed -> Sprout -> Young plant -> Full Tree!"],
            "success": "Great science! A sprout grows into a young plant!",
            "retry": "Look at the order from tiny seed to big tree!",
            "words": ["tree"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What do green plants need to grow healthy?",
            "media": {
                "subtitle": "Plant Needs",
                "obj": "obj_tree",
                "scenario": "A little sapling stands in soil. What does it need from the sky to grow?",
            },
            "correct": "Sunlight and rain", "options": ["Sunlight and rain", "Toy blocks", "Ice cream"],
            "hints": ["Plants drink water and soak up light.", "Sunlight and rain help plants make food and grow!"],
            "success": "Yes! Plants need sunlight and rain!",
            "retry": "Think about what helps flowers and trees grow outside!",
            "words": ["tree"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Identify the plant part described in the Science Lab notes!",
            "media": {
                "subtitle": "Botany Clues",
                "obj": "obj_apple",
                "scenario": "Read all clues from the lab notebook:",
                "clues": [
                    "This part grows underground out of sight.",
                    "It anchors the plant firmly in the soil.",
                    "It absorbs water and dissolved minerals.",
                ],
            },
            "correct": "Roots", "options": ["Roots", "Petals", "Fruit skin", "Pollen"],
            "hints": ["Which part of a tree stretches beneath the dirt?", "Roots hold the plant and drink up water!"],
            "success": "Spot on! Roots anchor the plant and absorb water!",
            "retry": "Look at the clue about growing underground!",
            "words": ["apple"],
        },
    )

    register(
        6, 5,
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Match each living thing with its natural habitat!",
            "media": {
                "pairs": [
                    pair("fish", a_label="FISH", a_sprite="obj_fish", b_label="Ocean Reef"),
                    pair("bird", a_label="BIRD", a_sprite="obj_bird", b_label="Forest Canopy"),
                    pair("tree", a_label="TREE", a_sprite="obj_tree", b_label="Sunny Meadow"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Think where each living thing lives.", "Fish -> Ocean Reef, Bird -> Forest Canopy, Tree -> Sunny Meadow!"],
            "success": "Great habitat matching! Every organism is home!",
            "retry": "Connect each creature or plant to where it thrives!",
            "words": ["fish", "bird", "tree"],
        },
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Find the matching nature pairs in the lab!",
            "media": {
                "pairs": [
                    pair("tree", a_label="TREE", a_sprite="obj_tree"),
                    pair("fish", a_label="FISH", a_sprite="obj_fish"),
                    pair("sun", a_label="SUN", a_sprite="obj_sun"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Flip two cards and look for identical pictures.", "Match tree with tree, fish with fish, and sun with sun!"],
            "success": "Nice memory! You found all three nature pairs!",
            "retry": "Remember where each picture was hiding!",
            "words": ["tree", "fish", "sun"],
        },
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Match each ecosystem role with its example!",
            "media": {
                "pairs": [
                    pair("prod", a_label="Producer", b_label="Green Plant"),
                    pair("decomp", a_label="Decomposer", b_label="Mushroom"),
                    pair("herb", a_label="Herbivore", b_label="Rabbit"),
                    pair("carn", a_label="Carnivore", b_label="Hawk"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Plants make food from sun; mushrooms recycle dead matter.", "Rabbits eat plants (herbivore); hawks hunt prey (carnivore)!"],
            "success": "Ecosystem mastery! Every role matched!",
            "retry": "Think about how each organism gets its energy!",
        },
    )

    register(
        6, 6,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What happens when liquid water freezes in the cold lab?",
            "media": {
                "subtitle": "Matter Change",
                "obj": "obj_cauldron",
                "scenario": "We cool liquid water below 0 degrees Celsius. What state does it become?",
                "sequence": ["Liquid water", "Cools & freezes", "?"],
            },
            "correct": "Solid ice", "options": ["Solid ice", "Hot steam", "Dry sand", "Wood ash"],
            "hints": ["Think about an ice cube tray in a freezer.", "Frozen water turns into solid ice!"],
            "success": "Correct! Cooling water below freezing turns it into solid ice!",
            "retry": "Remember what happens to water inside a freezer!",
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What emerges from a butterfly's chrysalis?",
            "media": {
                "subtitle": "Life Cycle",
                "obj": "obj_bird",
                "scenario": "Follow the insect life cycle. What grows inside the chrysalis?",
                "sequence": ["Egg", "Caterpillar", "Chrysalis", "?"],
            },
            "correct": "Butterfly", "options": ["Butterfly", "Puppy", "Goldfish"],
            "hints": ["A caterpillar changes inside a chrysalis to get wings.", "An adult butterfly comes out!"],
            "success": "Hooray! A caterpillar transforms into a butterfly!",
            "retry": "Think about what flies out after the caterpillar stage!",
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Name the phase change observed on the cool beaker lid!",
            "media": {
                "subtitle": "Lab Inference",
                "obj": "obj_cauldron",
                "scenario": "Deduce which phase change is happening in the experiment:",
                "clues": [
                    "Warm water evaporates into invisible water vapor.",
                    "The vapor rises and touches a chilled glass lid.",
                    "Tiny liquid water droplets form on the underside of the lid.",
                ],
            },
            "correct": "Condensation", "options": ["Condensation", "Evaporation", "Melting", "Erosion"],
            "hints": ["Gas turning back into liquid droplets when cooled has a special name.", "Vapor cooling back into liquid droplets is condensation!"],
            "success": "Great scientific reasoning! That is condensation!",
            "retry": "Watch the direction: gas cooling back into liquid droplets!",
        },
    )

    # ---- Level 7: Sky Kingdom (weather / clouds / storms) ----
    register(
        7, 4,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Which clouds bring steady rain across Sky Kingdom?",
            "media": {
                "subtitle": "Cloud Watch",
                "obj": "obj_sun",
                "scenario": "Thick, dark gray clouds blanket the whole sky right before steady rain falls. What are rain-bearing clouds called?",
            },
            "correct": "Nimbus clouds", "options": ["Nimbus clouds", "Clear blue sky", "Rainbow mist", "Moonlight"],
            "hints": ["'Nimbus' means rain cloud in Latin.", "Nimbus clouds carry heavy water droplets for rain!"],
            "success": "Right! Nimbus clouds bring steady rain!",
            "retry": "Look for the scientific cloud name!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What should you carry when dark rain clouds pour?",
            "media": {
                "subtitle": "Rainy Weather",
                "obj": "obj_sun",
                "scenario": "Raindrops are falling from gray clouds! What keeps you dry on your walk?",
            },
            "correct": "Umbrella", "options": ["Umbrella", "Sunglasses", "Sandals"],
            "hints": ["You hold this open over your head in the rain.", "An umbrella blocks falling raindrops!"],
            "success": "Smart choice! An umbrella keeps you dry!",
            "retry": "Pick the item made for rainy weather!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Identify the cloud type from the weather balloon log!",
            "media": {
                "subtitle": "Cloud Classification",
                "obj": "obj_sun",
                "scenario": "Use all three altitude and shape clues:",
                "clues": [
                    "These clouds float very high above 6,000 meters.",
                    "They look thin, feathery, and wispy in the sky.",
                    "They are made of tiny ice crystals rather than liquid drops.",
                ],
            },
            "correct": "Cirrus", "options": ["Cirrus", "Stratus", "Fog", "Cumulonimbus"],
            "hints": ["Think of the highest, feathery ice-crystal clouds.", "Cirrus clouds look like wispy horse-tails high in the sky!"],
            "success": "Meteorologist level! High wispy ice clouds are Cirrus!",
            "retry": "Focus on altitude (6,000 m) and wispy ice crystals!",
            "words": ["sun"],
        },
    )

    register(
        7, 5,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Why do we see lightning before we hear thunder?",
            "media": {
                "subtitle": "Storm Science",
                "obj": "obj_star",
                "scenario": "Lightning flashes in the sky and thunder booms a few seconds later. Why does the flash arrive first?",
            },
            "correct": "Light travels faster than sound",
            "options": [
                "Light travels faster than sound",
                "Thunder begins five minutes later",
                "Clouds absorb every flash",
                "Rain slows down the light",
            ],
            "hints": ["Both happen at the same instant, so compare speed.", "Light races much faster through air than sound waves!"],
            "success": "Brilliant! Light reaches your eyes far faster than sound reaches your ears!",
            "retry": "Compare how fast light travels versus how fast sound travels!",
            "words": ["star"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Where is the safest place during a thunderstorm?",
            "media": {
                "subtitle": "Storm Safety",
                "obj": "obj_house",
                "scenario": "You hear loud thunder and see lightning flashes. Where should you go right away?",
            },
            "correct": "Inside a sturdy house", "options": ["Inside a sturdy house", "Under a tall tree", "In an open pool"],
            "hints": ["Stay away from tall trees and open water.", "Going inside a sturdy house keeps you safe!"],
            "success": "Safe choice! Indoors in a sturdy house is safest!",
            "retry": "Avoid tall trees and water during lightning!",
            "words": ["house"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Predict the weather event from the Sky Station sensors!",
            "media": {
                "subtitle": "Weather Station",
                "obj": "obj_star",
                "scenario": "Examine the three sensor readings:",
                "clues": [
                    "Air pressure drops sharply in two hours.",
                    "Warm humid air surges upward into cold upper air.",
                    "Tall anvil-shaped cumulonimbus clouds form overhead.",
                ],
            },
            "correct": "Thunderstorm", "options": ["Thunderstorm", "Calm drought", "Light morning dew", "Clear heatwave"],
            "hints": ["Cumulonimbus anvil clouds and rapid updrafts bring heavy weather.", "Those conditions produce a thunderstorm!"],
            "success": "Accurate forecast! Those sensors signal a thunderstorm!",
            "retry": "Check what cumulonimbus clouds and dropping pressure produce!",
            "words": ["star"],
        },
    )

    register(
        7, 6,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What two ingredients create a rainbow in the sky?",
            "media": {
                "subtitle": "Sky Magic",
                "obj": "obj_sun",
                "scenario": "Complete the rainbow recipe in Sky Kingdom:",
                "sequence": ["Sunlight", "+", "?", "=", "Rainbow"],
            },
            "correct": "Raindrops", "options": ["Raindrops", "Snowdrifts", "Thunderclaps", "Starlight"],
            "hints": ["Sunlight bends and splits into colors when shining through water droplets.", "Sunlight + Raindrops = Rainbow!"],
            "success": "Colorful science! Sunlight shining through raindrops makes a rainbow!",
            "retry": "Think about what is falling in the air while the sun shines!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What invisible force pushes kites high and bends tree leaves?",
            "media": {
                "subtitle": "Windy Day",
                "obj": "obj_tree",
                "scenario": "Tree branches sway and kites soar across the sky. What moving air pushes them?",
            },
            "correct": "Wind", "options": ["Wind", "Frost", "Shadow"],
            "hints": ["Moving air across the sky is called...", "Wind makes kites fly and leaves rustle!"],
            "success": "Great job! Wind is moving air!",
            "retry": "Pick the weather word for moving air!",
            "words": ["tree"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Name the final falling stage of the water cycle!",
            "media": {
                "subtitle": "Water Cycle",
                "obj": "obj_sun",
                "scenario": "Trace the water cycle and name the missing final step:",
                "clues": [
                    "1. Sun warms ocean water so it evaporates into vapor.",
                    "2. Vapor cools high in the sky and condenses into clouds.",
                    "3. Heavy water drops or ice fall back to Earth as rain or snow.",
                ],
            },
            "correct": "Precipitation", "options": ["Precipitation", "Evaporation", "Collection", "Friction"],
            "hints": ["Rain, snow, sleet, and hail falling from clouds share one scientific name.", "Water falling from clouds is precipitation!"],
            "success": "Excellent! Rain and snow falling to Earth is precipitation!",
            "retry": "Step 1 is evaporation, Step 2 is condensation; name Step 3!",
            "words": ["sun"],
        },
    )

    # ---- Level 8: Ocean Reef (sea-life / reef-habitat) ----
    register(
        8, 4,
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "What do a fish's gills take from ocean water?",
            "media": {
                "subtitle": "Reef Words",
                "obj": "obj_fish",
                "scenario": "A fish swims through Ocean Reef using fins to steer and gills to breathe. What gas do gills absorb from water?",
            },
            "correct": "Oxygen", "options": ["Oxygen", "Sand", "Pebbles", "Seaweed"],
            "hints": ["Just like our lungs breathe oxygen from air, gills pull it from water.", "Fish breathe dissolved oxygen!"],
            "success": "Great reef biology! Fish absorb oxygen with their gills!",
            "retry": "Think about what living animals must breathe to survive!",
            "words": ["fish"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Which body part helps a fish steer through water?",
            "media": {
                "subtitle": "Sea Animal",
                "obj": "obj_fish",
                "scenario": "Look at the fish gliding over the reef. What helps it swim and turn?",
            },
            "correct": "Fins", "options": ["Fins", "Wings", "Hooves"],
            "hints": ["Birds have wings, horses have hooves, and fish have...", "Fish swim and steer with fins!"],
            "success": "Awesome! Fish use fins to swim!",
            "retry": "Think about the body part on a fish's back and sides!",
            "words": ["fish"],
        },
        {
            "ctype": "choice-card", "skill": "vocabulary",
            "prompt": "Identify the Ocean Reef vocabulary term from the diver's log!",
            "media": {
                "subtitle": "Reef Vocabulary",
                "obj": "obj_fish",
                "scenario": "Read the three marine log clues:",
                "clues": [
                    "It is built over centuries by tiny marine polyps.",
                    "It thrives in warm, shallow, sunlit tropical seas.",
                    "It shelters thousands of species of fish, crabs, and sponges.",
                ],
            },
            "correct": "Coral reef", "options": ["Coral reef", "Abyssal trench", "Polar glacier", "Salt desert"],
            "hints": ["Shallow sunlit waters full of polyps build colorful underwater ridges.", "That vibrant habitat is a coral reef!"],
            "success": "Spot on! Coral reefs shelter amazing sea life!",
            "retry": "Check the clues about shallow sunlit water and marine polyps!",
            "words": ["fish"],
        },
    )

    register(
        8, 5,
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Match each ocean creature to its reef adaptation!",
            "media": {
                "pairs": [
                    pair("fish", a_label="Fish", a_sprite="obj_fish", b_label="Breathes with gills"),
                    pair("turtle", a_label="Sea Turtle", b_label="Hard protective shell"),
                    pair("coral", a_label="Coral", b_label="Living reef shelter"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Think about each sea creature's special feature.", "Fish -> gills, Sea Turtle -> shell, Coral -> reef shelter!"],
            "success": "Ocean explorer! Every reef match is complete!",
            "retry": "Match each sea creature with what helps it live on the reef!",
            "words": ["fish"],
        },
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Match the hidden sea pictures under the waves!",
            "media": {
                "pairs": [
                    pair("fish", a_label="FISH", a_sprite="obj_fish"),
                    pair("star", a_label="STARFISH", a_sprite="obj_star"),
                    pair("chest", a_label="TREASURE", a_sprite="obj_chest"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Tap two cards to peek below the waves.", "Find both fish, both starfish, and both treasure chests!"],
            "success": "Splendid! You matched all ocean pairs!",
            "retry": "Remember where each picture swam!",
            "words": ["fish", "star", "chest"],
        },
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Match each reef organism with its food-web role!",
            "media": {
                "pairs": [
                    pair("kelp", a_label="Kelp", b_label="Sunlight Producer"),
                    pair("parrot", a_label="Parrotfish", b_label="Algae Grazer"),
                    pair("octopus", a_label="Octopus", b_label="Camouflage Hunter"),
                    pair("shrimp", a_label="Cleaner Shrimp", b_label="Reef Helper"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Kelp uses sunlight; cleaner shrimp clean other fish.", "Parrotfish graze algae; octopuses hunt using camouflage!"],
            "success": "Reef biologist! You matched the entire food web!",
            "retry": "Connect each creature to how it survives on the reef!",
        },
    )

    register(
        8, 6,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Why do small reef fish hide among coral branches?",
            "media": {
                "subtitle": "Tide Pool",
                "obj": "obj_star",
                "scenario": "Little fish dart into rocky cracks and coral branches whenever bigger shadows pass overhead. Why?",
            },
            "correct": "To stay safe from predators",
            "options": [
                "To stay safe from predators",
                "To fly above the clouds",
                "To build snow forts",
                "To climb pine trees",
            ],
            "hints": ["Think about why small fish hide from bigger hungry fish.", "Coral gives tiny fish shelter from predators!"],
            "success": "Correct! Coral reefs act as safe shelters!",
            "retry": "Choose the ocean survival reason!",
            "words": ["star", "fish"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Where does a clownfish make its home?",
            "media": {
                "subtitle": "Ocean Home",
                "obj": "obj_fish",
                "scenario": "A bright clownfish lives beside sea anemones on a colorful reef. Where is its habitat?",
            },
            "correct": "In the ocean", "options": ["In the ocean", "In a dry desert", "On a snowy peak"],
            "hints": ["Fish need water all around them.", "Clownfish live in warm ocean reefs!"],
            "success": "Yes! Clownfish live in the ocean!",
            "retry": "Pick the habitat filled with saltwater!",
            "words": ["fish"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Predict the chain reaction in the kelp forest reef!",
            "media": {
                "subtitle": "Reef Balance",
                "obj": "obj_fish",
                "scenario": "Use the ecological clues to predict what happens next:",
                "clues": [
                    "Sea urchins eat kelp blades very quickly.",
                    "Sea otters eat sea urchins and keep their numbers balanced.",
                    "If all sea otters leave the reef, what happens first?",
                ],
            },
            "correct": "Urchins multiply and kelp shrinks",
            "options": [
                "Urchins multiply and kelp shrinks",
                "Kelp grows taller overnight",
                "Coral turns into solid ice",
                "Sea urchins learn to fly",
            ],
            "hints": ["Without predators eating them, sea urchin numbers rise.", "More urchins eat much more kelp, shrinking the kelp forest!"],
            "success": "Great food-web deduction! Every species keeps balance!",
            "retry": "Follow the cause and effect: fewer otters -> more urchins -> less kelp!",
            "words": ["fish"],
        },
    )

    # ---- Level 9: Puzzle Park (directions & logic-maze/sequence) ----
    register(
        9, 4,
        {
            "ctype": "directions", "skill": "directions",
            "prompt": "Move 3 steps RIGHT, then 3 steps UP to open the park chest!",
            "media": {
                "gw": 5, "gh": 5, "sx": 0, "sy": 0, "tx": 3, "ty": 3,
                "moves": [{"dir": "right", "n": 3}, {"dir": "up", "n": 3}],
                "collect": 0, "use": 0,
            },
            "correct": "target", "options": [],
            "hints": ["Tap Right 3 times first.", "Now tap Up 3 times to reach (3, 3)!"],
            "success": "Awesome navigation! You reached the park treasure!",
            "retry": "Follow the grid coordinates step by step!",
        },
        {
            "ctype": "directions", "skill": "directions",
            "prompt": "Move 2 steps RIGHT, then 1 step UP!",
            "media": {
                "gw": 3, "gh": 3, "sx": 0, "sy": 0, "tx": 2, "ty": 1,
                "moves": [{"dir": "right", "n": 2}, {"dir": "up", "n": 1}],
                "collect": 0, "use": 0,
            },
            "correct": "target", "options": [],
            "hints": ["Tap Right, Right to cross the bottom row.", "Then tap Up 1 time to grab the chest!"],
            "success": "Way to go! You found the chest!",
            "retry": "Watch your step count on Right and Up!",
        },
        {
            "ctype": "directions", "skill": "directions",
            "prompt": "Navigate the multi-turn maze: 2 DOWN, 3 RIGHT, then 1 DOWN!",
            "media": {
                "gw": 5, "gh": 5, "sx": 1, "sy": 4, "tx": 4, "ty": 1,
                "moves": [{"dir": "down", "n": 2}, {"dir": "right", "n": 3}, {"dir": "down", "n": 1}],
                "collect": 0, "use": 0,
            },
            "correct": "target", "options": [],
            "hints": ["Start with 2 Down, then 3 Right.", "Finish with 1 more Down to land on (4, 1)!"],
            "success": "Expert pathfinder! Every turn was exact!",
            "retry": "Track each move carefully: Down 2, Right 3, Down 1!",
        },
    )

    register(
        9, 5,
        {
            "ctype": "choice-card", "skill": "directions",
            "prompt": "Where does the Puzzle Park path finish?",
            "media": {
                "subtitle": "Maze Path",
                "obj": "obj_key",
                "scenario": "Start at (0, 0). Walk 2 steps North (up) and then 1 step East (right). Which coordinate do you reach?",
                "sequence": ["(0, 0)", "2 North", "1 East", "?"],
            },
            "correct": "(1, 2)", "options": ["(1, 2)", "(2, 1)", "(1, 1)", "(2, 2)"],
            "hints": ["East changes the first number (x); North changes the second number (y).", "x = 1 and y = 2 gives (1, 2)!"],
            "success": "Spot on! 1 East and 2 North is (1, 2)!",
            "retry": "Remember (x, y): East is x, North is y!",
            "words": ["key"],
        },
        {
            "ctype": "choice-card", "skill": "directions",
            "prompt": "Which arrow points to the RIGHT?",
            "media": {
                "subtitle": "Park Turn",
                "obj": "obj_key",
                "scenario": "Lumo sees the golden key gate to the RIGHT. Choose the arrow pointing RIGHT!",
            },
            "correct": "Right ->", "options": ["Right ->", "Left <-", "Up ^"],
            "hints": ["Look at the tip of each arrow.", "The arrow pointing toward the right side is Right ->!"],
            "success": "Great job! Right -> points the way!",
            "retry": "Check which way the arrow tip points!",
            "words": ["key"],
        },
        {
            "ctype": "choice-card", "skill": "directions",
            "prompt": "Track the park drone's turns to find its final heading!",
            "media": {
                "subtitle": "Compass Logic",
                "obj": "obj_key",
                "scenario": "Trace every rotation from start to finish:",
                "clues": [
                    "The drone begins facing North.",
                    "It turns 90 degrees right, flies forward, then turns 90 degrees right again.",
                    "Finally, it turns 180 degrees all the way around.",
                    "Which direction is the drone facing now?",
                ],
            },
            "correct": "North", "options": ["North", "South", "East", "West"],
            "hints": ["Two 90-degree right turns from North leave it facing South.", "Turning 180 degrees from South faces North again!"],
            "success": "Sharp spatial logic! North -> East -> South -> North!",
            "retry": "Walk through each turn: 90R, 90R, then 180!",
            "words": ["key"],
        },
    )

    register(
        9, 6,
        {
            "ctype": "choice-card", "skill": "directions",
            "prompt": "Find the missing step in the repeating compass sequence!",
            "media": {
                "subtitle": "Arrow Sequence",
                "scenario": "Look at the repeating loop of directions. What replaces '?'?",
                "sequence": ["UP", "RIGHT", "DOWN", "UP", "RIGHT", "?"],
            },
            "correct": "DOWN", "options": ["DOWN", "UP", "LEFT", "RIGHT"],
            "hints": ["The 3-step cycle is UP, RIGHT, DOWN.", "After UP and RIGHT comes DOWN!"],
            "success": "Pattern solved! DOWN completes the loop!",
            "retry": "Repeat the 3-step cycle: UP, RIGHT, DOWN!",
        },
        {
            "ctype": "choice-card", "skill": "directions",
            "prompt": "Which direction finishes the pattern?",
            "media": {
                "subtitle": "Arrow Pattern",
                "scenario": "Look at the bouncing pattern. What comes next?",
                "sequence": ["UP", "DOWN", "UP", "?"],
            },
            "correct": "DOWN", "options": ["DOWN", "UP", "LEFT"],
            "hints": ["Say it aloud: UP, DOWN, UP...", "DOWN comes right after UP!"],
            "success": "Awesome! UP, DOWN, UP, DOWN!",
            "retry": "Follow the alternating up-and-down rhythm!",
        },
        {
            "ctype": "choice-card", "skill": "directions",
            "prompt": "Deduce the next coded step in the spiral path!",
            "media": {
                "subtitle": "Rule Finder",
                "scenario": "Each move rotates 90 degrees clockwise and adds 1 more pace:",
                "sequence": ["1 North", "2 East", "3 South", "?"],
            },
            "correct": "4 West", "options": ["4 West", "4 North", "3 West", "5 East"],
            "hints": ["After 1, 2, 3 comes 4; clockwise after South is West.", "Combine both rules: 4 West!"],
            "success": "Master decoder! 4 West continues the spiral!",
            "retry": "Check both the number (+1) and the clockwise compass order (N->E->S->W)!",
        },
    )

    # ---- Level 10: Space Station (planets / solar-system) ----
    register(
        10, 4,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Which planet orbits closest to the Sun?",
            "media": {
                "subtitle": "Solar System",
                "obj": "obj_sun",
                "scenario": "Look at the inner planets lined up outward from the Sun. Which planet sits in the '?' spot?",
                "sequence": ["Sun", "?", "Venus", "Earth", "Mars"],
            },
            "correct": "Mercury", "options": ["Mercury", "Jupiter", "Saturn", "Neptune"],
            "hints": ["It is the smallest rocky planet right beside the Sun.", "Mercury is the first planet from the Sun!"],
            "success": "Blast off! Mercury is closest to the Sun!",
            "retry": "Think of the first planet before Venus and Earth!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What is the name of our home planet?",
            "media": {
                "subtitle": "Our Planet",
                "obj": "obj_sun",
                "scenario": "From the Space Station window, you see blue oceans, green continents, and swirling white clouds. Which planet is our home?",
            },
            "correct": "Earth", "options": ["Earth", "Mars", "Saturn"],
            "hints": ["We live, breathe, and grow trees on this blue planet.", "Our home planet is Earth!"],
            "success": "Yes! Earth is our blue ocean home!",
            "retry": "Choose the planet with oceans and air for us!",
            "words": ["sun"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Deduce the mystery planet from Space Probe telemetry!",
            "media": {
                "subtitle": "Orbital Logic",
                "obj": "obj_star",
                "scenario": "Use all three probe clues to identify the planet:",
                "clues": [
                    "It orbits farther from the Sun than the asteroid belt.",
                    "It is a gas giant famous for bright rings of ice and rock.",
                    "It is the sixth planet from the Sun.",
                ],
            },
            "correct": "Saturn", "options": ["Saturn", "Mercury", "Venus", "Uranus"],
            "hints": ["Count outward: Mercury, Venus, Earth, Mars, Jupiter, ...", "The sixth planet with bright icy rings is Saturn!"],
            "success": "Mission accomplished! Saturn is planet number six!",
            "retry": "Check the sixth planet and its famous icy rings!",
            "words": ["star"],
        },
    )

    register(
        10, 5,
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Match each space object with its description!",
            "media": {
                "pairs": [
                    pair("sun", a_label="Sun", a_sprite="obj_sun", b_label="Center Star"),
                    pair("mars", a_label="Mars", b_label="Red Planet"),
                    pair("saturn", a_label="Saturn", b_label="Icy Rings"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Pair each planet or star with its famous trait.", "Sun -> Center Star, Mars -> Red Planet, Saturn -> Icy Rings!"],
            "success": "Stellar memory! Space pairs locked!",
            "retry": "Remember where each space card sits!",
            "words": ["sun"],
        },
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Match the glowing space symbols by memory!",
            "media": {
                "pairs": [
                    pair("sun", a_label="SUN", a_sprite="obj_sun"),
                    pair("star", a_label="STAR", a_sprite="obj_star"),
                    pair("crystal", a_label="CRYSTAL", a_sprite="obj_crystal"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Flip two cards at a time to locate matching space pictures.", "Match both suns, both stars, and both crystals!"],
            "success": "Great space memory!",
            "retry": "Keep scanning the grid and remember each flip!",
            "words": ["sun", "star", "crystal"],
        },
        {
            "ctype": "memory-match", "skill": "science",
            "prompt": "Pair each astrophysics term with its definition!",
            "media": {
                "pairs": [
                    pair("orbit", a_label="Orbit", b_label="Curved path around star"),
                    pair("gravity", a_label="Gravity", b_label="Pulls masses together"),
                    pair("comet", a_label="Comet", b_label="Icy tail traveler"),
                    pair("moon", a_label="Moon", b_label="Natural satellite"),
                ],
            },
            "correct": "done", "options": [],
            "hints": ["Match terms with what they do in space.", "Orbit=curved path, Gravity=pull, Comet=icy traveler, Moon=satellite!"],
            "success": "Space Station commander! Every concept paired!",
            "retry": "Read each clue carefully and remember card spots!",
        },
    )

    register(
        10, 6,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Why do astronauts float inside the Space Station?",
            "media": {
                "subtitle": "Space Science",
                "obj": "obj_star",
                "scenario": "Astronauts onboard the orbiting Space Station drift weightlessly through the cabin. What explains this?",
            },
            "correct": "They are in free-fall orbit",
            "options": [
                "They are in free-fall orbit",
                "Their boots have tiny wings",
                "Sunlight pushes them upward",
                "Stars pull them to the roof",
            ],
            "hints": ["Earth's gravity still pulls on the station, keeping it curved in orbit.", "The station and astronauts fall together around Earth in free-fall orbit!"],
            "success": "Awesome physics! Orbit is continuous free-fall around Earth!",
            "retry": "Think about how an orbiting station moves around Earth!",
            "words": ["star"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What bright sky neighbor shines by reflecting sunlight at night?",
            "media": {
                "subtitle": "Night Sky",
                "obj": "obj_star",
                "scenario": "You look out at the night sky and see a glowing round neighbor orbiting Earth. What is it?",
            },
            "correct": "The Moon", "options": ["The Moon", "An Airplane", "A Raincloud"],
            "hints": ["It travels around Earth and changes shape through phases.", "The Moon shines with reflected sunlight!"],
            "success": "Right! The Moon orbits Earth!",
            "retry": "Pick the natural space neighbor that circles Earth!",
            "words": ["star"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Deduce which motion causes Earth's four seasons!",
            "media": {
                "subtitle": "Day & Year",
                "obj": "obj_sun",
                "scenario": "Compare the Space Station's planetary notes:",
                "clues": [
                    "Earth spins once on its axis every 24 hours, causing day and night.",
                    "Earth completes one trip around the Sun every 365 days on a tilted axis.",
                    "Which motion (with Earth's tilt) creates spring, summer, fall, and winter?",
                ],
            },
            "correct": "Revolution around the Sun",
            "options": [
                "Revolution around the Sun",
                "Spinning once in 24 hours",
                "Moon phases every month",
                "Shooting stars at night",
            ],
            "hints": ["Day and night take 24 hours; seasons change over a full year.", "Earth's yearly revolution around the Sun with its tilt makes the seasons!"],
            "success": "Astrophysics pro! Yearly revolution plus tilt makes the seasons!",
            "retry": "Check which motion takes a whole year instead of 24 hours!",
            "words": ["sun"],
        },
    )

    # ---- Level 11: Kindness Corner (feelings / manners / sharing) ----
    register(
        11, 4,
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "Notice Maya's feelings and pick the kindest response!",
            "media": {
                "subtitle": "Feelings",
                "obj": "obj_dog",
                "scenario": "Maya spilled her paint tray by mistake and tears well up in her eyes. How is Maya feeling, and what helps most?",
            },
            "correct": "Sad - offer to help clean up",
            "options": [
                "Sad - offer to help clean up",
                "Angry - hide her paintbrushes",
                "Sleepy - walk out quietly",
                "Proud - point and laugh",
            ],
            "hints": ["Tears after a spill show sadness.", "Offering a helping hand cheers her up and solves the mess!"],
            "success": "Warm heart! Helping a sad friend makes both of you stronger!",
            "retry": "Look at Maya's tears and choose the caring helper response!",
            "words": ["dog"],
        },
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "What is the kind thing to do when a friend drops crayons?",
            "media": {
                "subtitle": "Helping Hands",
                "obj": "obj_cat",
                "scenario": "Your friend's crayon box falls and crayons roll everywhere. What should you do?",
            },
            "correct": "Help pick them up", "options": ["Help pick them up", "Step over them", "Hide a crayon"],
            "hints": ["Imagine how you feel when your crayons spill.", "Kneeling down to help pick them up is kind!"],
            "success": "So kind! Helping friends shows great care!",
            "retry": "Pick the choice that helps your friend!",
            "words": ["cat"],
        },
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "Read the social clues and choose the most empathetic action!",
            "media": {
                "subtitle": "Empathy Clues",
                "obj": "obj_house",
                "scenario": "Consider how Tariq feels in this new situation:",
                "clues": [
                    "Tariq just joined the school today and sits alone at lunch.",
                    "He looks toward a friendly table laughing, then looks back down quietly.",
                    "Which action helps Tariq feel welcome without embarrassing him?",
                ],
            },
            "correct": "Invite him warmly to join your table",
            "options": [
                "Invite him warmly to join your table",
                "Announce loudly that he looks lonely",
                "Stare at him from across the room",
                "Ignore him so he stays quiet",
            ],
            "hints": ["Put yourself in a new student's shoes.", "A quiet, warm invitation makes him feel safe and included!"],
            "success": "Deep empathy! A warm invite builds real belonging!",
            "retry": "Pick the response that respects Tariq's feelings and welcomes him!",
            "words": ["house"],
        },
    )

    register(
        11, 5,
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "Which polite response shows gratitude?",
            "media": {
                "subtitle": "Good Manners",
                "obj": "obj_basket",
                "scenario": "Grandpa hands you a basket of fresh apples he picked especially for you. What do you say?",
            },
            "correct": "Thank you so much!",
            "options": ["Thank you so much!", "Give me more right now!", "I did not ask for this.", "Hurry up next time!"],
            "hints": ["Gratitude means showing appreciation for someone's kindness.", "Saying 'Thank you so much!' warms Grandpa's heart!"],
            "success": "Wonderful manners! Saying thank you spreads joy!",
            "retry": "Choose the grateful, respectful words!",
            "words": ["basket", "apple"],
        },
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "Which polite word do we use when asking to borrow something?",
            "media": {
                "subtitle": "Polite Words",
                "obj": "obj_apple",
                "scenario": "You would like to borrow your classmate's green pencil. What magic polite word helps you ask?",
            },
            "correct": "Please", "options": ["Please", "Mine", "Stop"],
            "hints": ["May I borrow your pencil, ______?", "Adding 'Please' makes every request polite!"],
            "success": "Lovely manners! 'Please' is always kind!",
            "retry": "Think of the polite asking word!",
            "words": ["apple"],
        },
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "Resolve the team disagreement with respectful manners!",
            "media": {
                "subtitle": "Respectful Teamwork",
                "obj": "obj_gem",
                "scenario": "Two teammates have different ideas for the class science poster:",
                "clues": [
                    "One teammate wants clear labeled diagrams; the other wants bright hand-drawn art.",
                    "Both ideas could help readers understand the poster.",
                    "What shows respectful teamwork?",
                ],
            },
            "correct": "Listen to both and combine their strengths",
            "options": [
                "Listen to both and combine their strengths",
                "Insist only your own idea works",
                "Refuse to talk to your group",
                "Erase your teammate's sketches",
            ],
            "hints": ["Respect means valuing everyone's voice.", "Bright art AND clear labels together make the best poster!"],
            "success": "True leadership! Listening and blending ideas respects everyone!",
            "retry": "Pick the choice that respects both teammates!",
            "words": ["gem"],
        },
    )

    register(
        11, 6,
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "Choose the fairest way to share one ball with four friends!",
            "media": {
                "subtitle": "Sharing",
                "obj": "obj_ball",
                "scenario": "Four friends arrive at Kindness Corner to play, and there is only one ball. What is the fairest plan?",
            },
            "correct": "Play a game where everyone takes turns",
            "options": [
                "Play a game where everyone takes turns",
                "Keep the ball only for yourself",
                "Take the ball home early",
                "Hide the ball behind a bush",
            ],
            "hints": ["Fair sharing means nobody gets left out.", "Taking turns lets all four friends play together!"],
            "success": "Fair and kind! Taking turns makes playtime fun for all!",
            "retry": "Pick the plan where all four friends get to join!",
            "words": ["ball"],
        },
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "How can you share your snacks kindly?",
            "media": {
                "subtitle": "Sharing",
                "obj": "obj_banana",
                "scenario": "You have 2 sweet bananas and your friend forgot to pack a snack. What is a kind choice?",
            },
            "correct": "Share one banana with them",
            "options": ["Share one banana with them", "Hide both bananas", "Toss one away"],
            "hints": ["Sharing one leaves both of you with a snack!", "Giving one banana lets both friends enjoy snack time!"],
            "success": "Kind sharer! Now you both have a yummy banana!",
            "retry": "Choose the option where both friends get a snack!",
            "words": ["banana"],
        },
        {
            "ctype": "choice-card", "skill": "kindness",
            "prompt": "Solve the Kindness Corner fairness puzzle!",
            "media": {
                "subtitle": "Fairness Puzzle",
                "obj": "obj_coin",
                "scenario": "Four volunteers earned 12 garden tokens together:",
                "clues": [
                    "All four volunteers worked equally hard for the hour.",
                    "Two tokens were spent on flower seeds for the shared garden.",
                    "How should the remaining 10 tokens be divided fairly?",
                ],
            },
            "correct": "Share 2 each and save 2 for the group",
            "options": [
                "Share 2 each and save 2 for the group",
                "Give all 10 to the loudest volunteer",
                "Leave them scattered on the path",
                "Keep 7 yourself and give 1 to each friend",
            ],
            "hints": ["4 friends x 2 tokens = 8 tokens, leaving 2 extra.", "Giving everyone an equal 2 tokens and saving 2 for next time is completely fair!"],
            "success": "Fairness champion! Equal shares plus group savings leaves zero hurt feelings!",
            "retry": "Check which option treats every volunteer equally!",
            "words": ["coin"],
        },
    )

    # ---- Level 12: Dino Valley (fossils / prehistoric life / footprint deduction) ----
    register(
        12, 4,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Complete the steps showing how a fossil forms!",
            "media": {
                "subtitle": "Fossil Lab",
                "obj": "obj_shard",
                "scenario": "Follow how ancient bones or shells become fossils over millions of years:",
                "sequence": ["Buried in mud", "Soft parts fade", "Minerals fill in", "?"],
            },
            "correct": "Rock preserves its shape",
            "options": [
                "Rock preserves its shape",
                "It turns into ocean clouds",
                "It floats up into space",
                "It sprouts fresh leaves",
            ],
            "hints": ["Minerals harden into stone matching the original outline.", "The hardened rock preserves the fossil's shape!"],
            "success": "Awesome paleontology! Minerals turn buried remains into stone!",
            "retry": "Think about what hardens around minerals over millions of years!",
            "words": ["shard"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What do we call dinosaur bones or prints turned to stone?",
            "media": {
                "subtitle": "Dino Fossils",
                "obj": "obj_shard",
                "scenario": "Explorers brush sand off ancient stone footprints in Dino Valley. What are these ancient traces called?",
            },
            "correct": "Fossils", "options": ["Fossils", "Bubbles", "Feathers"],
            "hints": ["Starts with 'F' - preserved clues of ancient life.", "Bones, shells, and prints turned to stone are fossils!"],
            "success": "Yes! Ancient stone traces are fossils!",
            "retry": "Pick the science word for ancient life preserved in stone!",
            "words": ["shard"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Deduce which cliff fossil is the OLDEST!",
            "media": {
                "subtitle": "Rock Layers",
                "obj": "obj_shard",
                "scenario": "Inspect the undisturbed canyon cliff layers from top to bottom:",
                "clues": [
                    "The top rock layer holds dinosaur footprints.",
                    "The middle rock layer holds fossilized fern leaves.",
                    "The bottom rock layer holds ancient sea shells.",
                    "Which fossil in this cliff is the OLDEST?",
                ],
            },
            "correct": "Sea shells in the bottom layer",
            "options": [
                "Sea shells in the bottom layer",
                "Footprints in the top layer",
                "Fern leaves in the middle layer",
                "All three formed on the same day",
            ],
            "hints": ["Sediment piles up from bottom to top over ages.", "The deepest layer at the bottom was laid down first, so it is oldest!"],
            "success": "Geologist thinking! Deeper undisturbed rock layers are older!",
            "retry": "Remember rock layers build upward over time - bottom came first!",
            "words": ["shard"],
        },
    )

    register(
        12, 5,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What did a flat-toothed Triceratops eat?",
            "media": {
                "subtitle": "Prehistoric Diet",
                "obj": "obj_tree",
                "scenario": "Fossil skulls show Triceratops had a strong beak and rows of flat grinding teeth. What kind of food was its jaw built for?",
            },
            "correct": "Plants (herbivore)",
            "options": ["Plants (herbivore)", "Only fast fish", "Other giant dinos", "Volcanic lava"],
            "hints": ["Flat grinding teeth mash tough leaves and ferns.", "Plant-eating dinosaurs are herbivores!"],
            "success": "Spot on! Triceratops was a plant-eating herbivore!",
            "retry": "Compare flat grinding teeth to sharp tearing teeth!",
            "words": ["tree"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "What did tall long-necked dinosaurs reach high in trees to eat?",
            "media": {
                "subtitle": "Plant Eater",
                "obj": "obj_tree",
                "scenario": "A long-necked dinosaur stretched high into tall treetops. What was it eating?",
            },
            "correct": "Leaves and plants", "options": ["Leaves and plants", "Ice cubes", "Gold coins"],
            "hints": ["Look at what grows high on tree branches!", "It munched leaves and plants!"],
            "success": "Great job! Tall dinosaurs ate treetop leaves!",
            "retry": "Think about what grows at the top of tall trees!",
            "words": ["tree"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Infer the dinosaur's diet from fossil skull evidence!",
            "media": {
                "subtitle": "Skull Evidence",
                "obj": "obj_shard",
                "scenario": "Analyze the fossil skeleton unearthed in Dino Valley:",
                "clues": [
                    "Jaws hold long, curved, serrated (saw-edged) teeth.",
                    "Eye sockets face forward for judging hunting distance.",
                    "Hands and feet end in sharp, hooked claws.",
                    "What best describes this dinosaur?",
                ],
            },
            "correct": "Predator (carnivore)",
            "options": [
                "Predator (carnivore)",
                "Gentle fern grazer",
                "Deep-ocean coral",
                "Tree-bark chewer",
            ],
            "hints": ["Serrated teeth slice meat, and forward eyes track moving prey.", "All clues point to a meat-eating predator (carnivore)!"],
            "success": "Sharp fossil detective work! Every trait fits a carnivore!",
            "retry": "Notice serrated teeth, forward vision, and hooked claws!",
            "words": ["shard"],
        },
    )

    register(
        12, 6,
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Deduce the story told by the Dino Valley trackway!",
            "media": {
                "subtitle": "Track Deduction",
                "obj": "obj_key",
                "scenario": "Examine the preserved footprint clues in ancient mudstone:",
                "clues": [
                    "Two trackways run side by side at the same steady angle.",
                    "One trackway has huge prints; the other has smaller prints.",
                    "Suddenly, the stride spacing between prints gets much wider.",
                ],
            },
            "correct": "Two dinosaurs walked together and sped up",
            "options": [
                "Two dinosaurs walked together and sped up",
                "A giant bird nested in the clouds",
                "Ocean waves carved square tiles",
                "Only fern roots grew in the mud",
            ],
            "hints": ["Parallel tracks show two travelers; wider steps show running faster!", "A big dinosaur and smaller one walked side by side, then sped up!"],
            "success": "Trackway decoded! Wider stride spacing means faster speed!",
            "retry": "Match the parallel tracks and wider steps to the story!",
            "words": ["key"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Which animal left the deepest, widest footprints by the river?",
            "media": {
                "subtitle": "Dino Tracks",
                "obj": "obj_bird",
                "scenario": "Three sets of tracks cross soft mud. Which creature made the biggest, deepest footprints?",
            },
            "correct": "The heaviest dinosaur", "options": ["The heaviest dinosaur", "A tiny beetle", "A little frog"],
            "hints": ["Heavy feet sink deepest and widest into mud.", "The heaviest dinosaur leaves the largest prints!"],
            "success": "Right! Big heavy feet make giant footprints!",
            "retry": "Compare the weight of a giant dinosaur, a beetle, and a frog!",
            "words": ["bird"],
        },
        {
            "ctype": "choice-card", "skill": "science",
            "prompt": "Reconstruct the exact timeline on the ancient riverbed!",
            "media": {
                "subtitle": "Trackway Inference",
                "obj": "obj_key",
                "scenario": "Order these overlapping traces to find which event happened FIRST:",
                "clues": [
                    "Track A steps cleanly on TOP of Track B.",
                    "Tiny raindrop pits appear INSIDE Track A's toe marks.",
                    "Sun-baked mud cracks cut straight across the raindrop pits.",
                ],
            },
            "correct": "Track B dinosaur walked by",
            "options": [
                "Track B dinosaur walked by",
                "Track A dinosaur crossed over",
                "Rain fell onto the wet mud",
                "Sun dried and cracked the mud",
            ],
            "hints": ["Whatever lies underneath was already there before the next mark stamped over it.", "B was stepped over by A, then rained on, then dried: B came first!"],
            "success": "Master detective! Track B had to be there first for A to step over it!",
            "retry": "Work backward: sun cracks cut rain, rain hit A, A stepped on B!",
            "words": ["key"],
        },
    )

    # ---- Level 13: Music Room (music-play: echo & next) ----
    register(
        13, 13,
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Listen to the 3-note chime, then echo it on the xylophone!",
            "media": {"notes": ["C", "E", "G"], "showGuides": True, "musicMode": "echo"},
            "correct": "C-E-G", "options": [],
            "hints": ["Watch which bars light up and listen carefully.", "Tap C, then E, then G in order!"],
            "success": "Musical ear! You echoed C-E-G cleanly!",
            "retry": "Tap Listen Again and match the three notes in order!",
        },
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Listen and tap the 2 glowing notes in order!",
            "media": {"notes": ["C", "G"], "showGuides": True, "musicMode": "echo"},
            "correct": "C-G", "options": [],
            "hints": ["Watch the two bars glow one after the other.", "First tap C, then tap G!"],
            "success": "Yay! You played C and G!",
            "retry": "Tap Listen Again to see the two glowing bars!",
        },
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Ear training! Listen closely and echo all 4 notes with no lights!",
            "media": {"notes": ["C", "E", "G", "C5"], "showGuides": False, "musicMode": "echo"},
            "correct": "C-E-G-C5", "options": [],
            "hints": ["The melody climbs steadily upward as a major chord.", "Start on low C and climb C -> E -> G -> high C5!"],
            "success": "Incredible ear! You nailed the full arpeggio!",
            "retry": "Listen to the rising pitch and test bars before replaying Listen Again!",
        },
    )

    register(
        13, 14,
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Listen to C, D, E - tap the NEXT note climbing up the scale!",
            "media": {"notes": ["C", "D", "E", "F"], "showGuides": True, "musicMode": "next"},
            "correct": "F", "options": [],
            "hints": ["The xylophone steps right one bar each time: C, D, E...", "The very next bar after E is F!"],
            "success": "Spot on! F continues the climbing scale!",
            "retry": "Listen to the steps C -> D -> E and tap the very next bar!",
        },
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Listen to C, D - tap the NEXT bar to the right!",
            "media": {"notes": ["C", "D", "E"], "showGuides": True, "musicMode": "next"},
            "correct": "E", "options": [],
            "hints": ["Look one bar past D!", "Tap the yellow E bar!"],
            "success": "Awesome! C, D, E!",
            "retry": "Follow the rainbow bars one step past D!",
        },
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Ear-only challenge! Listen to the descending notes and find the next one!",
            "media": {"notes": ["C5", "A", "G", "F"], "showGuides": False, "musicMode": "next"},
            "correct": "F", "options": [],
            "hints": ["The melody steps downward from high C5 through A and G.", "One step below G on our xylophone is F!"],
            "success": "Golden ears! F resolves that descending line cleanly!",
            "retry": "Tap Listen Again and hear how the pitch steps down below G!",
        },
    )

    register(
        13, 15,
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Echo the finale 4-note melody on the xylophone!",
            "media": {"notes": ["G", "E", "C", "G"], "showGuides": True, "musicMode": "echo"},
            "correct": "G-E-C-G", "options": [],
            "hints": ["Notice it steps down G -> E -> C, then jumps back up to G.", "Tap G, E, C, G in order!"],
            "success": "Bravo! You played the Crystal Finale melody!",
            "retry": "Tap Listen Again and follow the four glowing bars!",
        },
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Echo the 2-note chime: E then G!",
            "media": {"notes": ["E", "G"], "showGuides": True, "musicMode": "echo"},
            "correct": "E-G", "options": [],
            "hints": ["Watch which two bars light up.", "Tap yellow E, then teal G!"],
            "success": "Sweet music! E and G sound bright!",
            "retry": "Tap Listen Again and watch E then G!",
        },
        {
            "ctype": "music-play", "skill": "music",
            "prompt": "Master ear challenge! Echo the 5-note Music Room finale with no guides!",
            "media": {"notes": ["C", "E", "G", "A", "C5"], "showGuides": False, "musicMode": "echo"},
            "correct": "C-E-G-A-C5", "options": [],
            "hints": ["Starts on the C-E-G chord, steps up to A, and resolves on high C5.", "Play C -> E -> G -> A -> C5!"],
            "success": "Virtuoso! You mastered the Music Room finale!",
            "retry": "Tap Listen Again and break the 5-note climb into steps!",
        },
    )

    return by_level, variants


def eval_op(a: int, b: int, op: str) -> int:
    return a + b if op == "+" else a - b


def validate_bank(bank: dict[str, Any], objects_dir: Path) -> None:
    """Strictly validates the complete generated Phase 2 bank."""
    errors: list[str] = []

    if bank.get("skills") != PHASE2_SKILLS:
        errors.append(f"bank.skills mismatch: {bank.get('skills')}")

    available_sprites = {p.stem for p in objects_dir.glob("*.png")}
    if not available_sprites:
        errors.append(f"No object sprites found in {objects_dir}")
    available_bgs = {p.stem for p in BACKGROUNDS_DIR.glob("*.png")}
    if not available_bgs:
        errors.append(f"No background sprites found in {BACKGROUNDS_DIR}")

    levels = bank.get("levels") or []
    variants = bank.get("variants") or []
    if len(levels) != 13:
        errors.append(f"Expected 13 levels, found {len(levels)}")

    variant_map: dict[str, dict[str, Any]] = {}
    for v in variants:
        vid = v.get("id", "")
        if vid in variant_map:
            errors.append(f"Duplicate variant id '{vid}'")
        variant_map[vid] = v

    all_ids: set[str] = set()
    core_count = 0
    final_count = 0

    def check_sprite(where: str, sprite_name: str | None) -> None:
        if sprite_name and sprite_name not in available_sprites:
            errors.append(f"{where}: missing sprite '{sprite_name}' in {objects_dir}")

    def check_ascii_field(where: str, label: str, val: str | None) -> None:
        if val is not None and not is_ascii(val):
            errors.append(f"{where}: non-ASCII character in {label}: {val!r}")

    def validate_entry(c: dict[str, Any], is_phase2: bool) -> None:
        cid = c.get("id", "")
        where = f"challenge '{cid}'"
        if not cid:
            errors.append("Challenge missing id")
            return
        if cid in all_ids:
            errors.append(f"{where}: duplicate id across bank")
        all_ids.add(cid)

        ctype = c.get("type", "")
        skill = c.get("skill", "")
        diff = c.get("difficulty", "")
        if ctype not in VALID_TYPES:
            errors.append(f"{where}: invalid type '{ctype}'")
        if skill not in PHASE2_SKILLS:
            errors.append(f"{where}: invalid skill '{skill}'")
        if diff not in {"base", "easier", "harder"}:
            errors.append(f"{where}: invalid difficulty '{diff}'")
        if (c.get("timer") or 0) <= 0:
            errors.append(f"{where}: timer must be > 0")

        prompt = c.get("prompt", "")
        vo = c.get("vo", "")
        if not prompt:
            errors.append(f"{where}: missing prompt")
        if not vo or not VO_RE.match(vo):
            errors.append(f"{where}: invalid vo '{vo}'")

        hints = c.get("hints") or []
        if len(hints) != 2 or not hints[0] or not hints[1]:
            errors.append(f"{where}: must have 2 non-empty hints")

        fb = c.get("feedback") or {}
        if not fb.get("success") or not fb.get("retry"):
            errors.append(f"{where}: feedback.success and feedback.retry required")

        media = c.get("media")
        if not isinstance(media, dict):
            errors.append(f"{where}: missing media object")
            return

        if is_phase2:
            check_ascii_field(where, "prompt", prompt)
            for idx, h in enumerate(hints):
                check_ascii_field(where, f"hints[{idx}]", h)
            check_ascii_field(where, "feedback.success", fb.get("success"))
            check_ascii_field(where, "feedback.retry", fb.get("retry"))
            for idx, opt in enumerate(c.get("options") or []):
                check_ascii_field(where, f"options[{idx}]", opt)
            check_ascii_field(where, "media.subtitle", media.get("subtitle"))
            check_ascii_field(where, "media.scenario", media.get("scenario"))
            for idx, clue in enumerate(media.get("clues") or []):
                check_ascii_field(where, f"media.clues[{idx}]", clue)
            for idx, seq in enumerate(media.get("sequence") or []):
                check_ascii_field(where, f"media.sequence[{idx}]", seq)
            for w in c.get("words") or []:
                if w not in REVIEWABLE_WORDS:
                    errors.append(f"{where}: word '{w}' is not in ReviewableWords")

        check_sprite(where, media.get("obj"))
        for pic in media.get("pictures") or []:
            check_sprite(where, pic.get("sprite"))
        for tri in media.get("triples") or []:
            check_sprite(where, tri.get("picObj"))

        options = c.get("options") or []
        correct = c.get("correctAnswer", "")
        if len(options) != len(set(options)):
            errors.append(f"{where}: duplicate options {options}")

        if ctype in {"choice-card", "math-choice"}:
            if not options:
                errors.append(f"{where}: {ctype} requires non-empty options")
            elif correct not in options:
                errors.append(f"{where}: correctAnswer '{correct}' not in options {options}")

        if ctype == "choice-card":
            if not (2 <= len(options) <= 4):
                errors.append(f"{where}: choice-card expects 2..4 options, got {len(options)}")
            has_body = bool(media.get("scenario") or media.get("clues") or media.get("sequence"))
            if not has_body:
                errors.append(f"{where}: choice-card needs scenario, clues, or sequence")

        elif ctype == "math-choice":
            if len(options) not in {4, 6}:
                errors.append(f"{where}: math-choice expects 4 or 6 options, got {len(options)}")
            for opt in options:
                try:
                    int(opt)
                except ValueError:
                    errors.append(f"{where}: math-choice option '{opt}' must be integer")
            try:
                ans_num = int(correct)
            except ValueError:
                errors.append(f"{where}: math-choice answer '{correct}' must be integer")
                ans_num = 0
            if media.get("numberline"):
                if (media.get("start", 0) + media.get("move", 0)) != ans_num:
                    errors.append(f"{where}: numberline arithmetic mismatch")
            elif media.get("steps") and len(media["steps"]) >= 2:
                s0, s1 = media["steps"][0], media["steps"][1]
                if eval_op(s0["a"], s0["b"], s0["op"]) != s0["r"] or eval_op(s1["a"], s1["b"], s1["op"]) != ans_num:
                    errors.append(f"{where}: steps arithmetic mismatch")
            elif not media.get("countOnly"):
                if eval_op(media.get("a", 0), media.get("b", 0), media.get("op", "+")) != ans_num:
                    errors.append(f"{where}: arithmetic mismatch")

        elif ctype == "directions":
            moves = media.get("moves") or []
            if moves:
                x, y = media.get("sx", 0), media.get("sy", 0)
                gw, gh = media.get("gw", 0), media.get("gh", 0)
                for mv in moves:
                    d, n = mv.get("dir"), mv.get("n", 0)
                    if d == "right":
                        x += n
                    elif d == "left":
                        x -= n
                    elif d == "up":
                        y += n
                    elif d == "down":
                        y -= n
                    else:
                        errors.append(f"{where}: invalid move dir '{d}'")
                    if not (0 <= x < gw and 0 <= y < gh):
                        errors.append(f"{where}: move exits {gw}x{gh} grid at ({x}, {y})")
                if (x, y) != (media.get("tx"), media.get("ty")):
                    errors.append(f"{where}: moves end at ({x},{y}) != target ({media.get('tx')},{media.get('ty')})")

        elif ctype == "memory-match":
            pairs = media.get("pairs") or []
            if len(pairs) < 2:
                errors.append(f"{where}: memory-match needs at least 2 pairs")
            pair_ids: set[str] = set()
            for idx, p in enumerate(pairs):
                pid = p.get("id", "")
                if not pid:
                    errors.append(f"{where}: pair[{idx}] missing id")
                elif pid in pair_ids:
                    errors.append(f"{where}: duplicate pair id '{pid}'")
                pair_ids.add(pid)
                if not p.get("aLabel") and not p.get("aSprite"):
                    errors.append(f"{where}: pair '{pid}' needs aLabel or aSprite")
                check_sprite(f"{where}.pair[{pid}].aSprite", p.get("aSprite"))
                check_sprite(f"{where}.pair[{pid}].bSprite", p.get("bSprite"))
                if is_phase2:
                    check_ascii_field(where, f"pair[{pid}].aLabel", p.get("aLabel"))
                    check_ascii_field(where, f"pair[{pid}].bLabel", p.get("bLabel"))

        elif ctype == "pixel-art":
            gw = media.get("gw", 0)
            gh = media.get("gh", 0)
            palette = media.get("palette") or []
            target_grid = media.get("targetGrid") or []
            start_grid = media.get("startGrid") or []
            art_mode = media.get("artMode", "")
            if gw <= 0 or gh <= 0:
                errors.append(f"{where}: pixel-art invalid dimensions {gw}x{gh}")
            if art_mode not in {"copy", "symmetry", "pattern"}:
                errors.append(f"{where}: pixel-art invalid artMode '{art_mode}'")
            if not palette:
                errors.append(f"{where}: pixel-art needs non-empty palette")
            for col in palette:
                if not HEX_RE.match(col):
                    errors.append(f"{where}: pixel-art invalid hex color '{col}'")
            if len(target_grid) != gw * gh:
                errors.append(f"{where}: targetGrid length {len(target_grid)} != {gw * gh}")
            if len(start_grid) != gw * gh:
                errors.append(f"{where}: startGrid length {len(start_grid)} != {gw * gh}")
            for i, val in enumerate(target_grid):
                if val < -1 or val >= len(palette):
                    errors.append(f"{where}: targetGrid[{i}]={val} out of palette range")
            for i, val in enumerate(start_grid):
                if val < -1 or val >= len(palette):
                    errors.append(f"{where}: startGrid[{i}]={val} out of palette range")
                elif val >= 0 and i < len(target_grid) and val != target_grid[i]:
                    errors.append(f"{where}: locked startGrid[{i}]={val} != targetGrid[{i}]={target_grid[i]}")

        elif ctype == "music-play":
            notes = media.get("notes") or []
            mode = media.get("musicMode", "")
            if mode not in {"echo", "next"}:
                errors.append(f"{where}: music-play invalid musicMode '{mode}'")
            if len(notes) < 2:
                errors.append(f"{where}: music-play needs at least 2 notes")
            for n in notes:
                if n not in VALID_NOTES:
                    errors.append(f"{where}: music-play invalid note '{n}'")
            expected_ans = "-".join(notes) if mode == "echo" else (notes[-1] if notes else "")
            if correct != expected_ans:
                errors.append(f"{where}: music-play correctAnswer '{correct}' != '{expected_ans}'")

    for lv in levels:
        lid = lv.get("id", 0)
        bg = lv.get("bg", "")
        if bg not in available_bgs:
            errors.append(f"level {lid}: missing background '{bg}' in {BACKGROUNDS_DIR}")
        chals = lv.get("challenges") or []
        expected_len = 15 if lid == 13 else 6
        if len(chals) != expected_len:
            errors.append(f"level {lid}: expected {expected_len} challenges, found {len(chals)}")

        if lid == 1 and chals and chals[0].get("type") != "math-choice":
            errors.append("level 1 round 1 must remain math-choice for QaTestDriver")
        if 3 <= lid <= 13 and len(chals) >= 3:
            for r_idx in range(3):
                if not chals[r_idx].get("id", "").startswith("P2_"):
                    errors.append(f"level {lid} round {r_idx + 1} must be a Phase 2 activity first")

        for expected_idx, c in enumerate(chals, start=1):
            if c.get("round") != expected_idx:
                errors.append(f"level {lid} challenge '{c.get('id')}': round={c.get('round')} expected {expected_idx}")
            is_p2 = c.get("id", "").startswith("P2_")
            validate_entry(c, is_p2)
            if isinstance(c.get("media"), dict) and "arabic" in c["media"]:
                errors.append(f"{c.get('id')}: media must not contain 'arabic'")
            if lid <= 12:
                core_count += 1
            else:
                final_count += 1

            for link_key, req_diff in (("easier", "easier"), ("band5to7", "easier"), ("harder", "harder"), ("band11to12", "harder")):
                target_id = c.get(link_key)
                if target_id:
                    if target_id not in variant_map:
                        errors.append(f"{c.get('id')}: {link_key} variant '{target_id}' not found")
                    elif variant_map[target_id].get("difficulty") != req_diff:
                        errors.append(f"{c.get('id')}: {link_key} '{target_id}' has difficulty '{variant_map[target_id].get('difficulty')}' != '{req_diff}'")

            if is_p2:
                if not c.get("band5to7") or not c.get("band11to12"):
                    errors.append(f"{c.get('id')}: Phase 2 base challenge must define both band5to7 and band11to12")
                if c.get("easier") != c.get("band5to7") or c.get("harder") != c.get("band11to12"):
                    errors.append(f"{c.get('id')}: easier/harder must match band5to7/band11to12")

    for v in variants:
        validate_entry(v, v.get("id", "").startswith("V_P2_"))
        if isinstance(v.get("media"), dict) and "arabic" in v["media"]:
            errors.append(f"{v.get('id')}: media must not contain 'arabic'")

    if any(0x0600 <= ord(ch) <= 0x06FF for ch in json.dumps(bank, ensure_ascii=False)):
        errors.append("bank contains Arabic Unicode characters")

    if core_count != 72:
        errors.append(f"Expected 72 core challenges (L1..12), found {core_count}")
    if final_count != 15:
        errors.append(f"Expected 15 final challenges (L13), found {final_count}")
    if len(variants) != 110:
        errors.append(f"Expected 110 total variants (32 original + 78 Phase 2), found {len(variants)}")

    if errors:
        raise ValueError("Phase 2 bank validation failed:\n  - " + "\n  - ".join(errors))


def generate_bank(bank: dict[str, Any]) -> dict[str, Any]:
    """Strips prior Phase 2 / Arabic entries and orders Phase 2 first on Islands 3-13."""
    levels = bank.get("levels") or []
    variants = bank.get("variants") or []

    def strip_arabic(entry: dict[str, Any]) -> None:
        media = entry.get("media")
        if isinstance(media, dict):
            media.pop("arabic", None)

    # Strip previously generated Phase 2 entries for idempotency
    for lv in levels:
        kept = [c for c in (lv.get("challenges") or []) if not c.get("id", "").startswith("P2_")]
        for c in kept:
            strip_arabic(c)
        lv["challenges"] = kept
    variants = [v for v in variants if not v.get("id", "").startswith("V_P2_")]
    for v in variants:
        strip_arabic(v)

    orig_core = sum(len(lv["challenges"]) for lv in levels if lv.get("id", 0) <= 12)
    orig_final = sum(len(lv["challenges"]) for lv in levels if lv.get("id", 0) == 13)
    if orig_core != 36 or orig_final != 12 or len(variants) != 32:
        raise ValueError(
            f"Unexpected baseline counts after stripping Phase 2 entries: "
            f"core={orig_core} (expected 36), final={orig_final} (expected 12), variants={len(variants)} (expected 32)"
        )

    p2_by_level, p2_variants = build_phase2_entries()
    for lv in levels:
        lid = lv["id"]
        if lid <= 2:
            lv["challenges"] = lv["challenges"] + p2_by_level[lid]
        else:
            lv["challenges"] = p2_by_level[lid] + lv["challenges"]
        for rnd_idx, ch in enumerate(lv["challenges"], start=1):
            ch["round"] = rnd_idx

    bank["skills"] = list(PHASE2_SKILLS)
    bank["variants"] = variants + p2_variants
    return bank


def main() -> int:
    parser = argparse.ArgumentParser(description="Build and validate the Phase 2 Wonder Islands levels.json bank.")
    parser.add_argument("--dry-run", action="store_true", help="Validate and print summary without modifying output file.")
    parser.add_argument("--input", type=Path, default=DEFAULT_BANK_PATH, help="Input levels.json path.")
    parser.add_argument("--output", type=Path, default=DEFAULT_BANK_PATH, help="Output levels.json path.")
    args = parser.parse_args()

    with args.input.open("r", encoding="utf-8") as f:
        bank = json.load(f)

    updated = generate_bank(bank)
    validate_bank(updated, OBJECTS_DIR)

    p2_core = sum(1 for lv in updated["levels"] for c in lv["challenges"] if c["id"].startswith("P2_"))
    p2_vars = sum(1 for v in updated["variants"] if v["id"].startswith("V_P2_"))
    total_core = sum(len(lv["challenges"]) for lv in updated["levels"])

    type_counts: dict[str, int] = {}
    for lv in updated["levels"]:
        for c in lv["challenges"]:
            type_counts[c["type"]] = type_counts.get(c["type"], 0) + 1

    print(f"Phase 2 bank validation PASSED")
    print(f"  Skills ({len(updated['skills'])}): {', '.join(updated['skills'])}")
    print(f"  Levels: {len(updated['levels'])} (L1-12: 6 rounds each = 72; L13: 15 rounds)")
    print(f"  Total core challenges: {total_core} (36+12 original + {p2_core} Phase 2)")
    print(f"  Total variants: {len(updated['variants'])} (32 original + {p2_vars} Phase 2)")
    print(f"  Core types: {dict(sorted(type_counts.items()))}")

    if args.dry_run:
        print("  Mode: DRY-RUN (no files modified)")
    else:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("w", encoding="utf-8", newline="\n") as f:
            json.dump(updated, f, ensure_ascii=False, indent=1)
            f.write("\n")
        print(f"  Wrote updated bank to {args.output}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
