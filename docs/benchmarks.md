# Benchmarks

Results for the Phase 8 performance work (plan item #34). Run any of them with:

```bash
dotnet run -c Release --project Fuzzion.Benchmarks -- --filter '*CdistBenchmarks*'
```

**Machine:** shared Linux cloud container, x64 with AVX2 (`Vector<byte>.Count` = 32),
.NET 10.0.14, BenchmarkDotNet 0.15.8. The container is noisy: compare rows within a
table, not across runs. "Default job" results are reliable to a few percent;
"short run" results (3 iterations) can be off by 2x and only show orders of magnitude.

## SIMD batch Ratio in `Cdist` (#33)

`Process.Cdist(100 queries, 2,000 choices)` with plain `Ratio`; choices are between half
and the full length shown. Default job.

| Choice length | Scalar | SIMD | Speed-up | Allocated (scalar → SIMD) |
|---|---|---|---|---|
| 8 | 14.3 ms | 3.3 ms | **4.3x** | 13.8 MB → 2.1 MB |
| 16 | 20.0 ms | 4.7 ms | **4.3x** | 13.8 MB → 2.6 MB |
| 32 | 29.1 ms | 7.6 ms | **3.8x** | 13.8 MB → 3.7 MB |
| 64 | 52.2 ms | 16.8 ms | **3.1x** | 13.8 MB → 5.7 MB |

Source: `CdistBenchmarks`.

### `Cdist` with `Scorers.Levenshtein`

100 queries (16 characters) × 2,000 choices (8-16 characters), default job.

| Scalar | SIMD | Speed-up |
|---|---|---|
| 51.7 ms | 3.9 ms | **13x** |

Source: `BatchSearchBenchmarks`.

### Not shipped: SIMD for `Extract*`

Batching the choices of a single `Process.ExtractTop` call was measured and made it
**slower** (Ratio: 239 µs → 496 µs; Levenshtein: 427 µs → 502 µs, 2,000 choices): with
one query, building the per-choice bit tables costs more than the vectorized loop
saves. In `Cdist` the tables are reused for every query, which is where SIMD pays off.
The `Extract*` methods therefore keep the scalar path; for many queries, use `Cdist`.

## Damerau-Levenshtein (P8.3)

String path (array of last rows for characters below 256) versus the generic path
(dictionary, used for `ReadOnlySpan<T>` of other types), with Levenshtein for scale.
Similar random strings. Default job.

| Length | String path | Generic path | Speed-up | Levenshtein |
|---|---|---|---|---|
| 8 | 71 ns | 119 ns | 1.7x | 108 ns |
| 64 | 18.0 µs | 31.6 µs | 1.8x | 0.61 µs |
| 256 | 458 µs | 929 µs | 2.0x | 6.8 µs |

Damerau-Levenshtein needs a full O(n·m) table (RapidFuzz's too), so it stays far slower
than the bit-parallel Levenshtein on long strings. Source: `DamerauLevenshteinBenchmarks`.

## Jaro (P8.2, P8.4)

Time relative to `Levenshtein.Distance` on the same input; default job.

| Length | Before Phase 8 | `Jaro.Similarity` now | Cached `new Jaro(a).SimilarityWith(b)` |
|---|---|---|---|
| 8 | 1.1x | **0.97x** | 0.94x |
| 64 | 3.9x | **1.41x** | **1.06x** |
| 256 | 5.1x | **0.66x** | 0.52x |
| 1,024 | 7.0x | **0.26x** | 0.27x |

Inputs of up to 16 characters use the plain algorithm (cheaper than building bit masks);
longer ones the bit-parallel search on one or several 64-bit words. The cached instance
builds its bit pattern once and allocates nothing per call. Source: `JaroBenchmarks`.

## Phase 2 metrics after P8.1 and P8.2

Time relative to `Levenshtein.Distance` on the same input. Short run, so only the order
of magnitude is meaningful.

| Metric | 8 | 64 | 256 | 1,024 | Before Phase 8 (256 / 1,024) |
|---|---|---|---|---|---|
| `Osa` (P8.1: multi-word bit-parallel above 64) | ~1x | ~1x | **~2x** | **~1.5x** | 38x / 67x |
| `Jaro` | see the Jaro table above | | | | |
| `Hamming`, `Prefix` | < 0.1x | < 0.1x | < 0.03x | < 0.01x | unchanged |

Source: `DistanceMetricsBenchmarks`.

## Levenshtein cutoff (#30)

Two 1,000-character strings. Measured with a timing script, not BenchmarkDotNet, so
rough.

| Case | Full distance | With `scoreCutoff: 5` | With `scoreHint: 3` |
|---|---|---|---|
| Close (3 edits apart) | 40 µs | 34 µs | 25 µs |
| Unrelated | 68 µs | **8 µs** | — |

Source: `LevenshteinCutoffBenchmarks`.

## Other benchmark classes

Every feature has a benchmark class; these have no recorded results yet. Run them on a
quiet machine before quoting numbers.

| Class | Covers |
|---|---|
| `EditOpsBenchmarks` | `Levenshtein.GetEditOps` / `GetOpCodes`, `Indel.GetEditOps`, `EditOps.Apply` (Phase 6) |
| `TokenScorerParityBenchmarks` | `QRatio`, `TokenRatio`, `PartialTokenRatio`, `TokenDifferenceRatio`, `PartialTokenDifferenceRatio`, `Ratio` with a cutoff (Phase 3) |
| `UnicodeBenchmarks` | `Full`, `FullUnicode`, `RemoveDiacritics`, `CodePoints.Compare` (Phase 7) |
| `ProcessExtrasBenchmarks` | `Process.Dedupe`, `Process.Cpdist`, `ApproximatelyEquals` (Phases 4 and 5) |
| `SetMetricsBenchmarks` | `Jaccard`, `Tanimoto`, `RatcliffObershelp`, `LongestCommonSubstring` (Phase 4) |
| `ApproximateSubstringRatioBenchmarks`, `Ratio*`, `PartialRatio*`, `Token*`, `PartialToken*`, `WeightedRatio*`, `Extract*` | The scorers and searches inherited from Raffinert.FuzzySharp |

## Regression check

The *Benchmark regression check* workflow (`.github/workflows/benchmarks.yml`, run on
demand) measures the base branch and the current branch on the same runner and fails
when any benchmark is more than 20% slower (`tools/benchmarks/compare.py`). It uses
BenchmarkDotNet's medium job: in testing, short runs of identical code differed by up
to 19%.
