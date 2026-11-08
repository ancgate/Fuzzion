# Changelog

## Fuzzion 1.0.0-rc.1

*Release candidate: every phase of the implementation plan. Not yet published.*

### Examples

- Added [examples/](examples/README.md): 30 runnable examples covering every feature, plus recipes ("did you mean", company names, record linkage, autocomplete, diffs, canonical values, log grouping). CI runs them all.

### Release and API (Phase 9)

- **Breaking (pre-release):** `Tolerance` was renamed to `SimilarityTolerance`.
- Added `string` overloads to `Indel` and `LongestCommonSubsequence` (`Distance`, `Similarity`, `NormalizedDistance`, `NormalizedSimilarity`).
- The public API is frozen by `PublicApiTests` against `Fuzzion.Tests/PublicApi/Fuzzion.api.txt`; see [docs/api-review.md](docs/api-review.md).
- The package now ships XML documentation and is validated to have the same API on every target framework.
- CI (`.github/workflows/ci.yml`): Linux and Windows (with the .NET Framework tests), a check that the oracle fixtures regenerate identically, packing, and publishing to NuGet on `v*` tags. An on-demand benchmark regression check (`benchmarks.yml`). See [RELEASING.md](RELEASING.md) and [CONTRIBUTING.md](CONTRIBUTING.md).
- `FullUnicode` lowercases title-case letters (`ǅ` → `ǆ`) on every runtime, not only where the runtime does it.

### Performance (rest of Phase 8)

- `Jaro`/`JaroWinkler`: bit-parallel on several 64-bit words for long inputs, the plain algorithm for inputs of up to 16 characters, and real caching in the `Jaro`, `JaroWinkler` and `JaroT<T>` instance classes. Jaro is now 0.26-1.4x the cost of Levenshtein (was 1.1-7x).
- `Scorers.Levenshtein(maxDistance)`: a built-in Levenshtein distance scorer; `Process.Cdist` scores it with SIMD (13x faster for 100 × 2,000 short strings).

### All RapidFuzz distance metrics (plan items #1-#7)

- Added `Hamming` (with `pad`), `Jaro`, `JaroWinkler` (with `prefixWeight`, 0 to 1), `Osa` (optimal string alignment), `DamerauLevenshtein` (unrestricted), `Prefix` and `Postfix`. Each has `Distance`, `Similarity`, `NormalizedDistance` and `NormalizedSimilarity` with `string` and generic span overloads, and all match Python RapidFuzz 3.14.6 on every oracle value.
- Added instance classes `Hamming`, `Jaro`, `JaroWinkler`, `Osa`, `DamerauLevenshtein` and their generic `...T<T>` versions.
- Added `Indel.Similarity`.
- **Behavior change:** `Fuzz.Ratio` and `Fuzz.TokenSortRatio` (and the cached and generic ratio scorers) now return 100 for two empty strings, as RapidFuzz and TheFuzz do. One empty string still scores 0.
- **Behavior change:** `Fuzz.WeightedRatio` and `CachedWeightedRatioScorer` now use `PartialTokenSortRatio` and `PartialTokenSetRatio` when one string is at least 1.5 times longer than the other, as RapidFuzz's `WRatio` does. Scores in that case are often higher, which can change `Process.Extract*` results (the default scorer). All 410 oracle sentence pairs now match RapidFuzz.
- Fixed `CachedWeightedRatioScorer.Dispose` not disposing its token-set scorer.
- Added `Fuzz.QRatio`, `Fuzz.TokenRatio`, `Fuzz.PartialTokenRatio` and `Fuzz.PartialRatioAlignment` (returns the new `ScoreAlignment` struct), with scorers `QRatioScorer`, `TokenRatioScorer` and `PartialTokenRatioScorer`. All match RapidFuzz on every oracle sentence pair, including alignment positions (plan items #17-#20).
- Added `scoreCutoff` overloads to every `Fuzz` scorer: `X(string, string, double scoreCutoff)` and `X(string, string, Func<string, string> preprocessor, double scoreCutoff)`. Scores below the cutoff return 0; `Ratio` and `QRatio` skip pairs whose lengths cannot reach it. They are separate overloads (not optional parameters) so method-group conversions keep working (plan item #21).
- Added `ReadOnlySpan<char>` overloads of `Ratio`, `QRatio`, `PartialRatio` and `ApproximateSubstringRatio`, with and without `scoreCutoff` (plan item #22).
- Added cached scorers `CachedQRatioScorer`, `CachedPartialRatioScorer` (reuses the query's bit pattern when the query is the shorter input), `CachedPartialTokenSortScorer`, `CachedTokenRatioScorer` and `CachedPartialTokenRatioScorer` (plan item #23).
- Added set-based metrics `Jaccard`, `SorensenDice`, `Overlap` and `Tanimoto` on q-grams (`QGramOptions`: length, padding, set or multiset counting) and on any `IEnumerable<T>`, plus `QGrams.Of` (plan items #8-#12). Written from the definitions, not from kdjones/fuzzystring, whose versions count distinct characters and score `Dice("aaaa", "aaaa")` as 0.25.
- Added `RatcliffObershelp` (matches Python's `difflib.SequenceMatcher.ratio()`, with optional `autoJunk`), `LongestCommonSubstring` (`Find`, `Of`, normalized similarity) and `LongestCommonSubsequence.Of` (plan items #13-#15).
- Added `ApproximatelyEquals` extension methods with `SimilarityTolerance` and `SimilarityMetric` to combine metrics into a yes/no answer (plan item #16).
- Added distance scorers for searches: `Scorers.FromDistance(f, maxDistance)` returns an `IDistanceScorer`; `Process.Extract*` and pipelines then return the smallest distances first and keep only results within `maxDistance` (null = no limit, 0 = exact only). The `cutoff` argument stays a minimum similarity and is rejected for distance scorers. Also `Scorers.FromSimilarity` (plan item #26).
- Added `Process.Cdist` and `Process.Cpdist` (as RapidFuzz's `cdist`/`cpdist`): matrix and pairwise scoring with optional multi-threading (`workers`), cancellation, a score cutoff, and built-in scorers prepared once per query (plan items #24, #25).
- Added `Process.Dedupe` (as TheFuzz's `dedupe`), with results in order of first appearance (plan item #27).
- **Changed:** `Levenshtein.GetEditOps` and `LongestCommonSubsequence.GetEditOps` return the new `EditOps` type (with `SourceLength`/`DestLength`, `Apply`, `Inverse`, `RemoveSubsequence`, `AsOpCodes`, `AsMatchingBlocks`) instead of `EditOp[]`. It has `Length` and converts implicitly to `EditOp[]`, so most existing code compiles unchanged. Added `OpCodes` (ranges, with `Apply`, `Inverse`, `AsEditOps`), `Levenshtein.GetOpCodes`, and `Indel.GetEditOps` / `Indel.GetOpCodes`, all matching RapidFuzz (plan items #28, #29).
- Added `scoreCutoff` and `scoreHint` to `Levenshtein.Distance`: distances above the cutoff stop early and return `scoreCutoff + 1`; the hint speeds up close strings. Close strings use Ukkonen's banded search. About 8x faster than the full distance on unrelated 1,000-character strings with a cutoff of 5 (plan item #30).
- **Fixed:** token scorers (`TokenSortRatio`, `TokenSetRatio`, their partial and cached versions, and `WeightedRatio`) sorted words with the current culture, so results differed from RapidFuzz for mixed-case or accented words and could change with the machine's culture (`TokenSortRatio("Zoo apple", "apple zoo")` was 88.9, now 55.6 as in RapidFuzz). Words are now sorted ordinally. Inherited from Raffinert.FuzzySharp.
- Added `StringPreprocessor.RemoveDiacritics`, `StringPreprocessor.Compose` and `StringPreprocessor.FullUnicode` (matches RapidFuzz's `default_process`, including numbers like `²`/`½`, `İ`→`i`, and characters outside the BMP) (plan items #31, #32).
- Added `CodePoints` (`Compare`, `Scorer`, `Count`, `Of`): compares whole Unicode characters with any metric, so emoji and other characters outside the BMP count once, as in RapidFuzz (plan item #31).
- Performance (Phase 8, see [docs/benchmarks.md](docs/benchmarks.md)):
  - `Process.Cdist` with `Ratio` or `QRatio` scores choices of up to 64 characters many at a time with SIMD (`System.Numerics.Vector<T>`, .NET Core 3.0+), with results identical to the scalar path: 3-4x faster and 2.5-6.5x less memory for 100 × 2,000 strings (plan item #33).
  - `Osa` is bit-parallel at every length (multi-word above 64 characters): about 2x the cost of `Levenshtein` instead of 38-67x (P8.1).
  - `Jaro` / `JaroWinkler` use a bit-parallel algorithm when both inputs fit in 64 characters after trimming to the match window (P8.2).
  - `DamerauLevenshtein` on strings uses an array instead of a dictionary for characters below 256: 1.7-2x faster (P8.3).

### Rebrand of Raffinert.FuzzySharp v6.0.1 as Fuzzion (planned 0.1.0-preview.1, never published)

- **Breaking:** Package ID is now `Fuzzion`; assembly name is `Fuzzion`.
- **Breaking:** Root namespace `Raffinert.FuzzySharp` is now `Fuzzion` (sub-namespaces keep their names, for example `Fuzzion.PreProcess`).
- The public API matches Raffinert.FuzzySharp v6.0.1. Scores match too, apart from the two bug fixes below.
- Added `THIRD-PARTY-NOTICES.md` (shipped inside the NuGet package) and the [implementation plan](docs/IMPLEMENTATION-PLAN.md).
- Fixed `Indel.NormalizedDistance` dividing by the lengths left after trimming the shared prefix and suffix instead of the full lengths (for example `("abc", "abcd")` returned 1.0 instead of 1/7). Inherited from Raffinert.FuzzySharp v6.0.1.
- Fixed `LongestCommonSubsequence.NormalizedSimilarity` returning 0 instead of 1 for two empty sequences.
- Added `Fuzz.ApproximateSubstringRatio`: the best approximate occurrence of the shorter input inside the longer one, using bit-parallel Indel distance, with stateless and cached scorers. Faster than `PartialRatio` (3.5-9.8x in the upstream benchmarks) but not numerically equal to it. Merged from [Raffinert/FuzzySharp#31](https://github.com/Raffinert/FuzzySharp/pull/31) by Yevhen Cherkes, converted to `double` scores, and changed so the non-cached string path also uses the exact-match fast path (about 37x faster on an exact match in a 4,096-character text).
- Added `Indel.BestSubstringMatch`, which returns the best raw Indel distance of a pattern inside a text and the first endpoint that strictly improves on deleting the whole pattern.
- Added oracle tests that compare Fuzzion with Python RapidFuzz 3.14.6 (`tools/oracle`, `Fuzzion.Tests/Oracle`). Deliberate differences are listed in [docs/known-differences.md](docs/known-differences.md).

## History inherited from Raffinert.FuzzySharp

## v6.0.1

- Fixed `PartialRatio` false exact matches for near-perfect long strings.
- Clarified `PartialRatio` empty-input semantics: two empty inputs score 100; one empty input scores 0.
- Corrected cached `WeightedRatio` token-sort scoring to match uncached results and dispose its owned strategy.
- Added validation for `ExtractTop` limits; zero performs no work and negative limits throw `ArgumentOutOfRangeException`.
- Made the default string preprocessor's casing culture-invariant.

## v6.0.0

*Double-precision scoring and simplified distance APIs*

- **Breaking:** Removed `scoreCutoff` from the Indel, Levenshtein, LCS, and partial-ratio APIs.
- **Breaking:** Scorers, scoring strategies, `Fuzz` methods, and extracted-result scores now use `double` and preserve fractional similarity values.
- Extraction `cutoff` parameters now accept `double` values.
- Optimized pattern-match-vector construction with bulk character population, a dedicated single-block fast path, lazy non-ASCII storage, and direct dense-mask lookup for ASCII characters.

## v5.0.3

*Extractor selection performance and allocation improvements*

- Optimized `ExtractOne` and `ExtractTop` across the standard, cached, parallel, and parallel-cached extractors by selecting candidates directly instead of materializing every qualifying `ExtractedResult` and then using LINQ `Max` or `MaxN`.
- `ExtractOne` now tracks the best candidate during scoring; `ExtractTop` retains only the requested number of candidates in a bounded heap.
- Parallel extractors score candidates independently and reduce local results while preserving score ordering and first-occurrence tie-breaking.
- Added extractor selection regression tests and benchmarks for sequential, cached, parallel, and parallel-cached paths.

## v5.0.2

*Levenshtein/LCS correctness and cutoff semantics*

- Fixed `Levenshtein.Distance<T>` weighted dispatch so non-fast-path weights correctly use `GenericDistance` and return the computed value.
- Corrected `LevenshteinMaximum` length-difference math and aligned `Similarity` cutoff handling with RapidFuzz semantics by converting similarity cutoffs to distance cutoffs.
- Fixed `NormalizedSimilarity` cutoff flow to use the corresponding normalized-distance cutoff (`1 - similarityCutoff`).
- Made Levenshtein cutoff short-circuit strict-safe in both single- and multi-block paths using a remaining-length lower bound (`dist > cutoff + remaining`).
- Added Levenshtein tests for weighted cases, cutoff behavior, maximum formula, and recoverable-prefix cutoff safety.
- Updated multi-block LCS update logic and matrix construction to use the correct `u = S & U` transition form with consolidated per-block loops.

## v5.0.1

*LongestCommonSubsequence matrix correctness for long inputs*

- Fixed `LongestCommonSubsequence` matrix update logic for `s1.Length > 64` to match RapidFuzz LCSseq references.
- Corrected transition from using raw mask arithmetic (`S +/- U`) to the proper bit-parallel form based on `u = S & U`, then `S = (S + u) | (S - u)`.
- Refactored the multi-block add/sub computation into a single per-block loop while preserving carry/borrow behavior.
- This fixes incorrect early matrix rows (including the second-row failure case) and downstream reconstruction issues in `GetEditOps` and `MatchingBlocks` for long sequences.

## v5.0.0

*Breaking API changes and preprocessing fixes*

- **Breaking:** Replaced `PreprocessMode`-based APIs with `Func<string, string>` preprocessors across public scoring/extraction APIs. Calls that passed `PreprocessMode.Full`/`PreprocessMode.None` must now pass `StringPreprocessor.Full`/`StringPreprocessor.None` (or a custom delegate).
- **Breaking:** Renamed `PreProcess.StringPreprocessorFactory` to `PreProcess.StringPreprocessor`.
- **Breaking:** Renamed generic extraction methods from `Process.Extract*` to `Process.Extract*By` when an extractor delegate is provided (for example, `ExtractOne` -> `ExtractOneBy`, `ExtractTop` -> `ExtractTopBy`).
- Fixed missing preprocessing in several internal extraction/scoring paths so configured preprocessors are now applied consistently.

## v4.0.2

- Added `string query` overloads for generic choice extraction, allowing a plain string query to be matched against `IEnumerable<T>` choices with a `Func<T, string>` processor.
- Added `ExtractOne`, `ExtractSorted`, and `ExtractTop` support for this overload shape in the sequential and parallel result extractors.
- Extended the `Process` API for string-query generic choice extraction and added matching `ProcessPipeline` overloads for `ExtractTop` and `ExtractOne`.
- Added tests covering string-query generic choice extraction for `Process` and `ProcessPipeline`.

## v4.0.1
- Removed unnecessary abstractions CachedScorerBase and CachedStrategySensitiveScorerBase.

## v4.0.0

*Fluent Pipeline API, Caching, and Parallelism*

- **New fluent builder API** (`Process.Configure()`) for building reusable, immutable pipelines with preconfigured scoring, caching, and parallel execution.
- **Automatic caching** (`.Cached()`) creates a `CachedWeightedRatioScorer` per extraction call for improved performance.
- **External scorer caching** (`.Cached(scorer)`) enables across-run caching by reusing a pre-initialized `ICachedRatioScorer`.
- **Parallel execution** (`.Parallel()`) with configurable `ParallelOptions` including `CancellationToken` support.
- Builder methods are **order independent** — `.Cached().Parallel()` and `.Parallel().Cached()` produce identical results.
- **Type-safe builder design** — the type system prevents invalid configurations (e.g., passing an `IRatioScorer` to a cached pipeline).

## v3.0.9

Implemented RapidFuzz-style window pruning for the full-width partial ratio scan so it avoids evaluating every window when len2 > len1, and adjusted the suffix loop to cover the last full-width window.

## v3.0.8

Removed finalizers from CharMaskBuffer and DictionarySlimPooled as they don't use unmanaged resources.

## v3.0.7

Improved LongestCommonSequence for faster execution. This caused the PartialRatioStrategy speedup.

## v3.0.6

Optimized TokenInitialismRatio for faster execution and reduced memory allocations.

## v3.0.5

Return netstandard2.0 support, small code cleanup.

## v3.0.4

Remove unnecessary dependency, thanks to @laicasaane.

## v3.0.3

Fix partial ratio issue with empty strings, more tests.

## v3.0.1, v3.0.2

Fix critical issue with strings that contain more than 64 unique characters. The issue was introduced in v3.0.0.

## v3.0.0

*Partial Ratio Accuracy and Performance Update*

- **Fixes multiple bugs in the Partial Ratio implementation.** In earlier versions, `Fuzz.PartialRatio` could return suboptimal scores in certain cases (for example, when a short string appeared multiple times in a longer string, it didn't always pick the highest scoring match).
- **Performance Optimizations:** All distance calculations were rewritten to use bit-parallel algorithms. Additionally, the `Levenshtein.Instance`, `Indel.Instance` and `LongestCommonSequence.Instance` classes may help get max speedup — see [BenchmarkAll.FuzzySharpDistanceFrom](https://github.com/Raffinert/FuzzySharp/blob/master/FuzzySharp.Benchmarks/BenchmarkAll.cs#L213).
- Bit-parallel implementations are highly borrowed from the MIT-licensed Python library [RapidFuzz](https://github.com/rapidfuzz/RapidFuzz).

## v2.0.3

Accent to performance and allocations. See [Benchmark](https://github.com/Raffinert/FuzzySharp/blob/dc2b858dc4cc56d8cdf26411904e255a019b0549/FuzzySharp.Benchmarks/BenchmarkDotNet.Artifacts/results/Raffinert.FuzzySharp.Benchmarks.BenchmarkAll-report-github.md).
Support local languages more naturally (removed regexps "a-zA-Z"). All regexps were replaced with string manipulations (fixes [PR!7](https://github.com/JakeBayer/FuzzySharp/pull/7)).
Extra performance improvement, reused approach [Dmitry Sushchevsky](https://github.com/blowin) — see [PR!42](https://github.com/JakeBayer/FuzzySharp/pull/42).
Implemented new `Process.ExtractAll` method, see [Issue!46](https://github.com/JakeBayer/FuzzySharp/issues/46).
Remove support of outdated/vulnerable platforms netcoreapp2.0; netcoreapp2.1; netstandard1.6.

## v2.0.0

As of 2.0.0, all empty strings will return a score of 0. Prior, the partial scoring system would return a score of 100, regardless if the other input had correct value or not. This was a result of the partial scoring system returning an empty set for the matching blocks. As a result, this led to incorrect values in the composite scores; several of them (token set, token sort) relied on the prior value of empty strings.

As a result, many 1.X.X unit tests may be broken with the 2.X.X upgrade, but the upgrade to the 2.X.X series is recommended regardless, as it is closer to the ideal behavior of the library.
