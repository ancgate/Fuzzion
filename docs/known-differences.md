# Known differences from RapidFuzz

Fuzzion's oracle tests (`Fuzzion.Tests/Oracle`) compare every metric with Python
RapidFuzz 3.14.6. This page lists every case where Fuzzion **deliberately** returns a
different value, and why. The oracle tests skip only the cases listed here
(`IsKnownDifference` in `OracleTests.cs`). Any other difference is a bug.

**There are currently no known differences.** Every oracle value matches RapidFuzz.

## Resolved

These two differences were inherited from Raffinert.FuzzySharp v6.0.1 and were changed
to match RapidFuzz. Tests: `Fuzzion.Tests/RapidFuzzParityDecisionTests.cs`.

| ID | Where | Before | Now (same as RapidFuzz and TheFuzz) |
|---|---|---|---|
| KD1 | `Fuzz.Ratio("", "")`, `Fuzz.TokenSortRatio("", "")`, cached and generic ratio scorers | 0 | 100. Exactly one empty input still scores 0. `WeightedRatio` still returns 0 when either input is empty, as RapidFuzz's `WRatio` does |
| KD2 | `Fuzz.WeightedRatio` and `CachedWeightedRatioScorer` when one string is at least 1.5x longer | Used the full `TokenSortRatio` / `TokenSetRatio` in the partial branch | Uses `PartialTokenSortRatio` / `PartialTokenSetRatio`. Example: `("smith corp", "limited smitzh corp")` went from 81 to 85.5 |

A consequence of KD2 worth knowing: `PartialTokenSetRatio` is 100 whenever two strings
share any word, so in the partial branch a short choice that shares one common word
with the query scores 85.5 (100 × 0.95 × 0.9). RapidFuzz and TheFuzz behave the same.

## Fixed bugs found by the oracle

Bugs inherited from Raffinert.FuzzySharp v6.0.1, found by the oracle and fixed.
Regression tests are in `Fuzzion.Tests/OracleFoundBugsTests.cs`.

| Bug | Wrong value | Correct value |
|---|---|---|
| `Indel.NormalizedDistance` divided by the lengths **after** trimming the shared prefix and suffix (532 of 2,958 oracle values wrong) | `("abc", "abcd")` gave 1.0 | 1/7 ≈ 0.142857 |
| `LongestCommonSubsequence.NormalizedSimilarity` of two empty sequences | 0 | 1 |
