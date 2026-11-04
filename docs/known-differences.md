# Known differences from RapidFuzz

Fuzzion's oracle tests (`Fuzzion.Tests/Oracle`) compare every metric with Python
RapidFuzz 3.14.6. This page lists every case where Fuzzion **deliberately** returns a
different value, and why. The oracle tests skip only these cases. Any other difference
is a bug.

| ID | Where | Fuzzion | RapidFuzz / TheFuzz | Status |
|---|---|---|---|---|
| KD1 | `Fuzz.Ratio("", "")`, `Fuzz.TokenSortRatio("", "")` | 0 | 100 | Open decision (see below) |
| KD2 | `Fuzz.WeightedRatio` when one string is at least 1.5x longer than the other | Uses full `TokenSortRatio` / `TokenSetRatio` in the partial branch | Uses `PartialTokenSortRatio` / `PartialTokenSetRatio` | Open decision (see below) |

## KD1: two empty strings score 0

Fuzzion inherits the original FuzzyWuzzy rule: if either input is empty, the score is 0
(`DefaultRatioStrategy.Calculate`). RapidFuzz and current TheFuzz return 100 when
**both** inputs are empty, because two empty strings are identical. One empty input
scores 0 everywhere.

Oracle impact: 1 of 410 sentence pairs for `ratio` and for `token_sort_ratio`.

**Recommendation:** return 100 for two empty strings, to match RapidFuzz and TheFuzz,
and keep 0 when exactly one is empty. `QRatio` (item #17) will return 0 for any empty
input, which is the place for the "empty means no match" rule.

## KD2: WeightedRatio partial branch

When the length ratio is 1.5 or more, FuzzyWuzzy's `WRatio` compares the shorter string
against parts of the longer one using **partial** token scores. Fuzzion (inherited from
the original FuzzySharp) uses the **full** token scores there. The code says so:
`// Keep the legacy full TokenSort/TokenSet partial branch for compatibility.`

The full scores are lower whenever the shorter string matches only part of the longer
one, so Fuzzion's `WeightedRatio` is lower than RapidFuzz's in those cases. Example:
`WRatio("smith corp", "limited smitzh corp")` is 81 in Fuzzion and 85.5 in RapidFuzz.

Oracle impact: 43 of 410 sentence pairs, all in the partial branch. All pairs where the
length ratio is below 1.5 match exactly.

**Recommendation:** switch to the partial token scores, to match RapidFuzz. This raises
`WeightedRatio` (and so default `Process.Extract*` scores) for strings of very different
lengths.

## Fixed (not differences any more)

These were bugs inherited from Raffinert.FuzzySharp v6.0.1, found by the oracle and
fixed in Fuzzion 0.1.0-preview.1. Regression tests are in
`Fuzzion.Tests/OracleFoundBugsTests.cs`.

| Bug | Wrong value | Correct value |
|---|---|---|
| `Indel.NormalizedDistance` divided by the lengths **after** trimming the shared prefix and suffix (532 of 2,958 oracle values wrong) | `("abc", "abcd")` gave 1.0 | 1/7 ≈ 0.142857 |
| `LongestCommonSubsequence.NormalizedSimilarity` of two empty sequences | 0 | 1 |
