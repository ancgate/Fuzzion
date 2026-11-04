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

## Phase 2 metrics after P8.1 and P8.2

Time relative to `Levenshtein.Distance` on the same input. Short run, so only the order
of magnitude is meaningful.

| Metric | 8 | 64 | 256 | 1,024 | Before Phase 8 (256 / 1,024) |
|---|---|---|---|---|---|
| `Osa` (P8.1: multi-word bit-parallel above 64) | ~1x | ~1x | **~2x** | **~1.5x** | 38x / 67x |
| `Jaro` (P8.2: bit-parallel up to 64) | ~2x | **~1.4x** (was 3.9x) | ~5x | ~4x | 5x / 7x (unchanged: scalar above 64) |
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

## Not yet done

- **CI regression job** (optional in the plan): a short benchmark run that fails on a
  slowdown of more than 20%. Not added, because shared CI runners are about as noisy as
  this container.
- See the Phase 8 notes in `docs/IMPLEMENTATION-PLAN.md` for remaining speed-ups
  (multi-word Jaro, caching in the Jaro instance classes, SIMD for `Extract*` and for
  distance scorers).
