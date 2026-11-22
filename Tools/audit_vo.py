#!/usr/bin/env python3
"""Audit the Lumo voice-over set (Batch 5).

Replicates the job list built by Tools/gen_vo.ps1 (prompts, feedback lines,
intros, word/sentence pronunciations, per-word clips, story intro, and the
spoken number words one..twenty) and compares it against the WAV files on
disk in Assets/Resources/Audio/Voice/.

Read-only: prints missing/extra files and exits 1 on any mismatch, 0 when
the set matches exactly.

Usage:  python3 Tools/audit_vo.py   (run from the repo root)
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BANK = os.path.join(ROOT, "Assets", "Resources", "Data", "levels.json")
VOICE_DIR = os.path.join(ROOT, "Assets", "Resources", "Audio", "Voice")

NUMBER_WORDS = [
    "one", "two", "three", "four", "five", "six", "seven", "eight",
    "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen",
    "sixteen", "seventeen", "eighteen", "nineteen", "twenty",
]


def expected_keys(bank):
    """Distinct VO file stems gen_vo.ps1 would generate from this bank."""
    jobs = set()

    def collect(c):
        if c.get("vo"):
            jobs.add(c["vo"])
        media = c.get("media") or {}
        for key in ("voWord", "voSentence"):
            if media.get(key):
                jobs.add(media[key])
        for w in c.get("words") or []:
            jobs.add("vo_word_" + w)
        jobs.add("vo_fb_" + c["id"] + "_ok")
        jobs.add("vo_fb_" + c["id"] + "_no")

    for lv in bank["levels"]:
        jobs.add("vo_intro_lvl%d" % lv["id"])
        for c in lv["challenges"]:
            collect(c)
    jobs.add("vo_story_intro")
    for v in bank.get("variants", []):
        collect(v)
    for w in NUMBER_WORDS:
        jobs.add("vo_word_" + w)
    return jobs


def main():
    with open(BANK, encoding="utf-8") as f:
        bank = json.load(f)
    expected = expected_keys(bank)
    on_disk = {
        os.path.splitext(name)[0]
        for name in os.listdir(VOICE_DIR)
        if name.endswith(".wav")
    }
    missing = sorted(expected - on_disk)
    extra = sorted(on_disk - expected)
    print("VO audit: %d expected (from levels.json) vs %d .wav on disk"
          % (len(expected), len(on_disk)))
    if missing:
        print("MISSING on disk (%d):" % len(missing))
        for name in missing:
            print("  - %s.wav" % name)
    if extra:
        print("EXTRA on disk, not referenced (%d):" % len(extra))
        for name in extra:
            print("  - %s.wav" % name)
    if missing or extra:
        print("VO audit: FAIL")
        return 1
    print("VO audit: PASS (0 missing, 0 extra)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
