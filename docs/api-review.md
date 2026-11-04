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

## Undocumented members

`GenerateDocumentationFile` is on, so the package ships IntelliSense docs. 247 public
members have no XML docs; all are inherited from Raffinert.FuzzySharp (mostly scorer and
strategy classes under `Fuzzion.SimilarityRatio`, the helper types above, `EditOp`,
`OpCode`, `MatchingBlock` and the `ResultExtractor` overloads). Members added by Fuzzion
are documented. The "missing XML comment" warning (CS1591) is suppressed so that real
doc errors stay visible; filling in the inherited docs is a documentation-only follow-up
that does not affect the API freeze. The review fixed the malformed doc comments it found
(an unescaped `<=` in two inherited comments, a misplaced comment and missing `param` tags).
