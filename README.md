# Fuzzion

Fuzzion is a high-performance .NET fuzzy string matching library inspired by SeatGeek's FuzzyWuzzy and RapidFuzz.

Fuzzion is based on [Raffinert.FuzzySharp](https://github.com/Raffinert/FuzzySharp) by Yevhen Cherkes, itself based on [FuzzySharp](https://github.com/JakeBayer/FuzzySharp) by Jacob Bayer. It keeps their bit-parallel distance algorithms, [partial_ratio fixes](https://github.com/rapidfuzz/RapidFuzz/blob/main/api_differences.md#partial_ratio-implementation), cached scorers, and extraction pipelines, and extends them toward full [RapidFuzz](https://github.com/rapidfuzz/RapidFuzz) parity plus set-based similarity metrics. See the [implementation plan](docs/IMPLEMENTATION-PLAN.md) for the roadmap.

> **Status:** preview (`0.x`). The public API may change between minor versions until 1.0.

## Overview

Use Fuzzion when you need to:

- Score two strings for similarity.
- Find the best match in a list of choices.
- Match strings while ignoring word order, duplicate words, case, punctuation, or abbreviations.
- Reuse cached scorers for one-to-many comparisons.
- Run extraction pipelines sequentially or in parallel.
- Access fast Levenshtein, Indel, and Longest Common Subsequence distance APIs directly.

## Contents

- [Installation](#installation)
- [Compatibility](#compatibility)
- [Quick Start](#quick-start)
- [Choosing a Scorer](#choosing-a-scorer)
- [Ratio Examples](#ratio-examples)
- [Process Extraction](#process-extraction)
- [Fluent Pipeline API](#fluent-pipeline-api)
- [Using Different Scorers](#using-different-scorers)
- [Distance APIs](#distance-apis)
- [String Preprocessors](#string-preprocessors)
- [Performance](#performance)
- [Migration Notes](#migration-notes)
- [Credits](#credits)
- [Support](#support)

## Installation

```powershell
Install-Package Fuzzion -Prerelease
```

or

```bash
dotnet add package Fuzzion --prerelease
```

## Compatibility

Fuzzion targets:

- .NET Standard 2.0 and 2.1
- .NET Framework 4.5, 4.6, 4.6.2, 4.7.2, and 4.8
- .NET Core 3.1
- .NET 6, .NET 8, .NET 9, and .NET 10

## Quick Start

Find the best match from a list of choices:

```csharp
var choices = new[] { "Atlanta Falcons", "New York Jets", "New York Giants", "Dallas Cowboys" };

var result = Process.ExtractOne("cowboys", choices);
// (string: Dallas Cowboys, score: 90, index: 3)
```

Compare two strings directly:

```csharp
Fuzz.Ratio("mysmilarstring", "mysimilarstring");
// 96.55172413793103

Fuzz.WeightedRatio(
    "The quick brown fox jimps ofver the small lazy dog",
    "the quick brown fox jumps over the small lazy dog");
// 94.94949494949495
```

Similarity scores are `double` values in the range 0–100.

By default, `Fuzz` methods compare strings as-is. `Process` extraction methods use `StringPreprocessor.Full` by default, which normalizes whitespace, lowercases, and strips non-alphanumeric characters.

## Choosing a Scorer

| Scorer | Use when |
|--------|----------|
| `Fuzz.Ratio` | You need direct similarity between two strings. |
| `Fuzz.QRatio` | Like `Ratio`, but an empty input always scores 0 (even two empty inputs). |
| `Fuzz.PartialRatio` | One string may be a substring or close substring of the other. |
| `Fuzz.ApproximateSubstringRatio` | You need the best Indel-distance match of the shorter input within the longer input. |
| `Fuzz.TokenSortRatio` | Word order should not matter. |
| `Fuzz.TokenSetRatio` | Duplicate words or extra common words should have less impact. |
| `Fuzz.TokenRatio` | You want the better of `TokenSortRatio` and `TokenSetRatio`. |
| `Fuzz.PartialTokenRatio` | You want the better of the partial token scores (100 when any word is shared). |
| `Fuzz.TokenInitialismRatio` | You need to compare an initialism with its expanded phrase. |
| `Fuzz.TokenAbbreviationRatio` | You need abbreviation-aware matching. |
| `Fuzz.WeightedRatio` | You want a general-purpose scorer that combines several strategies. |
| `Process.ExtractOne` / `ExtractTop` / `ExtractAll` | You need to search a collection of choices. |
| `Process.Configure()` | You want reusable extraction with caching, custom scorers, or parallel execution. |

## Ratio Examples

### Simple Ratios
<p align="right"><a href="https://dotnetfiddle.net/9JpFTQ">Run .NET fiddle</a></p>

```csharp
Fuzz.Ratio("mysmilarstring", "myawfullysimilarstirng");
// 72.22222222222221
Fuzz.Ratio("mysmilarstring", "mysimilarstring");
// 96.55172413793103
```

### Partial Ratio
<p align="right"><a href="https://dotnetfiddle.net/rk0dIO">Run .NET fiddle</a></p>

```csharp
Fuzz.PartialRatio("similar", "somewhresimlrbetweenthisstring");
// 71.42857142857143
```

### Approximate Substring Ratio

```csharp
double score = Fuzz.ApproximateSubstringRatio(
    "invoice number 12345",
    "processed invoice number 12345 successfully");
// 100
```

`ApproximateSubstringRatio` searches for the best approximate occurrence of the
shorter input, considering every possible start position implicitly. It uses the
Indel edit model: insertions and deletions cost one, while a substitution costs
two. The score is normalized relative to the shorter input's length. Equal-length
inputs are evaluated in both directions unless the first direction is an exact
match, so the high-level score is symmetric. Its numeric results are not
equivalent to `PartialRatio`.

For endpoint information, call `Indel.BestSubstringMatch` directly; its
`FirstBestEndIndex` is the first endpoint that strictly improves on the distance
of deleting the complete pattern. Equal-distance later endpoints do not replace
it, so it is not a unique match boundary. This directional API does not return a
start index or edit script.

### Token Sort Ratio
<p align="right"><a href="https://dotnetfiddle.net/b5RVp2">Run .NET fiddle</a></p>

```csharp
Fuzz.TokenSortRatio("order words out of", "  words out of order");
// 100
Fuzz.PartialTokenSortRatio("order words out of", "  words out of order");
// 100
```

### Token Set Ratio
<p align="right"><a href="https://dotnetfiddle.net/ZfZRGb">Run .NET fiddle</a></p>

```csharp
Fuzz.TokenSetRatio("fuzzy was a bear", "fuzzy fuzzy fuzzy bear");
// 100
Fuzz.PartialTokenSetRatio("fuzzy was a bear", "fuzzy fuzzy fuzzy bear");
// 100
```

### Token Initialism Ratio
<p align="right"><a href="https://dotnetfiddle.net/87181A">Run .NET fiddle</a></p>

```csharp
Fuzz.TokenInitialismRatio("NASA", "National Aeronautics and Space Administration");
// 88.88888888888889
Fuzz.TokenInitialismRatio("NASA", "National Aeronautics Space Administration");
// 100

Fuzz.TokenInitialismRatio("NASA", "National Aeronautics Space Administration, Kennedy Space Center, Cape Canaveral, Florida 32899");
// 53.333333333333336
Fuzz.PartialTokenInitialismRatio("NASA", "National Aeronautics Space Administration, Kennedy Space Center, Cape Canaveral, Florida 32899");
// 100
```

### Token Abbreviation Ratio
<p align="right"><a href="https://dotnetfiddle.net/MVlwrW">Run .NET fiddle</a></p>

```csharp
Fuzz.TokenAbbreviationRatio("bl 420", "Baseline section 420", StringPreprocessor.Full);
// 40
Fuzz.PartialTokenAbbreviationRatio("bl 420", "Baseline section 420", StringPreprocessor.Full);
// 66.66666666666667
```

### Weighted Ratio
<p align="right"><a href="https://dotnetfiddle.net/n9QxAk">Run .NET fiddle</a></p>

```csharp
Fuzz.WeightedRatio("The quick brown fox jimps ofver the small lazy dog", "the quick brown fox jumps over the small lazy dog");
// 94.94949494949495
```

`WeightedRatio` gives the same results as RapidFuzz's `WRatio`. When one string is at least 1.5 times longer than the other, it compares the shorter one with parts of the longer one using the partial token scores. Because `PartialTokenSetRatio` is 100 whenever two strings share a word, short choices that share one common word with the query can score 85.5 (for example `"braves vs mets"` for any query containing "vs"). If that is too generous for your data, use `TokenSortRatio`, `TokenSetRatio` or `Ratio` as the scorer.

### Partial Ratio Alignment

`PartialRatioAlignment` returns the partial ratio together with where the best match is
in each string:

```csharp
ScoreAlignment a = Fuzz.PartialRatioAlignment("this is a test", "yes this is a test!");
// Score = 100, SourceStart = 0, SourceEnd = 14, DestStart = 4, DestEnd = 18
// so "yes this is a test!".Substring(4, 14) == "this is a test"
```

## Process Extraction

Find the best match(es) from a collection of choices.

<p align="right"><a href="https://dotnetfiddle.net/8lEzk3">Run .NET fiddle</a></p>

```csharp
Process.ExtractOne("cowboys", new[] { "Atlanta Falcons", "New York Jets", "New York Giants", "Dallas Cowboys" });
// (string: Dallas Cowboys, score: 90, index: 3)
```
```csharp
Process.ExtractTop("goolge", new[] { "google", "bing", "facebook", "linkedin", "twitter", "googleplus", "bingnews", "plexoogl" }, limit: 3);
// [(string: google, score: 83.33333333333334, index: 0), (string: googleplus, score: 75.00000000000001, index: 5), (string: plexoogl, score: 42.85714285714286, index: 7)]
```
```csharp
Process.ExtractAll("goolge", new[] { "google", "bing", "facebook", "linkedin", "twitter", "googleplus", "bingnews", "plexoogl" });
// [(string: google, score: 83.33333333333334, index: 0), (string: bing, score: 36, index: 1), ...]

// With score cutoff
Process.ExtractAll("goolge", new[] { "google", "bing", "facebook", "linkedin", "twitter", "googleplus", "bingnews", "plexoogl" }, cutoff: 40);
// [(string: google, score: 83.33333333333334, index: 0), (string: googleplus, score: 75.00000000000001, index: 5), (string: plexoogl, score: 42.85714285714286, index: 7)]
```
```csharp
Process.ExtractSorted("goolge", new[] { "google", "bing", "facebook", "linkedin", "twitter", "googleplus", "bingnews", "plexoogl" });
// [(string: google, score: 83.33333333333334, index: 0), (string: googleplus, score: 75.00000000000001, index: 5), (string: plexoogl, score: 42.85714285714286, index: 7), ...]
```

Extraction uses `WeightedRatio` and `Full` preprocessing by default. Override these in the method parameters to use different scorers and processing:
```csharp
Process.ExtractOne("cowboys", new[] { "Atlanta Falcons", "New York Jets", "New York Giants", "Dallas Cowboys" }, s => s, ScorerCache.Get<DefaultRatioScorer>());
// (string: Dallas Cowboys, score: 57.14285714285714, index: 3)
```

### Generic Type Extraction

<p align="right"><a href="https://dotnetfiddle.net/YDtl6k">Run .NET fiddle</a></p>

Extraction can operate on objects of any type. Use the `extractor` parameter to reduce the object to the string it should be compared on:
```csharp
var events = new[]
{
    new[] { "chicago cubs vs new york mets", "CitiField", "2011-05-11", "8pm" },
    new[] { "new york yankees vs boston red sox", "Fenway Park", "2011-05-11", "8pm" },
    new[] { "atlanta braves vs pittsburgh pirates", "PNC Park", "2011-05-11", "8pm" },
};
var query = new[] { "new york mets vs chicago cubs", "CitiField", "2017-03-19", "8pm" };
var best = Process.ExtractOneBy(query, events, strings => strings[0]);
// (value: { "chicago cubs vs new york mets", "CitiField", "2011-05-11", "8pm" }, score: 95, index: 0)
```

If the query is already a string, it can be matched directly against generic choices by providing only a choice extractor:
```csharp
var events = new[]
{
    new { Name = "new york mets vs chicago cubs", Venue = "CitiField" },
    new { Name = "chicago cubs vs chicago white sox", Venue = "Wrigley Field" },
    new { Name = "philadelphia phillies vs atlanta braves", Venue = "Citizens Bank Park" },
};

var query = "new york mets at chicago cubs";

var best = Process.ExtractOneBy(query, events, e => e.Name);
// best.Value.Name == "new york mets vs chicago cubs"

var top = Process.ExtractTopBy(query, events, e => e.Name, limit: 2);
```

## Fluent Pipeline API

The `Process.Configure()` fluent builder creates reusable, immutable pipelines with preconfigured scoring, caching, and parallel execution.

### Basic Pipeline

Equivalent to the static `Process` methods, but reusable across multiple queries:

<p align="right"><a href="https://dotnetfiddle.net/KEFXxm">Run .NET fiddle</a></p>

```csharp
var pipeline = Process.Configure().Build();

var result1 = pipeline.ExtractOne("cowboys", new[] { "Atlanta Falcons", "New York Jets", "New York Giants", "Dallas Cowboys" });
//(string: Dallas Cowboys, score: 90, index: 3)
var result2 = pipeline.ExtractOne(
    "chicago cubs",
    new[]
    {
        "Boston Red Sox",
        "Los Angeles Dodgers",
        "New York Yankees",
        "San Francisco Giants",
        "St. Louis Cardinals",
        "Houston Astros"
    });
//(string: San Francisco Giants, score: 45, index: 3)
```

### Custom Scorer

<p align="right"><a href="https://dotnetfiddle.net/6JVmU9">Run .NET fiddle</a></p>

```csharp
var pipeline = Process.Configure()
    .WithScorer(ScorerCache.Get<DefaultRatioScorer>())
    .Build();

var result = pipeline.ExtractOne("cowboys", new[] { "Atlanta Falcons", "New York Jets", "New York Giants", "Dallas Cowboys" });
//(string: Dallas Cowboys, score: 66.66666666666667, index: 3)
```

### Parallel Execution

Enable multi-threaded processing for large choice sets:
```csharp
var pipeline = Process.Configure()
    .Parallel()
    .Build();

var results = pipeline.ExtractAll("goolge", largeChoicesList);
```

With `ParallelOptions` for fine-grained control:
```csharp
var pipeline = Process.Configure()
    .Parallel(new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount })
    .Build();
```

### Cached Execution

<p align="right"><a href="https://dotnetfiddle.net/y6qMJm">Run .NET fiddle</a></p>

Automatic caching creates a `CachedWeightedRatioScorer` per extraction call, pre-initializing internal data structures for the query string:
```csharp
var pipeline = Process.Configure()
    .Cached()
    .Build();

var result = pipeline.ExtractOne("cowboys", new[] { "Atlanta Falcons", "New York Jets", "New York Giants", "Dallas Cowboys" });
```

### Cached + Parallel

Combine caching and parallelism. Builder methods are **order independent** -- `.Cached().Parallel()` and `.Parallel().Cached()` produce identical results:
```csharp
var pipeline = Process.Configure()
    .Cached()
    .Parallel(new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount })
    .Build();

var results = pipeline.ExtractAll("goolge", largeChoicesList);
```

### External Cached Scorer

For maximum performance when running the same query against different choice sets, provide an externally managed `ICachedRatioScorer`. The scorer pre-initializes once and is reused across all extraction calls:
```csharp
using var scorer = new CachedWeightedRatioScorer("new york mets at atlanta braves");

var pipeline = Process.Configure()
    .Cached(scorer)
    .Parallel()
    .Build();

var results1 = pipeline.ExtractAll(choiceSet1);
var results2 = pipeline.ExtractAll(choiceSet2);
```

> **Note:** External cached scorers implement `IDisposable`. Use `using` to ensure proper cleanup.

### CancellationToken Support

Pass a `CancellationToken` via `ParallelOptions` to cancel long-running parallel extractions:
```csharp
var cts = new CancellationTokenSource();

var pipeline = Process.Configure()
    .Cached()
    .Parallel(new ParallelOptions { CancellationToken = cts.Token })
    .Build();

// Throws OperationCanceledException if cancelled
var results = pipeline.ExtractAll(query, largeChoicesList).ToList();
```

## Using Different Scorers

### Non-Cached Scorers (`IRatioScorer`)

Stateless scorers for use with `Process` static methods and the `WithScorer()` builder method:
```csharp
var ratio              = ScorerCache.Get<DefaultRatioScorer>();
var partialRatio       = ScorerCache.Get<PartialRatioScorer>();
var approximateSubstring = ScorerCache.Get<ApproximateSubstringRatioScorer>();
var tokenSet           = ScorerCache.Get<TokenSetScorer>();
var partialTokenSet    = ScorerCache.Get<PartialTokenSetScorer>();
var tokenSort          = ScorerCache.Get<TokenSortScorer>();
var partialTokenSort   = ScorerCache.Get<PartialTokenSortScorer>();
var tokenAbbreviation  = ScorerCache.Get<TokenAbbreviationScorer>();
var partialTokenAbbrev = ScorerCache.Get<PartialTokenAbbreviationScorer>();
var weighted           = ScorerCache.Get<WeightedRatioScorer>();
```

### Cached Scorers (`ICachedRatioScorer`)

<p align="right"><a href="https://dotnetfiddle.net/Ykr94M">Run .NET fiddle</a></p>

Pre-initialize with a query string for repeated comparisons. These implement `IDisposable`:
```csharp
using var scorer = new CachedWeightedRatioScorer("search query");
double score = scorer.Score("candidate string");
// 27.14285714285714
```

Available cached scorers:
- `CachedWeightedRatioScorer` -- weighted combination (default for `.Cached()`)
- `CachedQRatioScorer`, `CachedPartialRatioScorer`, `CachedPartialTokenSortScorer`, `CachedTokenRatioScorer`, `CachedPartialTokenRatioScorer`
- `CachedDefaultRatioScorer` -- simple Levenshtein ratio
- `CachedApproximateSubstringRatioScorer` -- approximate substring Indel ratio
- `CachedTokenSortScorer` -- token sort ratio
- `CachedTokenSetScorer` -- token set ratio
- `CachedPartialTokenSetScorer` -- partial token set ratio
- `CachedTokenDifferenceScorer` -- token difference ratio

## Distance APIs

### Levenshtein Distance API

<p align="right"><a href="https://dotnetfiddle.net/JzzcwS">Run .NET fiddle</a></p>

Low-level access to the bit-parallel Levenshtein distance implementation:

```csharp
// Edit distance
int distance = Levenshtein.Distance("kitten", "sitting");
// 3

// Normalized similarity (1.0 = identical, 0.0 = completely different)
double similarity = Levenshtein.NormalizedSimilarity("kitten", "sitting");
// 0.5714285714285714

// Edit operations to transform one string into another
EditOp[] ops = Levenshtein.GetEditOps("kitten", "sitting");
// [REPLACE(0, 0), REPLACE(4, 4), INSERT(6, 6)]
```

### More Distance Metrics

Fuzzion also has the other RapidFuzz distance metrics. Each has `Distance`,
`Similarity`, `NormalizedDistance` and `NormalizedSimilarity`, with `string` and
generic `ReadOnlySpan<T>` overloads, and gives the same results as RapidFuzz.

| Class | Measures | Example |
|---|---|---|
| `Hamming` | Positions that differ (`pad: false` requires equal lengths) | `Hamming.Distance("karolin", "kathrin")` → 3 |
| `Jaro` | Matching characters within a window, and their order; 0 to 1 | `Jaro.Similarity("MARTHA", "MARHTA")` → 0.944 |
| `JaroWinkler` | Jaro plus a bonus for a shared start (`prefixWeight`, default 0.1) | `JaroWinkler.Similarity("MARTHA", "MARHTA")` → 0.961 |
| `Osa` | Edits plus swaps of two neighbours, each part edited once | `Osa.Distance("CA", "ABC")` → 3 |
| `DamerauLevenshtein` | Edits plus swaps of two neighbours, no restriction | `DamerauLevenshtein.Distance("CA", "ABC")` → 2 |
| `Prefix` / `Postfix` | Length of the shared start / end | `Prefix.Similarity("prefix", "preface")` → 4 |

`Indel.Similarity` is also available (`len1 + len2 - Distance`).

```csharp
double nameScore = JaroWinkler.NormalizedSimilarity("Jonathon", "Jonathan"); // 0.95
int typos = DamerauLevenshtein.Distance("recieve", "receive");               // 1
```

`Hamming`, `Jaro`, `JaroWinkler`, `Osa` and `DamerauLevenshtein` also have instance
classes (for example `new JaroWinkler(query).SimilarityWith(candidate)`) and generic
versions (`JaroWinklerT<T>`).

### Instance Distance Classes

The `Levenshtein`, `Indel`, and `LongestCommonSubsequence` classes also offer an **instance API** for one-to-many comparisons. The constructor pre-computes a bit-parallel pattern match vector from the source string, which is then reused across all subsequent calls. This avoids rebuilding the internal data structure on every comparison, giving a significant speedup when comparing one source against many targets.

All three implement `IDisposable` -- use `using` to return pooled arrays.

#### Levenshtein Instance

<p align="right"><a href="https://dotnetfiddle.net/hxUB72">Run .NET fiddle</a></p>

```csharp
using var lev = new Levenshtein("chicago cubs vs new york mets");

int d1 = lev.DistanceFrom("new york mets vs chicago cubs");
// 22
int d2 = lev.DistanceFrom("atlanta braves vs pittsburgh pirates");
// 26
```

#### Indel Instance

<p align="right"><a href="https://dotnetfiddle.net/UkyXk2">Run .NET fiddle</a></p>

Indel distance counts only insertions and deletions (no replacements). `NormalizedSimilarityWith` returns a value between 0.0 (completely different) and 1.0 (identical):

```csharp
using var indel = new Indel("chicago cubs");

int distance = indel.DistanceFrom("chicago white sox");
// 11
double similarity = indel.NormalizedSimilarityWith("chicago white sox");
// 0.6206896551724138
```

The static approximate-substring API returns the best raw Indel distance and its
endpoint:

```csharp
IndelSubstringMatch match = Indel.BestSubstringMatch(
    "invoice number 12345".AsSpan(),
    "processed invoice number 12345 successfully".AsSpan());
// match.FirstBestEndIndex identifies the first strict improvement (the final '5')
```

A generic variant `IndelT<T>` is available for comparing sequences of any `IEquatable<T>`:

```csharp
using var indel = new IndelT<string>(new[] { "hello", "world" });

int distance = indel.DistanceFrom(new[] { "hello", "there" });
// 2
double similarity = indel.NormalizedSimilarityWith(new[] { "hello", "there" });
// 0.5
```

#### LongestCommonSubsequence Instance

<p align="right"><a href="https://dotnetfiddle.net/s60rZT">Run .NET fiddle</a></p>

LCS distance is defined as `max(len1, len2) - LCS_length`:

```csharp
using var lcs = new LongestCommonSubsequence("chicago cubs");

int distance = lcs.DistanceFrom("chicago white sox");
// 8
```

## String Preprocessors
<p align="right"><a href="https://dotnetfiddle.net/cIQ6PB">Run .NET fiddle</a></p>

By default, `Fuzz` methods compare strings as-is. Pass `StringPreprocessor.Full` to normalize whitespace, lowercase, and strip non-alphanumeric characters before comparing:

```csharp
Fuzz.Ratio("new york mets", "NEW YORK METS");
// < 100 (case sensitive)

Fuzz.Ratio("new york mets", "NEW YORK METS", StringPreprocessor.Full);
// 100 (case insensitive after preprocessing)
```

`Process` extraction methods use `StringPreprocessor.Full` by default. The default processor lowercases with invariant Unicode casing, so its results are deterministic across request cultures. Pass `StringPreprocessor.None` (or a custom `processor` function) to override this behavior or provide locale-specific matching.

## Performance

The distance implementations use bit-parallel algorithms designed to reduce both execution time and allocations. The table below compares naive DP Levenshtein distance calculation (baseline), original FuzzySharp, Fastenshtein, Quickenshtein, and Raffinert.FuzzySharp (the code Fuzzion is based on; these results were measured upstream).

Random words of 3 to 1024 random chars (`LevenshteinLarge.cs`):

| Method                                                          | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0       | Gen1       | Allocated   | Alloc Ratio |
|-----------------------------------------------------------------|------------|------------|-----------|-------|---------|------------|------------|-------------|-------------|
| NaiveDp                                                         | 231.563 ms | 57.5403 ms | 3.1540 ms |  1.00 |    0.02 | 43500.0000 | 34500.0000 | 275312920 B |       1.000 |
| [FuzzySharp](https://github.com/JakeBayer/FuzzySharp)           | 141.820 ms |  4.0905 ms | 0.2242 ms |  0.61 |    0.01 |          - |          - |   1545732 B |       0.006 |
| [Fastenshtein](https://github.com/DanHarltey/Fastenshtein)      | 123.356 ms | 13.0959 ms | 0.7178 ms |  0.53 |    0.01 |          - |          - |     34028 B |       0.000 |
| [Quickenshtein](https://github.com/Turnerj/Quickenshtein)       |  12.918 ms | 12.8046 ms | 0.7019 ms |  0.06 |    0.00 |          - |          - |        12 B |       0.000 |
| [Raffinert.FuzzySharp](https://github.com/Raffinert/FuzzySharp) |   4.970 ms |  0.3311 ms | 0.0181 ms |  0.02 |    0.00 |          - |          - |      3051 B |       0.000 |

Benchmark sources are in [Fuzzion.Benchmarks](Fuzzion.Benchmarks).

## Migration Notes

### From Raffinert.FuzzySharp v6

Fuzzion 0.1 has the same API as Raffinert.FuzzySharp v6.0.1. Change the package and namespaces:

- Package: `Raffinert.FuzzySharp` → `Fuzzion`
- Namespaces: `Raffinert.FuzzySharp` → `Fuzzion` (for example `Raffinert.FuzzySharp.PreProcess` → `Fuzzion.PreProcess`)

Some scores change, to match RapidFuzz (see [docs/known-differences.md](docs/known-differences.md)):

- `Ratio` and `TokenSortRatio` of two empty strings are 100 (were 0).
- `WeightedRatio` uses the partial token scores when one string is at least 1.5 times longer, so those scores are often higher.
- `Indel.NormalizedDistance` and `LongestCommonSubsequence.NormalizedSimilarity` had bugs that are fixed.

### From original FuzzySharp

The package name is `Fuzzion`, and the default namespace is `Fuzzion`. It keeps the familiar FuzzyWuzzy-style API while adding faster distance implementations, fixed partial-ratio behavior, cached scorers, and the fluent process pipeline. Also apply the v4-to-v5 and v5-to-v6 notes below.

### From Raffinert.FuzzySharp v4 to v5

- `PreprocessMode`-based APIs were replaced with `Func<string, string>` preprocessors.
- Use `StringPreprocessor.Full`, `StringPreprocessor.None`, or a custom delegate instead of `PreprocessMode.Full` or `PreprocessMode.None`.
- Generic extraction methods that use extractor delegates are now named `Extract*By`, such as `ExtractOneBy` and `ExtractTopBy`.

### From Raffinert.FuzzySharp v5 to v6

Similarity scoring now uses `double` throughout the library so fractional scores are preserved instead of being rounded to integers:

```csharp
double score = Fuzz.Ratio("new york mets", "new york mets!");

ExtractedResult<string> match = Process.ExtractOne(
    "goolge",
    new[] { "google", "bing", "facebook" });

double matchScore = match.Score;
```

The following APIs changed from `int` to `double`:

- `Fuzz` similarity methods
- `Scorer` and `CachedScorer` delegates
- `IRatioScorer`, `ICachedRatioScorer`, and their generic variants
- scoring strategies, including `ICachedStrategy`
- `ExtractedResult<T>.Score`
- extraction `cutoff` parameters across standard, cached, parallel, and pipeline APIs

Extraction cutoffs can therefore retain fractional thresholds:

```csharp
var matches = Process.ExtractAll(
    "goolge",
    new[] { "google", "bing", "facebook" },
    cutoff: 87.5);
```

Custom scorers and method-group targets must update their return types accordingly:

```csharp
public sealed class CustomScorer : IRatioScorer
{
    public double Score(string input1, string input2) => /* calculate score */ 0.0;

    public double Score(
        string input1,
        string input2,
        Func<string, string> preprocessor) =>
        Score(preprocessor(input1), preprocessor(input2));
}
```

The optional `scoreCutoff` argument was removed from Indel, Levenshtein, and longest-common-subsequence distance and similarity APIs. Compute the result first and apply any threshold explicitly:

```csharp
// v5
int limitedDistance = Levenshtein.Distance("kitten", "sitting", scoreCutoff: 2);

// v6
int distance = Levenshtein.Distance("kitten", "sitting");
bool isWithinCutoff = distance <= 2;
```

This does not remove extraction filtering: `Process` and `ResultExtractor` still accept `cutoff`, now as a `double`. Raw edit distances and unnormalized similarity counts remain `int`; normalized distance and similarity values remain `double` in the range 0–1.

## Credits

- [Adam Cohen (seatgeek/fuzzywuzzy)](https://chairnerd.seatgeek.com/fuzzywuzzy-fuzzy-string-matching-in-python/)
- [Antti Haapala (python-Levenshtein)](https://github.com/ztane)
- [David Necas (python-Levenshtein)](http://publications.physics.muni.cz/author/yeti)
- [Jacob Bayer (original FuzzySharp library)](https://github.com/JakeBayer/FuzzySharp)
- [Yevhen Cherkes (Raffinert.FuzzySharp, the code base Fuzzion starts from)](https://github.com/Raffinert/FuzzySharp)
- [Max Bachmann (RapidFuzz)](https://github.com/rapidfuzz/RapidFuzz)
- [Mikko Ohtamaa (python-Levenshtein)](https://github.com/miohtama/python-Levenshtein)
- [Panayiotis (Java implementation)](https://github.com/xdrop)

## Support

Fuzzion builds on the work of Yevhen Cherkes. You can support the upstream author through [GitHub Sponsors](https://github.com/sponsors/ycherkes).

See [CHANGELOG.md](CHANGELOG.md) for release history and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for licenses of the code Fuzzion is based on.
