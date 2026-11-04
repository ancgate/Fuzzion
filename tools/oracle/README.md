# Test oracle

`generate.py` writes reference results from Python RapidFuzz (and, later, TheFuzz and
`difflib`) to `Fuzzion.Tests/Oracle/data/`. The C# tests in `Fuzzion.Tests/Oracle`
check that Fuzzion returns the same values.

## Regenerate

```bash
python3 -m venv .venv
.venv/bin/pip install -r tools/oracle/requirements.txt
.venv/bin/python tools/oracle/generate.py
```

Output is deterministic for a given library version and seed, so a regenerated file
should not show a diff unless you changed the generator or the pinned versions.

## Files

| File | Contents |
|---|---|
| `char-pairs.json.gz` | ~1,000 character-string pairs (edge cases, short, around the 64-character boundary, long, two-letter alphabet) with results for every RapidFuzz distance metric, Ratcliff-Obershelp and longest common substring |
| `sentence-pairs.json` | ~400 word-sentence pairs with results for every `fuzz.*` scorer and `partial_ratio_alignment` |

Each row stores `a`, `b`, a `group` name, and one entry per metric. Distance metrics
store `d` (distance), `s` (similarity), `nd` (normalized distance) and `ns`
(normalized similarity).

## Adding a metric

1. Add it to `char_record` or `sentence_record` in `generate.py` and regenerate.
2. Add an `Oracle_<Metric>` test in `Fuzzion.Tests/Oracle/OracleTests.cs`.
3. If Fuzzion deliberately differs, add the case to `IsKnownDifference` and to
   `docs/known-differences.md`. Any other difference is a bug.
