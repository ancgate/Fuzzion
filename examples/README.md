# Fuzzion examples

30 small, runnable programs that show how to use Fuzzion, from a first comparison to
complete recipes such as "did you mean", record linkage and log grouping. Each file is
self-contained: copy the parts you need into your own code.

## Running them

You need the .NET 8 SDK or later. From the repository root:

```bash
dotnet run --project examples/Fuzzion.Examples
```

lists the examples. Run one by number or name, or all of them:

```bash
dotnet run --project examples/Fuzzion.Examples -- 7
```

```bash
dotnet run --project examples/Fuzzion.Examples -- did-you-mean
```

```bash
dotnet run --project examples/Fuzzion.Examples -- all
```

The examples use the library's source through a project reference. In your own project,
install the package instead:

```bash
dotnet add package Fuzzion --prerelease
```

CI runs every example on Linux and Windows, so they stay in step with the library.

## Where to start

| I want to ... | See |
|---|---|
| Compare two strings | [1](Fuzzion.Examples/Basics/01_QuickStart.cs), [2](Fuzzion.Examples/Basics/02_ChoosingAScorer.cs) |
| Ignore case, punctuation or accents | [3](Fuzzion.Examples/Basics/03_Preprocessing.cs), [22](Fuzzion.Examples/Advanced/22_UnicodeText.cs) |
| Find the best match in a list | [7](Fuzzion.Examples/Searching/07_SearchAList.cs), [8](Fuzzion.Examples/Searching/08_SearchObjects.cs) |
| Search a large list quickly, or many queries at once | [9](Fuzzion.Examples/Searching/09_Pipelines.cs), [11](Fuzzion.Examples/Searching/11_ScoreMatrix.cs), [19](Fuzzion.Examples/Metrics/19_ReusableInstances.cs) |
| Allow a fixed number of typos | [10](Fuzzion.Examples/Searching/10_DistanceSearch.cs), [24](Fuzzion.Examples/Recipes/24_DidYouMean.cs) |
| Remove duplicates from a list | [12](Fuzzion.Examples/Searching/12_Deduplication.cs) |
| Match people, companies or addresses | [14](Fuzzion.Examples/Metrics/14_JaroWinklerNames.cs), [25](Fuzzion.Examples/Recipes/25_CompanyNames.cs), [26](Fuzzion.Examples/Recipes/26_RecordLinkage.cs) |
| Get a yes/no answer | [18](Fuzzion.Examples/Metrics/18_ApproximatelyEquals.cs) |
| Show what changed between two texts | [21](Fuzzion.Examples/Advanced/21_EditOperations.cs), [28](Fuzzion.Examples/Recipes/28_DiffHighlighter.cs) |
| Clean up a column of free-text values | [29](Fuzzion.Examples/Recipes/29_CanonicalValues.cs) |

## All examples

### Basics

| # | Example | Shows |
|---|---|---|
| 1 | [Quick start](Fuzzion.Examples/Basics/01_QuickStart.cs) | `Fuzz.Ratio`, `Fuzz.TokenSortRatio`, `Process.ExtractOne`, `Process.ExtractTop`, `Levenshtein.Distance` |
| 2 | [Every Fuzz scorer side by side](Fuzzion.Examples/Basics/02_ChoosingAScorer.cs) | All 17 `Fuzz` scorers on typos, word order, repeats, substrings, abbreviations and initials, and when to use each |
| 3 | [Cleaning text before comparing](Fuzzion.Examples/Basics/03_Preprocessing.cs) | `StringPreprocessor.None`, `Full`, `FullUnicode`, `RemoveDiacritics`, `Compose`, your own preprocessor, defaults of `Fuzz` vs `Process` |
| 4 | [Finding one string inside another](Fuzzion.Examples/Basics/04_PartialMatching.cs) | `PartialRatio`, `PartialRatioAlignment` (where it matched), `ApproximateSubstringRatio`, `Indel.BestSubstringMatch` |
| 5 | [Word order, duplicates, abbreviations, initialisms](Fuzzion.Examples/Basics/05_TokenScorers.cs) | `TokenSortRatio`, `TokenSetRatio`, `TokenRatio`, `TokenDifferenceRatio`, `TokenInitialismRatio`, `TokenAbbreviationRatio` and the partial versions |
| 6 | [Score cutoffs and spans](Fuzzion.Examples/Basics/06_CutoffsAndSpans.cs) | `scoreCutoff` on `Fuzz` scorers, `Levenshtein.Distance` with `scoreCutoff` / `scoreHint`, `ReadOnlySpan<char>` input |

### Searching

| # | Example | Shows |
|---|---|---|
| 7 | [Best matches in a list of strings](Fuzzion.Examples/Searching/07_SearchAList.cs) | `ExtractOne`, `ExtractTop`, `ExtractSorted`, `ExtractAll`, `limit`, `cutoff`, `null` when nothing matches, `ScorerCache.Get<T>()` |
| 8 | [Best matches in a list of objects](Fuzzion.Examples/Searching/08_SearchObjects.cs) | `ExtractTopBy`, `ExtractOneBy`, `ExtractAllBy` with records, string queries and object queries |
| 9 | [Reusable, cached and parallel pipelines](Fuzzion.Examples/Searching/09_Pipelines.cs) | `Process.Configure()`, `WithScorer`, `Cached()`, `Cached(scorer)`, `Parallel`, cancellation |
| 10 | [Searching by distance](Fuzzion.Examples/Searching/10_DistanceSearch.cs) | `Scorers.Levenshtein(maxDistance)`, `Scorers.FromDistance`, `Scorers.FromSimilarity` |
| 11 | [Every query against every choice](Fuzzion.Examples/Searching/11_ScoreMatrix.cs) | `Process.Cdist` (scores and distances, `workers`, `scoreCutoff`, cancellation), `Process.Cpdist` |
| 12 | [Removing and grouping near-duplicates](Fuzzion.Examples/Searching/12_Deduplication.cs) | `Process.Dedupe` and choosing its threshold; grouping duplicates with `Cdist` |

### Metrics

| # | Example | Shows |
|---|---|---|
| 13 | [Edit distances](Fuzzion.Examples/Metrics/13_EditDistances.cs) | `Levenshtein` (with weights), `Indel`, `LongestCommonSubsequence`, `Osa`, `DamerauLevenshtein`, `Hamming`, and which to use |
| 14 | [Jaro and Jaro-Winkler for names](Fuzzion.Examples/Metrics/14_JaroWinklerNames.cs) | `Jaro`, `JaroWinkler`, `prefixWeight`, a reusable `JaroWinkler` instance |
| 15 | [Shared beginnings and endings](Fuzzion.Examples/Metrics/15_PrefixPostfix.cs) | `Prefix`, `Postfix` on paths, file names and phone numbers |
| 16 | [Set metrics and q-grams](Fuzzion.Examples/Metrics/16_SetMetrics.cs) | `QGrams.Of`, `Jaccard`, `SorensenDice`, `Overlap`, `Tanimoto`, `QGramOptions`, word sets, tags, IDs |
| 17 | [Ratcliff-Obershelp and common substrings](Fuzzion.Examples/Metrics/17_BlocksAndSubstrings.cs) | `RatcliffObershelp` (Python difflib), `MatchingBlocks`, `LongestCommonSubstring`, `LongestCommonSubsequence.Of` |
| 18 | [Yes/no answers](Fuzzion.Examples/Metrics/18_ApproximatelyEquals.cs) | `ApproximatelyEquals`, `SimilarityTolerance`, `SimilarityMetric`, `StringSimilarity.AverageSimilarity` |
| 19 | [Comparing one string with many, faster](Fuzzion.Examples/Metrics/19_ReusableInstances.cs) | Instance classes (`new Levenshtein(query).DistanceFrom(...)`, ...) and cached `Fuzz` scorers |
| 20 | [Words, numbers and other sequences](Fuzzion.Examples/Metrics/20_GenericSequences.cs) | Generic `ReadOnlySpan<T>` overloads, word-level edits, `IndelT<T>`, `OsaT<T>` |

### Advanced

| # | Example | Shows |
|---|---|---|
| 21 | [Edit operations](Fuzzion.Examples/Advanced/21_EditOperations.cs) | `GetEditOps`, `GetOpCodes`, `Apply`, `Inverse`, conversions, matching blocks, a readable change log |
| 22 | [Accents, emoji and other Unicode](Fuzzion.Examples/Advanced/22_UnicodeText.cs) | `RemoveDiacritics`, `Full` vs `FullUnicode`, `CodePoints.Count` / `Of` / `Compare` / `Scorer` |
| 23 | [Writing your own scorer](Fuzzion.Examples/Advanced/23_CustomScorers.cs) | `Scorers.FromSimilarity`, `Scorers.FromDistance`, implementing `IRatioScorer`, using it in pipelines, `Cdist` and `Dedupe` |

### Recipes

| # | Example | Shows |
|---|---|---|
| 24 | ["Did you mean ...?"](Fuzzion.Examples/Recipes/24_DidYouMean.cs) | Spelling suggestions with Damerau-Levenshtein and a typo budget that grows with word length |
| 25 | [Matching company names](Fuzzion.Examples/Recipes/25_CompanyNames.cs) | A custom normalizer for legal suffixes, "&", accents; matching against a master list with a cutoff |
| 26 | [Matching customer records](Fuzzion.Examples/Recipes/26_RecordLinkage.cs) | A weighted score over name, email, phone and zip, with match / check / no-match thresholds |
| 27 | [Fuzzy autocomplete](Fuzzion.Examples/Recipes/27_Autocomplete.cs) | Typo-tolerant suggestions that favour a shared start and short choices |
| 28 | [Highlighting what changed](Fuzzion.Examples/Recipes/28_DiffHighlighter.cs) | Word and character diffs from `GetOpCodes<T>` |
| 29 | [Mapping messy input to known values](Fuzzion.Examples/Recipes/29_CanonicalValues.cs) | Aliases, a cutoff that sends unknown values to review, and a cache |
| 30 | [Grouping log messages](Fuzzion.Examples/Recipes/30_LogGrouping.cs) | Masking IDs and numbers, then grouping similar messages |
