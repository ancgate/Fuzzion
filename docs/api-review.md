# API review before 1.0

The plan's Phase 9 asks for an API review before freezing the public API. This page
records what was checked, what changed, and what was deliberately kept. The frozen
surface is `Fuzzion.Tests/PublicApi/Fuzzion.api.txt`, enforced by `PublicApiTests`.

## Shape of the API

| Area | Entry points | Notes |
|---|---|---|
| Two-string scores (0-100) | `Fuzz.*` (17 scorers + `PartialRatioAlignment`) | Each has `(a, b)`, `(a, b, preprocessor)`, `(a, b, scoreCutoff)` and `(a, b, preprocessor, scoreCutoff)`; `Ratio`, `QRatio`, `PartialRatio`, `ApproximateSubstringRatio` also take `ReadOnlySpan<char>`. |
| Distance metrics | `Levenshtein`, `Indel`, `LongestCommonSubsequence`, `Hamming`, `Jaro`, `JaroWinkler`, `Osa`, `DamerauLevenshtein`, `Prefix`, `Postfix` | Same four functions everywhere (`Distance`, `Similarity`, `NormalizedDistance`, `NormalizedSimilarity`), `string` and `ReadOnlySpan<T>` overloads, instance classes and `...T<T>` generic instances (not for `Prefix`/`Postfix`). |
| Set and block metrics | `Jaccard`, `SorensenDice`, `Overlap`, `Tanimoto` (+ `QGrams`, `QGramOptions`), `RatcliffObershelp`, `LongestCommonSubstring` | `Similarity`/`Distance`; q-gram options or any `IEnumerable<T>`. |
| Searching | `Process.Extract*`, `Process.Configure()` pipelines, `Process.Cdist`, `Process.Cpdist`, `Process.Dedupe` | Scorers: `IRatioScorer`, `ICachedRatioScorer`, `IDistanceScorer`, `Scorers.FromDistance` / `FromSimilarity` / `Levenshtein`. |
| Edit operations | `EditOps`, `OpCodes` (namespace `Fuzzion.Edits`) | From `Levenshtein.GetEditOps/GetOpCodes`, `Indel.GetEditOps/GetOpCodes`, `LongestCommonSubsequence.GetEditOps`. |
| Text | `StringPreprocessor` (`Full`, `None`, `FullUnicode`, `RemoveDiacritics`, `Compose`), `CodePoints` | |
| Yes/no | `StringSimilarity.ApproximatelyEquals`, `SimilarityTolerance`, `SimilarityMetric` | |

## Changes made in the review

| Change | Why |
|---|---|
| Added `string` overloads to `Indel` and `LongestCommonSubsequence` (`Distance`, `Similarity`, `NormalizedDistance`, `NormalizedSimilarity`) | They only accepted spans, so `Indel.Distance("a", "b")` did not compile, unlike every other metric. Additive. |
| Renamed `Tolerance` to `SimilarityTolerance` | `Fuzzion.Tolerance` is a very generic public name that would clash with types in user code. Nothing has been published, so the rename is free now. |
| Froze the public API with `PublicApiTests` | Any further change is a visible, reviewed decision. |
| Turned on the XML documentation file and package validation | IntelliSense docs in the package; the API is checked to be identical on all 12 target frameworks. |

## Kept as is, on purpose

| Item | Reason |
|---|---|
| `EditType` values are upper case (`DELETE`, `INSERT`, ...) | Inherited from FuzzySharp; renaming would break code migrating from FuzzySharp / Raffinert.FuzzySharp for a cosmetic gain. |
| `ScorerCache.Get<T>()` and scorer classes under `Fuzzion.SimilarityRatio.Scorer.*` | Inherited, widely used by FuzzySharp users. `Scorers` (new) is the friendlier entry point. |
| `Fuzz` methods return `double`, distances return `int` (Jaro: `double`) | Raffinert v6 convention; Jaro has no integer distance. |
| UTF-16 by default, code points through `CodePoints` | Zero cost by default; see `docs/known-differences.md`. |
| `GetEditOps` returns `EditOps` (with implicit conversion to `EditOp[]`) instead of keeping a second `EditOp[]` method | Fewer methods; existing code keeps compiling. |
| The old `AsOpCodes(this IEnumerable<EditOp>, int, int)` extension ignores `REPLACE` | Inherited; documented. `EditOps.AsOpCodes()` is the correct one. Candidate for `[Obsolete]` in 1.x. |
| No `[Obsolete]` members | Nothing needed deprecating before the first release. |
| Helper types stay public: `Fuzzion.Utils` (`Heap<T>`, `MinHeap<T>`, `Permutation`, `Permutor<T>`, `PatternMatchVector`, `IPatternMatchVector<T>`), `Fuzzion.Extractor.ResultExtractor` (and nested classes), `Fuzzion.Extensions` | They were public in Raffinert.FuzzySharp v6, and Fuzzion promises the same API to migrating users; `IPatternMatchVector<T>` also appears in public `Indel.Block*` methods. Candidates to make internal in a 2.0. |

## XML documentation

Every public and protected member has XML docs, and a missing one now fails the build
(`CS1591` is an error in `Fuzzion.csproj`). The 247 members inherited from
Raffinert.FuzzySharp without docs were documented after reading their code, and the
docs state the behaviour below where it can surprise a caller.

## Inherited behaviour found while documenting

None of this changes in 1.0 (the API is frozen and these are inherited from
Raffinert.FuzzySharp); each is documented on the member. Candidates for 1.x or 2.0:

| Member | Behaviour | Possible change |
|---|---|---|
| `ResultExtractor.Parallel`, `.Cached`, `.Parallel.Cached` | A `null` processor throws `NullReferenceException`; the sequential overloads use `StringPreprocessor.Full` instead. `ResultExtractor` does not handle distance scorers (only `Process` and the pipelines do). | Same default everywhere; argument checks. `Process` is the supported entry point. |
| `CachedWeightedRatioScorer` | Caches the simple, token sort, token set and partial token set parts; the partial ratio and partial token sort parts are recomputed for every choice. | Use `CachedPartialRatioScorer` / `CachedPartialTokenSortScorer` inside it (speed only). |
| `EditOpsExtensions.AsOpCodes(ops, srcLen, destLen)` | Wrong with `REPLACE` operations (overlapping `KEEP` ranges); correct for insert/delete only. | `[Obsolete]`, pointing to `EditOps.AsOpCodes()`. |
| `EnumerableExtensions.MaxN` / `MaxNBy` | Return the top n smallest first; `n <= 0` on a non-empty source throws when enumerated. | Make internal in 2.0. |
| `Permutation.IsPermutationOf` | Compares as sets: `[a, a, b]` counts as a permutation of `[b, a]`. | Make internal in 2.0. |
| `Indel`, `Levenshtein`, `LongestCommonSubsequence` instances | Treat a `null` source or value as empty; the newer `Osa`, `Jaro`, ... instances throw `ArgumentNullException`. | One rule for all. |
| `ICachedStrategy`, `CachedScorerBase<T>`, `ICachedRatioScorer<T>`, `Scorer`, `CachedScorer` | Public but unused by the library, or with only internal implementations. | Make internal in 2.0. |
| `ExtractedResult<T>.CompareTo` | Compares by score only, lowest first. | Documented; keep. |
