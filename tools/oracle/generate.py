"""Generate reference results ("oracle" fixtures) for Fuzzion's tests.

Every value comes from Python RapidFuzz / TheFuzz / difflib, so the C# tests can check
that Fuzzion produces the same numbers. Output is deterministic: the same library
versions and seed always produce byte-identical files.

Usage (from the repository root):

    python -m venv .venv
    .venv/bin/pip install -r tools/oracle/requirements.txt
    .venv/bin/python tools/oracle/generate.py

Files are written to Fuzzion.Tests/Oracle/data/.
"""

from __future__ import annotations

import difflib
import gzip
import json
import random
import sys
from pathlib import Path

import rapidfuzz
from rapidfuzz import fuzz
from rapidfuzz.distance import (
    DamerauLevenshtein,
    Hamming,
    Indel,
    Jaro,
    JaroWinkler,
    LCSseq,
    Levenshtein,
    OSA,
    Postfix,
    Prefix,
)

SEED = 20261104
OUT_DIR = Path(__file__).resolve().parents[2] / "Fuzzion.Tests" / "Oracle" / "data"
GZIP_ABOVE_BYTES = 1_000_000

# Characters used for random strings. Only Basic Multilingual Plane characters: .NET
# strings are UTF-16, so characters above U+FFFF would be compared differently
# (see item #31 in docs/IMPLEMENTATION-PLAN.md).
ASCII = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 "
NON_ASCII = "éèàüößçñøåæœЖжДдЯя中文字日本語한국어αβγδ"
SMALL = "ab"

WORDS = (
    "new york mets yankees atlanta braves fuzzy wuzzy was a bear the quick brown fox "
    "jumps over lazy dog invoice number order payment street avenue road john smith "
    "jane doe acme corp inc ltd company limited international bank of america"
).split()

EDGE_PAIRS = [
    ("", ""),
    ("", "a"),
    ("a", ""),
    ("a", "a"),
    ("a", "b"),
    ("ab", "ba"),
    ("CA", "ABC"),
    ("abc", "abc"),
    ("abc", "abcd"),
    ("kitten", "sitting"),
    ("karolin", "kathrin"),
    ("MARTHA", "MARHTA"),
    ("DWAYNE", "DUANE"),
    ("DIXON", "DICKSONX"),
    ("CRATE", "TRACE"),
    ("prefix", "preface"),
    ("testing", "resting"),
    ("abcdef", "abcfde"),
    ("café", "cafe"),
    ("straße", "strasse"),
    ("Жук", "жук"),
    ("中文字", "中字"),
    ("a" * 64, "a" * 64),
    ("a" * 64, "a" * 63 + "b"),
    ("a" * 65, "b" + "a" * 64),
    ("ab" * 64, "ba" * 64),
]

EDGE_SENTENCES = [
    ("", ""),
    ("", "abc"),
    ("this is a test", "this is a test!"),
    ("this is a test", "yes this is a test!"),
    ("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear"),
    ("fuzzy was a bear", "fuzzy fuzzy was a bear"),
    ("new york mets", "new york mets vs atlanta braves"),
    ("new york mets", "the new york mets vs atlanta braves"),
    ("kitten", "sitting"),
    ("invoice number 12345", "processed invoice number 12345 successfully"),
]


def rand_string(rng: random.Random, alphabet: str, lo: int, hi: int) -> str:
    return "".join(rng.choice(alphabet) for _ in range(rng.randint(lo, hi)))


def mutate(rng: random.Random, s: str, alphabet: str, edits: int) -> str:
    """Apply random insertions, deletions, substitutions and swaps, so pairs are similar."""
    chars = list(s)
    for _ in range(edits):
        op = rng.randrange(4)
        if op == 0 or not chars:
            chars.insert(rng.randint(0, len(chars)), rng.choice(alphabet))
        elif op == 1:
            del chars[rng.randrange(len(chars))]
        elif op == 2:
            chars[rng.randrange(len(chars))] = rng.choice(alphabet)
        elif len(chars) > 1:
            i = rng.randrange(len(chars) - 1)
            chars[i], chars[i + 1] = chars[i + 1], chars[i]
    return "".join(chars)


def char_pairs(rng: random.Random) -> list[tuple[str, str, str]]:
    """Return (group, a, b) tuples covering short, 64-boundary, long and small-alphabet strings."""
    pairs: list[tuple[str, str, str]] = [("edge", a, b) for a, b in EDGE_PAIRS]
    groups = [
        ("short", ASCII + NON_ASCII, 0, 10, 400),
        ("boundary", ASCII, 60, 70, 300),
        ("long", ASCII, 100, 1100, 60),
        ("small-alphabet", SMALL, 0, 140, 200),
    ]
    for name, alphabet, lo, hi, count in groups:
        for i in range(count):
            a = rand_string(rng, alphabet, lo, hi)
            # Half the pairs are unrelated strings, half are close variants.
            if i % 2 == 0:
                b = rand_string(rng, alphabet, lo, hi)
            else:
                b = mutate(rng, a, alphabet, rng.randint(1, max(1, len(a) // 5)))
            pairs.append((name, a, b))
    return pairs


def sentence_pairs(rng: random.Random) -> list[tuple[str, str, str]]:
    pairs: list[tuple[str, str, str]] = [("edge", a, b) for a, b in EDGE_SENTENCES]
    for i in range(400):
        a_words = [rng.choice(WORDS) for _ in range(rng.randint(1, 8))]
        if i % 3 == 0:
            b_words = [rng.choice(WORDS) for _ in range(rng.randint(1, 8))]
        elif i % 3 == 1:
            b_words = a_words[:]
            rng.shuffle(b_words)
            if rng.random() < 0.5:
                b_words.append(rng.choice(WORDS))
        else:
            b_words = [mutate(rng, w, ASCII[:26], 1) if rng.random() < 0.3 else w for w in a_words]
            b_words = [rng.choice(WORDS)] + b_words if rng.random() < 0.5 else b_words
        pairs.append(("random", " ".join(a_words), " ".join(b_words)))
    return pairs


def four(module, a: str, b: str, **kw) -> dict:
    return {
        "d": module.distance(a, b, **kw),
        "s": module.similarity(a, b, **kw),
        "nd": module.normalized_distance(a, b, **kw),
        "ns": module.normalized_similarity(a, b, **kw),
    }


def char_record(group: str, a: str, b: str) -> dict:
    lcs_block = difflib.SequenceMatcher(None, a, b, autojunk=False).find_longest_match(0, len(a), 0, len(b))
    return {
        "group": group,
        "a": a,
        "b": b,
        "levenshtein": four(Levenshtein, a, b),
        "levenshtein_w112": four(Levenshtein, a, b, weights=(1, 1, 2)),
        "levenshtein_w234": four(Levenshtein, a, b, weights=(2, 3, 4)),
        "indel": four(Indel, a, b),
        "lcsseq": four(LCSseq, a, b),
        "hamming": four(Hamming, a, b, pad=True),
        "jaro": four(Jaro, a, b),
        "jaro_winkler": four(JaroWinkler, a, b),
        "jaro_winkler_p02": four(JaroWinkler, a, b, prefix_weight=0.2),
        "osa": four(OSA, a, b),
        "damerau_levenshtein": four(DamerauLevenshtein, a, b),
        "prefix": four(Prefix, a, b),
        "postfix": four(Postfix, a, b),
        "ratcliff_obershelp": difflib.SequenceMatcher(None, a, b, autojunk=False).ratio(),
        "longest_common_substring": [lcs_block.a, lcs_block.b, lcs_block.size],
    }


def sentence_record(group: str, a: str, b: str) -> dict:
    alignment = fuzz.partial_ratio_alignment(a, b)
    return {
        "group": group,
        "a": a,
        "b": b,
        "ratio": fuzz.ratio(a, b),
        "partial_ratio": fuzz.partial_ratio(a, b),
        "token_sort_ratio": fuzz.token_sort_ratio(a, b),
        "token_set_ratio": fuzz.token_set_ratio(a, b),
        "token_ratio": fuzz.token_ratio(a, b),
        "partial_token_sort_ratio": fuzz.partial_token_sort_ratio(a, b),
        "partial_token_set_ratio": fuzz.partial_token_set_ratio(a, b),
        "partial_token_ratio": fuzz.partial_token_ratio(a, b),
        "wratio": fuzz.WRatio(a, b),
        "qratio": fuzz.QRatio(a, b),
        "partial_ratio_alignment": [
            alignment.score,
            alignment.src_start,
            alignment.src_end,
            alignment.dest_start,
            alignment.dest_end,
        ],
    }


def write(name: str, payload: dict) -> Path:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    data = json.dumps(payload, ensure_ascii=False, separators=(",", ":"), sort_keys=True).encode("utf-8")
    if len(data) > GZIP_ABOVE_BYTES:
        path = OUT_DIR / f"{name}.json.gz"
        # mtime=0 keeps the gzip output byte-identical between runs.
        path.write_bytes(gzip.compress(data, mtime=0))
    else:
        path = OUT_DIR / f"{name}.json"
        path.write_bytes(data)
    return path


def main() -> int:
    rng = random.Random(SEED)
    meta = {
        "generator": "tools/oracle/generate.py",
        "seed": SEED,
        "rapidfuzz": rapidfuzz.__version__,
        "python": sys.version.split()[0],
    }
    chars = [char_record(*p) for p in char_pairs(rng)]
    sentences = [sentence_record(*p) for p in sentence_pairs(rng)]
    for name, rows in (("char-pairs", chars), ("sentence-pairs", sentences)):
        path = write(name, {"meta": meta, "rows": rows})
        print(f"{path.relative_to(OUT_DIR.parents[2])}: {len(rows)} rows, {path.stat().st_size:,} bytes")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
