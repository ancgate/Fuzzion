# Fuzzion Implementation Plan

Fuzzion is a C# fuzzy string matching library. It starts from
[Raffinert.FuzzySharp](https://github.com/Raffinert/FuzzySharp) v6.0.1 (commit
`c5d20d3`). The goal is to cover everything in
[RapidFuzz](https://github.com/rapidfuzz/RapidFuzz), plus the set-based metrics from
[kdjones/fuzzystring](https://github.com/kdjones/fuzzystring), plus
[Raffinert/FuzzySharp#31](https://github.com/Raffinert/FuzzySharp/pull/31).

This plan lists every work item with its files, the public API, where to port it
from, the unit tests to write, and when it counts as done. Expected test values marked
**(oracle)** were computed with Python `rapidfuzz==3.14.6`, `thefuzz==0.22.1` and the
standard-library `difflib`. They are exact; copy them into tests as they are.

## Progress

| Phase | Status |
|---|---|
| 0 - Rebrand | Done |
| 1 - Oracle and PR #31 | Done. The oracle found two inherited bugs (fixed) and two inherited differences, now changed to match RapidFuzz (D8, D9) |
| 2 - Distance metrics | Done (correct and oracle-checked; see the performance follow-ups in Phase 8) |
| 3 - Fuzz scorer parity | Done |
| 4 - Set-based metrics | Done |
| 5 - Process | Done (see the implementation note under #26) |
| 6 - Edit operations | Done |
| 7-9 | Not started |

---

## Contents

1. [Ground rules](#1-ground-rules)
2. [Reference sources and licenses](#2-reference-sources-and-licenses)
3. [Milestones](#3-milestones)
4. [Shared test plan (applies to every item)](#4-shared-test-plan)
5. [Phase 0 - Rebrand to Fuzzion](#phase-0---rebrand-to-fuzzion)
6. [Phase 1 - Test oracle and PR #31](#phase-1---test-oracle-and-pr-31)
7. [Phase 2 - Distance metrics](#phase-2---distance-metrics)
8. [Phase 3 - Fuzz scorer parity](#phase-3---fuzz-scorer-parity)
9. [Phase 4 - Set-based metrics](#phase-4---set-based-metrics)
10. [Phase 5 - Process (search over lists)](#phase-5---process)
11. [Phase 6 - Edit operations](#phase-6---edit-operations)
12. [Phase 7 - Unicode and text cleanup](#phase-7---unicode-and-text-cleanup)
13. [Phase 8 - Performance](#phase-8---performance)
14. [Phase 9 - Release](#phase-9---release)
15. [Open decisions](#open-decisions)
16. [Master checklist](#master-checklist)

---

## 1. Ground rules

These follow the conventions already in the codebase. Keep to them so new code looks
like the old code.

| Topic | Rule |
|---|---|
| Root namespace | `Fuzzion` (distance classes live directly in it, like `Levenshtein` today) |
| Distance class shape | `public sealed partial class X` split into `X.Static.cs` (static API) and `X.Instance.cs` (cached instance built from one source string, `IDisposable`), plus a generic `XT<T>` instance class for `IEquatable<T>` sequences, matching `Indel` / `IndelT<T>` |
| The four functions | `Distance`, `Similarity`, `NormalizedDistance`, `NormalizedSimilarity`. Normalized values are `double` in `[0, 1]`. `NormalizedSimilarity + NormalizedDistance == 1`. `Similarity + Distance == Maximum(len1, len2)` |
| Input overloads | `(string, string)` and `<T>(ReadOnlySpan<T>, ReadOnlySpan<T>) where T : IEquatable<T>` |
| `Fuzz` scores | `double` from 0 to 100 (v6 convention) |
| Target frameworks | Library must still build for `netstandard2.0; netstandard2.1; netcoreapp3.1; net45; net46; net462; net472; net48; net6.0; net8.0; net9.0; net10.0`. Anything that needs `Rune`, `Vector128`, or `System.Runtime.Intrinsics` goes behind `#if NETCOREAPP3_0_OR_GREATER` / `#if NET7_0_OR_GREATER` with a scalar fallback |
| Dependencies | No new runtime dependencies. Test-only and benchmark-only packages are fine |
| Memory | Use the existing pooled buffers (`ArrayPool`, `DictionarySlimPooled`, `PatternMatchVector`) instead of allocating per call |
| Tests | xUnit (`xunit` 2.9.x), in `Fuzzion.Tests`. Test project targets `net462; net472; net8.0; net10.0` |
| Benchmarks | BenchmarkDotNet, in `Fuzzion.Benchmarks` |
| Docs | Every public member gets XML docs. Every feature gets a README section and a CHANGELOG line |

---

## 2. Reference sources and licenses

| Source | License | Use it for | How |
|---|---|---|---|
| Raffinert.FuzzySharp | MIT | The base code | Already in this repo. Keep the original copyright notice |
| [rapidfuzz-cpp](https://github.com/rapidfuzz/rapidfuzz-cpp) `rapidfuzz/distance/*_impl.hpp`, `rapidfuzz/fuzz_impl.hpp`, `rapidfuzz/details/simd_*.hpp` | MIT | Fast algorithms to port | Port, keep its notice in `THIRD-PARTY-NOTICES.md` |
| [RapidFuzz](https://github.com/rapidfuzz/RapidFuzz) `src/rapidfuzz/*_py.py`, `distance/*_py.py` | MIT | Readable pure-Python versions of the same algorithms: the easiest place to understand behavior | Read and port, keep notice |
| RapidFuzz `tests/` | MIT | Ready-made test cases | Translate into xUnit |
| [TheFuzz](https://github.com/seatgeek/thefuzz) `thefuzz/process.py` | MIT | `dedupe` behavior | Port, keep notice |
| Python `difflib` | PSF | Ratcliff-Obershelp reference behavior | **Use only as a test oracle.** Implement from the published algorithm |
| kdjones/fuzzystring | EPL-1.0 | List of set-based metrics only | **Do not copy code.** It is EPL and has bugs (see Phase 4) |
| Raffinert/FuzzySharp#31 | MIT (same author as the base) | Approximate substring ratio | Merge the branch so authorship stays in history |

---

## 3. Milestones

| Version | Contents | Items |
|---|---|---|
| `0.1.0-preview.1` | Rebrand, test oracle, PR #31 | P0.1-P0.9, #35, #36 |
| `0.2.0` | All RapidFuzz distance metrics | #1-#7, #29 |
| `0.3.0` | `Fuzz` scorer parity with RapidFuzz | #17-#23 |
| `0.4.0` | Set-based metrics | #8-#16 |
| `0.5.0` | Process: `Cdist`, `Cpdist`, distance scorers, `Dedupe`; edit operations | #24-#28, #30 |
| `0.6.0` | Unicode and accent handling | #31, #32 |
| `1.0.0` | SIMD, full benchmark suite, API review and freeze | #33, #34 |

Dependencies between items:

```
P0 (rebrand) ──► #35 (oracle) ──► everything else
#2 Jaro ──► #3 Jaro-Winkler
#8 q-grams ──► #9 #10 #11 #12
#26 scorer metadata ──► #24 Cdist, #25 Cpdist (distance scorers)
#28 EditOps type ──► #29 Indel editops
#20 alignment ◄──► #36 ApproximateSubstring (share the window search)
#1-#7 ──► #33 SIMD (port scalar first, then vectorize)
```

---

## 4. Shared test plan

Every distance metric (#1-#7, #9-#14) gets **all** of these tests in addition to its
own cases. Put shared helpers in `Fuzzion.Tests/Support/`.

| # | Test | What it checks |
|---|---|---|
| T1 | `EmptyBoth` | `("", "")` gives distance 0 and normalized similarity 1 |
| T2 | `EmptyOne` | `("abc", "")` gives distance = maximum, normalized similarity 0 |
| T3 | `Identical` | `(s, s)` gives distance 0 for 20 random strings |
| T4 | `Symmetric` | `d(a, b) == d(b, a)` for every metric where that holds (all here except weighted Levenshtein with unequal insert/delete costs) |
| T5 | `NormalizedRange` | Normalized values are always in `[0, 1]` |
| T6 | `Complement` | `NormalizedSimilarity + NormalizedDistance == 1` (tolerance `1e-12`) and `Similarity + Distance == Maximum` |
| T7 | `StaticEqualsInstance` | The cached instance (`new X(a).DistanceFrom(b)`) returns the same value as the static call |
| T8 | `StringEqualsGeneric` | The `string` overload equals the generic overload on `int[]` code units |
| T9 | `BlockBoundaries` | Bit-parallel code changes paths at 64 characters, so test lengths **0, 1, 63, 64, 65, 127, 128, 129, 300, 1000** with a fixed seed (`new Random(1234)`) against the naive reference |
| T10 | `NaiveReference` | 2,000 random pairs (alphabet of 4 letters, so there are many matches) compared to a plain textbook implementation in `Support/Naive.cs`. This is the main correctness check |
| T11 | `Oracle` | Every row in the oracle fixture file for that metric (see #35) |
| T12 | `ThreadSafety` | `Parallel.For` over 10,000 calls of the static API gives the same results as a sequential run |
| T13 | `DisposeTwice` | Disposing an instance twice does not throw |

For `Fuzz` scorers (#17-#23, #36) use T1-T3, T5 (range 0-100), T7, T11 and T12.

**Naive references** to write in `Support/Naive.cs` (test code only, clarity over
speed): `Levenshtein`, `Indel`, `Lcs`, `Hamming`, `Jaro`, `JaroWinkler`, `Osa`,
`DamerauLevenshtein` (full Lowrance-Wagner table), `Prefix`, `Postfix`,
`LongestCommonSubstring`, `RatcliffObershelp`.

---

## Phase 0 - Rebrand to Fuzzion

Goal: same code and behavior, new name, correct credits. No feature changes in this
phase so any test failure points to the rename.

### P0.1 Rename folders, projects and solution

```bash
git mv FuzzySharp Fuzzion
git mv Fuzzion/FuzzySharp.csproj Fuzzion/Fuzzion.csproj
git mv FuzzySharp.Test Fuzzion.Tests
git mv Fuzzion.Tests/FuzzySharp.Test.csproj Fuzzion.Tests/Fuzzion.Tests.csproj
git mv FuzzySharp.Benchmarks Fuzzion.Benchmarks
git mv Fuzzion.Benchmarks/FuzzySharp.Benchmarks.csproj Fuzzion.Benchmarks/Fuzzion.Benchmarks.csproj
git mv FuzzySharp.slnx Fuzzion.slnx
```

Then update the three `<Project Path=...>` lines in `Fuzzion.slnx` and the two
`<ProjectReference>` paths in the test and benchmark projects.

### P0.2 Rename namespaces

**Rule: replace `Raffinert.FuzzySharp` with `Fuzzion`. Do not touch a plain
`FuzzySharp`.** The tests and benchmarks deliberately use the old `FuzzySharp` 2.0.2
NuGet package as a comparison baseline (`global::FuzzySharp...` and
`using FuzzLevenshteinClassic = FuzzySharp.Levenshtein;`). A blanket replace would
point those comparisons at Fuzzion itself.

```bash
grep -rl 'Raffinert\.FuzzySharp' --include='*.cs' --include='*.csproj' --include='*.slnx' --include='*.md' . \
  | xargs sed -i 's/Raffinert\.FuzzySharp/Fuzzion/g'
```

There is one more pattern that must change. Fourteen files in the library use the
partially qualified delegate name `FuzzySharp.Scorer` (for example
`TokenSortScorer.cs`: `protected override FuzzySharp.Scorer Scorer => ...`). Inside
the old `Raffinert.FuzzySharp.*` namespaces that resolved to Raffinert's delegate.
After the rename it would not resolve. Fix it **in the library folder only**:

```bash
grep -rl 'FuzzySharp\.Scorer' Fuzzion --include='*.cs' | xargs sed -i 's/\bFuzzySharp\.Scorer\b/Fuzzion.Scorer/g'
```

### P0.3 Update project properties

In `Fuzzion/Fuzzion.csproj`:

| Property | New value |
|---|---|
| `Version`, `PackageVersion` | `0.1.0-preview.1` |
| `AssemblyVersion`, `FileVersion` | `0.1.0.0` |
| `Authors` | `Jefferson Bien-Aime` |
| `Description` | `Fast fuzzy string matching for .NET: FuzzyWuzzy-style scorers, RapidFuzz-compatible distance metrics, and set-based similarity. Based on Raffinert.FuzzySharp.` |
| `PackageProjectUrl`, `RepositoryUrl` | `https://github.com/ancgate/Fuzzion` |
| `PackageTags` | `fuzzy string matching levenshtein jaro-winkler damerau jaccard rapidfuzz fuzzywuzzy similarity` |
| `AssemblyName` | `$(MSBuildProjectName)` (remove the `Raffinert.` prefix) |
| `RootNamespace` | `$(MSBuildProjectName.Replace(" ", "_"))` |
| `InternalsVisibleTo` | `Fuzzion.Tests` |

Remove the duplicated `FileVersion`, `Version` and `PackageVersion` lines (each
appears twice today). Apply the same `AssemblyName` and `RootNamespace` change to the
test and benchmark projects.

### P0.4 License and credits

- `LICENSE`: keep the MIT text. Replace the empty `Copyright (c) 2018` line with:
  ```
  Copyright (c) 2018 Jacob Bayer (FuzzySharp)
  Copyright (c) 2024-2026 Yevhen Cherkes (Raffinert.FuzzySharp)
  Copyright (c) 2026 Jefferson Bien-Aime (Fuzzion)
  ```
- New `THIRD-PARTY-NOTICES.md` with the full MIT notice of each source you port from:
  JakeBayer/FuzzySharp, Raffinert.FuzzySharp, rapidfuzz-cpp (Max Bachmann, Adam Cohen),
  RapidFuzz (Max Bachmann), TheFuzz (SeatGeek). Add entries as you port; it's fine to
  add them all now.
- Add `<None Include="../THIRD-PARTY-NOTICES.md" Pack="true" PackagePath="." />` so
  the notices ship inside the NuGet package.

### P0.5 README

Rewrite the title, badges, install command (`dotnet add package Fuzzion`) and
namespaces. Add near the top:
"Fuzzion is based on [Raffinert.FuzzySharp](https://github.com/Raffinert/FuzzySharp)
by Yevhen Cherkes, itself based on [FuzzySharp](https://github.com/JakeBayer/FuzzySharp)
by Jacob Bayer." Add a "Roadmap" link to this file.

### P0.6 Repository housekeeping

- `.github/FUNDING.yml`: delete (it points to Raffinert's sponsors) or replace with your own.
- `.github/workflows/build-nuget-packages.yml`: change the four `FuzzySharp` paths to
  `Fuzzion.slnx`, `Fuzzion.Tests/Fuzzion.Tests.csproj`, `Fuzzion/Fuzzion.csproj`.
- `.ai/context.md`: update the names, and fix the line that says the tests use NUnit
  (they use xUnit).
- `CHANGELOG.md`: add a `## Fuzzion 0.1.0-preview.1` section above the existing
  history and keep the old entries under a heading
  `## History inherited from Raffinert.FuzzySharp`.
- Add a git remote for upstream so you can pull future fixes:
  `git remote add upstream https://github.com/Raffinert/FuzzySharp.git`.

### P0.7 Unit tests for the rebrand

File: `Fuzzion.Tests/RebrandTests.cs`

| Test | Assertion |
|---|---|
| `AssemblyNameIsFuzzion` | `typeof(Fuzzion.Fuzz).Assembly.GetName().Name == "Fuzzion"` |
| `NoPublicTypeInOldNamespace` | No exported type of the Fuzzion assembly has a namespace starting with `"Raffinert"` |
| `PublicSurfaceUnchanged` | The number of exported types equals the count taken before the rename (record it in the test) |
| `BaselineScoresUnchanged` | `Fuzz.Ratio("this is a test", "this is a test!")` is `96.551724` within `1e-6` **(oracle)**; `Fuzz.TokenSortRatio("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear")` is `100` |

### P0.8 Done when

- `dotnet build Fuzzion.slnx -c Release` succeeds for all target frameworks.
- **All existing tests pass unchanged** (apart from the namespace edits) on net8.0 and net10.0.
- `dotnet pack` produces `Fuzzion.0.1.0-preview.1.nupkg` containing `README.md`, `LICENSE` and `THIRD-PARTY-NOTICES.md`.
- `grep -r "Raffinert" --include=*.cs .` returns nothing.

### P0.9 Commit as one commit

`Rebrand Raffinert.FuzzySharp to Fuzzion`. Keeping it as a single, behavior-free
commit makes later upstream cherry-picks easier to resolve.

---

## Phase 1 - Test oracle and PR #31

### #35 Test oracle from Python RapidFuzz

**Why first:** every later item is checked against it.

**Files**

```
tools/oracle/requirements.txt   rapidfuzz==3.14.6, thefuzz==0.22.1, numpy
tools/oracle/generate.py        writes the fixture files
tools/oracle/README.md          how to regenerate
Fuzzion.Tests/Oracle/data/*.json one file per metric, e.g. jaro.json
Fuzzion.Tests/Oracle/OracleData.cs   loads a file and returns xUnit MemberData
Fuzzion.Tests/Oracle/OracleTests.cs  one [Theory] per metric
```

**Generator rules**

- Fixed seed (`random.seed(20261104)`) so files are reproducible.
- For each metric write about 2,000 pairs: 500 short (0-10 characters), 500 around the
  64-character boundary (60-70), 500 long (100-1,100), 300 with a small alphabet
  (`"ab"`), and 200 hand-picked edge cases (empty, identical, one character).
  Include non-ASCII strings (`é`, `ß`, Cyrillic, CJK) but **no characters outside the
  Basic Multilingual Plane** for now (see #31).
- Always call RapidFuzz with `processor=None`. Fuzzion's `Process` cleans up text by
  default and RapidFuzz does not, so tests must pass `StringPreprocessor.None` on the
  C# side.
- Record format: `{"a": "...", "b": "...", "args": {...}, "distance": 3, "similarity": 4, "norm_distance": 0.43, "norm_similarity": 0.57}`.
- Gzip any file larger than 1 MB and load it with `GZipStream`.

**Tolerances**

| Kind of value | Tolerance |
|---|---|
| Integer distances and similarities | Exact |
| Normalized values | `1e-9` |
| `Fuzz` scores (0-100) | `1e-6` |

**Unit tests**

| Test | Assertion |
|---|---|
| `OracleFilesExist` | Each expected file is present and not empty |
| `OracleFilesParse` | Every row parses and has the fields its metric needs |
| `Oracle_<Metric>` | One `[Theory]` per metric. Start with the metrics that already exist (`Levenshtein`, `Indel`, `LCSseq`, `Ratio`, `PartialRatio`, `TokenSortRatio`, `TokenSetRatio`, `WRatio`) so differences in today's code show up immediately |

**Done when** the existing metrics pass their oracle tests, or any differences are
written down in `docs/known-differences.md` with the reason. `WRatio` and
`PartialRatio` are the most likely to differ.

### #36 Approximate substring ratio (Raffinert/FuzzySharp#31)

**What:** `Fuzz.ApproximateSubstringRatio` finds the best approximate occurrence of the
shorter string inside the longer one using bit-parallel Indel distance. It also adds
`Indel.BestSubstringMatch`, which returns the best distance and where that match ends.
Its own benchmarks show it **3.5-9.8x faster than `PartialRatio`**, and over 100x
faster than before on exact matches thanks to an `IndexOf` fast path.

**State of the PR:** draft, opened 2026-07-25 by the Raffinert maintainer, 4 commits,
15 files, about 1,700 lines added. It was written **before v6**, so it returns
`int` scores. A test merge onto the current code conflicts only in `CHANGELOG.md`, but
the types need updating.

**Steps**

1. Merge the PR branch so the author's commits stay in history:
   ```bash
   git fetch upstream pull/31/head:pr31
   git merge --no-ff pr31   # resolve CHANGELOG.md by hand
   ```
2. Apply the rename rule from P0.2 to the new files.
3. **Convert to `double` scores (v6 convention):**
   - `Fuzz.ApproximateSubstringRatio` returns `double`.
   - `ApproximateSubstringScore.FromDistance` returns
     `100.0 * Math.Max(0, 1.0 - distance / (double)patternLength)` with no rounding.
   - `ApproximateSubstringRatioScorer` and `CachedApproximateSubstringRatioScorer`
     return `double`.
4. **Fix the problems the PR's own review notes (`pr_review.md`) found:**
   - The new public `BestSubstringMatch(ReadOnlySpan<char>, ReadOnlySpan<char>)` overload
     changes which method existing callers get. Make it `internal`, or keep it public
     with full XML docs and a CHANGELOG note (Fuzzion is pre-1.0, so either is acceptable).
   - Remove the duplicate `IndexOf` check in `ApproximateSubstringRatioStrategy.cs`; the
     check inside `BestSubstringMatch` is enough.
   - Add an early return in `CachedApproximateSubstringRatioStrategy` when both inputs
     are equal.
5. Delete `pr_review.md` from the repository (move anything useful into the PR
   description or this plan).
6. Optional: add `StartIndex` to `IndelSubstringMatch` by running the same search
   backwards from `FirstBestEndIndex`. This also supplies most of #20.

**Unit tests**

- Keep the PR's `ApproximateSubstringRatioTests.cs` (250 lines) and
  `IndelBestSubstringTests.cs` (306 lines); update assertions from `int` to `double`.
- Add:

| Test | Input | Expected |
|---|---|---|
| `ExactSubstringIs100` | `"invoice number 12345"`, `"processed invoice number 12345 successfully"` | `100` |
| `TwoEmptyIs100` | `""`, `""` | `100` |
| `OneEmptyIs0` | `"abc"`, `""` | `0` |
| `ReturnsDouble` | a pair with one substitution in a 7-letter pattern | not a whole number |
| `SymmetricForEqualLength` | `(a, b)` and `(b, a)` of equal length | equal |
| `CachedEqualsUncached` | 1,000 random pairs | equal |
| `NotEqualToPartialRatio` | a pair where they differ | documents that the two metrics are different on purpose |
| `CharOverloadEqualsGeneric` | random pairs | `BestSubstringMatch` for `char` equals `BestSubstringMatch<T>` with `T = char` |
| `EndIndexIsFirstStrictImprovement` | the PR README example | `FirstBestEndIndex` points at the final `'5'` |

**Benchmarks:** keep `ApproximateSubstringRatioBenchmarks.cs` from the PR.

---

## Phase 2 - Distance metrics

Port from `rapidfuzz-cpp/rapidfuzz/distance/<Name>_impl.hpp`. If the C++ is hard to
follow, read `RapidFuzz/src/rapidfuzz/distance/<Name>_py.py` first.

### #1 Hamming

**Files:** `Fuzzion/Hamming.Static.cs`, `Fuzzion/Hamming.Instance.cs`,
`Fuzzion.Tests/Distance/HammingTests.cs`

**API**

```csharp
public static int Distance(string s1, string s2, bool pad = true);
public static int Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, bool pad = true) where T : IEquatable<T>;
// plus Similarity, NormalizedDistance, NormalizedSimilarity with the same parameters
```

With `pad = true` the length difference counts as edits. With `pad = false` different
lengths throw `ArgumentException`.

**Port from:** `Hamming_impl.hpp` (62 lines). **Size:** S

**Unit tests** (oracle values)

| Input | Distance | Normalized similarity |
|---|---|---|
| `"karolin"`, `"kathrin"` | 3 | 0.571429 |
| `"1011101"`, `"1001001"` | 2 | 0.714286 |
| `"abc"`, `"abcd"` (pad) | 1 | 0.75 |
| `"abc"`, `"abc"` | 0 | 1.0 |
| `""`, `""` | 0 | 1.0 |
| `"abc"`, `"abcd"`, `pad: false` | throws `ArgumentException` | |

Plus T1-T13.

### #2 Jaro

**Files:** `Fuzzion/Jaro.Static.cs`, `Fuzzion/Jaro.Instance.cs`, `Fuzzion.Tests/Distance/JaroTests.cs`

**API:** same four functions, but **all return `double`** (Jaro has no integer
distance). `Distance = 1 - Similarity`.

**Port from:** `Jaro_impl.hpp` (863 lines; bit-parallel, with separate paths for
patterns of 64 characters or fewer and for longer ones). **Size:** M

**Unit tests** (oracle values)

| Input | Similarity |
|---|---|
| `"MARTHA"`, `"MARHTA"` | 0.944444 |
| `"DWAYNE"`, `"DUANE"` | 0.822222 |
| `"DIXON"`, `"DICKSONX"` | 0.766667 |
| `"CRATE"`, `"TRACE"` | 0.733333 |
| `"abc"`, `"xyz"` | 0.0 |
| `""`, `""` | 1.0 |
| `"a"`, `""` | 0.0 |

Plus T1-T13. T9 matters most here because the long-pattern path is separate code.

### #3 Jaro-Winkler

**Files:** `Fuzzion/JaroWinkler.Static.cs`, `Fuzzion/JaroWinkler.Instance.cs`, `Fuzzion.Tests/Distance/JaroWinklerTests.cs`

**API:** like Jaro, with `double prefixWeight = 0.1`. The common prefix counts up to 4
characters, and the bonus applies only when the Jaro similarity is above 0.7. Throw
`ArgumentOutOfRangeException` if `prefixWeight` is outside `[0, 1]`, the same range
RapidFuzz accepts (the result is capped at 1).

**Port from:** `JaroWinkler_impl.hpp` (92 lines, on top of Jaro). **Size:** S

**Unit tests** (oracle values)

| Input | Default (0.1) | `prefixWeight: 0.2` |
|---|---|---|
| `"MARTHA"`, `"MARHTA"` | 0.961111 | 0.977778 |
| `"DWAYNE"`, `"DUANE"` | 0.84 | 0.857778 |
| `"DIXON"`, `"DICKSONX"` | 0.813333 | 0.86 |
| `"CRATE"`, `"TRACE"` (no common prefix) | 0.733333 (same as Jaro) | 0.733333 |

Plus `PrefixWeightOutOfRangeThrows` (`-0.1`, `1.5`, `NaN`), `NeverBelowJaro` (random pairs), and T1-T13.

### #4 OSA (Optimal String Alignment)

**Files:** `Fuzzion/Osa.Static.cs`, `Fuzzion/Osa.Instance.cs`, `Fuzzion.Tests/Distance/MetricTests.cs`

The class is named `Osa`, not `OSA`: .NET naming guidelines capitalize only two-letter acronyms.

Counts swapping two adjacent characters as one edit, but each part of the string can be
edited only once.

**Port from:** `OSA_impl.hpp` (276 lines; Hyyrö 2003 bit-parallel). **Size:** M

**Unit tests** (oracle values)

| Input | OSA | Levenshtein (for contrast) | OSA normalized similarity |
|---|---|---|---|
| `"ab"`, `"ba"` | 1 | 2 | 0.5 |
| `"CA"`, `"ABC"` | **3** | 3 | 0.0 |
| `"abcdef"`, `"abcfde"` | 2 | 2 | 0.666667 |
| `"kitten"`, `"sitting"` | 3 | 3 | 0.571429 |

The `"CA"` / `"ABC"` row is the textbook case where OSA and full Damerau-Levenshtein
disagree; test it explicitly as `OsaDiffersFromDamerauOnCaAbc`. Plus T1-T13.

### #5 Damerau-Levenshtein (unrestricted)

**Files:** `Fuzzion/DamerauLevenshtein.Static.cs`, `Fuzzion/DamerauLevenshtein.Instance.cs`, `Fuzzion.Tests/Distance/DamerauLevenshteinTests.cs`

**Port from:** `DamerauLevenshtein_impl.hpp` (140 lines; Zhao et al. algorithm, uses a
hash map of last-seen positions). **Size:** M

**Unit tests** (oracle values)

| Input | Distance | Normalized similarity |
|---|---|---|
| `"CA"`, `"ABC"` | **2** | 0.333333 |
| `"ab"`, `"ba"` | 1 | 0.5 |
| `"abcdef"`, `"abcfde"` | 2 | 0.666667 |
| `"kitten"`, `"sitting"` | 3 | 0.571429 |

Plus `NeverAboveOsa` and `NeverAboveLevenshtein` on random pairs, a non-ASCII test
(the last-seen map must handle any `char`), and T1-T13.

### #6 Prefix and Postfix

**Files:** `Fuzzion/Prefix.Static.cs`, `Fuzzion/Postfix.Static.cs` (an instance class
isn't needed; add one only for API consistency), `Fuzzion.Tests/Distance/PrefixPostfixTests.cs`

`Similarity` is the length of the shared start (or end). `Distance = max(len1, len2) - Similarity`.

**Port from:** `Prefix_impl.hpp`, `Postfix_impl.hpp` (31 lines each). **Size:** S

**Unit tests** (oracle values)

| Input | Prefix sim | Prefix dist | Postfix sim | Postfix dist |
|---|---|---|---|---|
| `"prefix"`, `"preface"` | 4 | 3 | 0 | 7 |
| `"testing"`, `"resting"` | 0 | 7 | 6 | 1 |
| `"abc"`, `"xyz"` | 0 | 3 | 0 | 3 |
| `""`, `""` | 0 | 0 | 0 | 0 |

Plus T1-T13.

### #7 `Indel.Similarity`

**Files:** `Fuzzion/Indel.Static.cs`, `Fuzzion.Tests/IndelTests.cs`

Add `Similarity(string, string)` and the generic overload; `Similarity = len1 + len2 - Distance`.
**Size:** S

**Unit tests** (oracle values): `("kitten", "sitting")` gives distance 5 and
similarity 8; `("abc", "abc")` gives distance 0 and similarity 6. Plus T6.

### Phase 2 benchmarks (part of #34)

One BenchmarkDotNet class per metric with lengths 8, 64, 256 and 1,024. Compare
Levenshtein with Quickenshtein and Fastenshtein, which are already referenced.

---

## Phase 3 - Fuzz scorer parity

### #17 QRatio

Same as `Ratio`, but returns **0 when either input is empty** (`Ratio("", "")` is 100).

**Files:** `Fuzzion/Fuzz.cs`, `SimilarityRatio/Scorer/StrategySensitive/Simple/QRatioScorer.cs`,
`.../CachedQRatioScorer.cs`, `Fuzzion.Tests/FuzzyTests/QRatioTests.cs`. **Size:** S

**Unit tests** (oracle values)

| Input | QRatio | Ratio |
|---|---|---|
| `"this is a test"`, `"this is a test!"` | 96.551724 | 96.551724 |
| `""`, `"abc"` | 0 | 0 |
| `""`, `""` | **0** | **100** |

### #18 TokenRatio

The higher of `TokenSortRatio` and `TokenSetRatio`, computed while sharing the
tokenization work. **Port from:** `fuzz_impl.hpp`, `token_ratio`. **Size:** S

**Unit tests** (oracle values)

| Input | TokenRatio |
|---|---|
| `"fuzzy wuzzy was a bear"`, `"wuzzy fuzzy was a bear"` | 100 |
| `"new york mets"`, `"new york mets vs atlanta braves"` | 100 |
| `"this is a test"`, `"this is a test!"` | 96.551724 |

Plus `EqualsMaxOfSortAndSet` on 1,000 random sentence pairs.

### #19 PartialTokenRatio

The higher of `PartialTokenSortRatio` and `PartialTokenSetRatio`, sharing work.
**Size:** S

**Unit tests** (oracle values): `("this is a test", "this is a test!")` gives 100;
`("new york mets", "new york mets vs atlanta braves")` gives 100; `("", "abc")` gives 0.
Plus `EqualsMaxOfPartialSortAndSet` on random pairs.

### #20 PartialRatioAlignment

Returns where the best partial match is, not just its score.

```csharp
public readonly record struct ScoreAlignment(double Score, int SourceStart, int SourceEnd, int DestStart, int DestEnd);
public static ScoreAlignment PartialRatioAlignment(string s1, string s2, Func<string, string> preprocessor = null);
```

**Files:** `Fuzzion/ScoreAlignment.cs`, `Fuzzion/Fuzz.cs`, the partial-ratio strategy
(expose the window it already finds). **Size:** M

**Unit tests** (oracle values)

| Input | Expected |
|---|---|
| `"this is a test"`, `"yes this is a test!"` | `(100, 0, 14, 4, 18)` |
| `"new york mets"`, `"the new york mets vs atlanta braves"` | `(100, 0, 13, 4, 17)` |

Plus `ScoreEqualsPartialRatio` (random pairs) and `SubstringAtAlignmentGivesScore`
(recompute `Ratio` on the returned slices and get the same score).

### #21 Score cutoff on scorers

**Decision first:** v6 deliberately removed `scoreCutoff` from the distance APIs
(see CHANGELOG v6.0.0). The plan is to add it back **as an optional last parameter
that defaults to 0**, so no existing call changes. A score below the cutoff returns 0.
Implementations should use the cutoff to stop early (for example, skip `TokenSet` work
in `WRatio` when the length ratio already rules out the cutoff).

**Implementation note (found while doing Phase 3):** adding an *optional* parameter
to the existing `Fuzz` methods would break method-group conversions such as
`Func<string, string, double> f = Fuzz.Ratio;`, which callers (and Fuzzion's own tests)
use. So add `scoreCutoff` as **separate overloads** —
`Ratio(string, string, double scoreCutoff)` and
`Ratio(string, string, Func<string, string> preprocessor, double scoreCutoff)` — and
use it for early exits where it pays off (for example the length bound
`200 * min(len) / (len1 + len2)` for `Ratio`). `Process.Extract*` already accepts
`cutoff`, so this matters mainly for direct `Fuzz` calls.

**Unit tests** (oracle values)

| Test | Expected |
|---|---|
| `Fuzz.Ratio("kitten", "sitting")` | 61.538462 |
| `Fuzz.Ratio("kitten", "sitting", scoreCutoff: 80)` | 0 |
| `CutoffZeroIsUnchanged` | every scorer with cutoff 0 equals the no-cutoff call (random pairs) |
| `AtOrAboveCutoffIsUnchanged` | score at or above the cutoff is unchanged |

**Size:** M

### #22 `ReadOnlySpan<char>` overloads

Add `Fuzz.X(ReadOnlySpan<char>, ReadOnlySpan<char>)` for `Ratio`, `QRatio`,
`PartialRatio` and `ApproximateSubstringRatio`. Token-based scorers need strings for
splitting; add span versions for them only if benchmarks show a gain.

**Unit tests:** `SpanEqualsString` for each overload on random pairs, and
`SliceOfLargerBuffer` (pass a slice of a bigger array and get the same result as the
substring). **Size:** M

### #23 Missing cached scorers

Add `CachedPartialRatioScorer`, `CachedPartialTokenSortScorer`, `CachedQRatioScorer`,
`CachedTokenRatioScorer` and `CachedPartialTokenRatioScorer`, and register them in
`ScorerCache`.

**Unit tests:** `CachedEqualsUncached_<Scorer>` on 1,000 random pairs each, and a
`Process.Configure().Cached(scorer)` test for each that returns the same results as the
uncached search. Extend the existing `CachedScorerTests.cs`. **Size:** M

---

## Phase 4 - Set-based metrics

These come from kdjones/fuzzystring, but **write them from the definitions; don't copy
that code.** It is EPL-licensed, and its versions are wrong. It uses LINQ
`Intersect`/`Union`, which count **distinct characters**, then divides by the raw
string lengths. So `Dice("aaaa", "aaaa")` gives 0.25 instead of 1.0, and
`Tanimoto("aaaa", "aaaa")` gives 0.142857 instead of 1.0. Its Hamming check in
`ApproximatelyEquals` uses integer division, so it is almost always 0.

Put these in `Fuzzion/SetSimilarity/` with namespace `Fuzzion`.

### #8 Q-gram helper

```csharp
public enum QGramMode { Set, Multiset }
public sealed record QGramOptions(int Q = 2, bool Pad = false, QGramMode Mode = QGramMode.Set);
public static class QGrams
{
    public static IReadOnlyList<string> Of(string s, int q = 2, bool pad = false);
    // internal: count q-grams into a pooled dictionary without allocating substrings
}
```

When `Pad` is true, add `q - 1` copies of `'\u0002'` before and `'\u0003'` after the
string, so short strings still produce q-grams and the first and last characters count
equally. **Size:** S

**Unit tests**

| Test | Expected |
|---|---|
| `Of("night", 2)` | `["ni", "ig", "gh", "ht"]` |
| `Of("aaaa", 2)` | `["aa", "aa", "aa"]` |
| `Of("a", 2)` | empty |
| `Of("a", 2, pad: true)` | 2 grams |
| `Of("", 2)` | empty |
| `QZeroThrows` | `ArgumentOutOfRangeException` |

### #9 Jaccard, #10 Sørensen-Dice, #11 Overlap coefficient, #12 Tanimoto

```csharp
public static class Jaccard     { double Similarity(string a, string b, QGramOptions o = null); double Distance(...); }
public static class SorensenDice{ ... }
public static class Overlap     { ... }
public static class Tanimoto    { ... }
```

| Metric | Formula (A, B = q-gram sets or counts) |
|---|---|
| Jaccard | \|A ∩ B\| / \|A ∪ B\| |
| Sørensen-Dice | 2\|A ∩ B\| / (\|A\| + \|B\|) |
| Overlap | \|A ∩ B\| / min(\|A\|, \|B\|) |
| Tanimoto | A·B / (\|A\|² + \|B\|² − A·B) on q-gram **count** vectors |

On plain sets Tanimoto equals Jaccard exactly. Defining it on counts is what makes it
a separate metric; say so in the XML docs.

Both empty gives 1.0. One empty gives 0.0. Two strings that are both too short to have
any q-gram (for example `"a"` and `"b"` with bigrams) are compared for plain equality,
so `"a"` vs `"b"` is 0, not 1. Each metric gets an `IEnumerable<T>`
overload so callers can compare word sets as well as q-grams. **Size:** S each.

**Unit tests** (bigrams, set mode; values from the Python reference in
`tools/oracle/generate.py`)

| Input | Jaccard | Dice | Overlap | Tanimoto (counts) |
|---|---|---|---|---|
| `"night"`, `"nacht"` | 0.142857 | 0.25 | 0.25 | 0.142857 |
| `"context"`, `"contact"` | 0.333333 | 0.5 | 0.5 | 0.333333 |
| `"aaaa"`, `"aaaa"` | **1.0** | **1.0** | **1.0** | **1.0** |
| `"abc"`, `"xyz"` | 0.0 | 0.0 | 0.0 | 0.0 |

The `"aaaa"` row is a regression test for the kdjones bug; name it
`RepeatedCharactersScoreOne`. Also add `DiceFromJaccard` (Dice = 2J / (1 + J) in set
mode, on random pairs), `MultisetDiffersFromSet` (`"aaab"` vs `"aab"`), and
`WordSetOverload` (`Jaccard.Similarity(new[] {"a","b"}, new[] {"b","c"})` gives
0.333333).

### #13 Ratcliff-Obershelp

Python's `difflib.SequenceMatcher(None, a, b).ratio()`: find the longest common block,
repeat on the left and right remainders, and score 2·matches / total length.

```csharp
public static class RatcliffObershelp
{
    public static double Similarity(string a, string b, bool autoJunk = false);
    public static IReadOnlyList<MatchingBlock> MatchingBlocks(string a, string b, bool autoJunk = false);
}
```

`autoJunk = true` copies difflib's rule: when the second string has 200+ characters,
characters that make up more than 1% of it are ignored. Default to `false` so results
are predictable. **Size:** M

**Unit tests** (oracle: `difflib` with `autojunk=False`)

| Input | Similarity |
|---|---|
| `"WIKIMEDIA"`, `"WIKIMANIA"` | 0.777778 |
| `"GESTALT PATTERN MATCHING"`, `"GESTALT PRACTICE"` | 0.6 |
| `"abcd"`, `"bcde"` | 0.75 |
| `""`, `""` | 1.0 |

Plus `AutoJunkMatchesDifflib` (strings of 250+ characters with `autoJunk: true`
against difflib's default), and oracle rows for 2,000 random pairs.

### #14 Longest common substring

```csharp
public readonly record struct CommonSubstring(int SourceStart, int TargetStart, int Length);
public static class LongestCommonSubstring
{
    public static CommonSubstring Find(string a, string b);
    public static string Of(string a, string b);
    public static int Similarity(string a, string b);          // = Length
    public static double NormalizedSimilarity(string a, string b); // Length / max(len)
}
```

Use a dynamic-programming table that keeps only one row, so memory is O(min(n, m)).
On ties, return the earliest position in `a`, as difflib does. **Size:** S

**Unit tests** (oracle: `difflib.find_longest_match`, `autojunk=False`)

| Input | Expected `(SourceStart, TargetStart, Length)` | `Of` |
|---|---|---|
| `"xabcdey"`, `"zzabcdq"` | `(1, 2, 4)` | `"abcd"` |
| `"GeeksforGeeks"`, `"GeeksQuiz"` | `(0, 0, 5)` | `"Geeks"` |
| `"abc"`, `"xyz"` | `(0, 0, 0)` | `""` |

Plus `DiffersFromSubsequence` (`"abcde"` vs `"aXcXe"`: substring length 1, subsequence
length 3) and T1-T13.

### #15 LCS returned as a string

`LongestCommonSubsequence.Of(string a, string b)`, built from the existing
`MatchingBlocks`. **Size:** S

**Unit tests:** `("abcde", "ace")` gives `"ace"`; `("abc", "xyz")` gives `""`;
`ResultIsSubsequenceOfBoth` and `LengthEqualsSimilarity` on random pairs.

### #16 `ApproximatelyEquals` (optional)

```csharp
public enum Tolerance { Strong, Normal, Weak }
public enum Metric { Levenshtein, DamerauLevenshtein, JaroWinkler, Jaccard, SorensenDice, RatcliffObershelp, LongestCommonSubstring /* ... */ }
public static bool ApproximatelyEquals(this string a, string b, Tolerance t, params Metric[] metrics);
public static bool ApproximatelyEquals(this string a, string b, double threshold, params Metric[] metrics);
```

Average the normalized similarities of the chosen metrics and compare with the
threshold: Strong ≥ 0.90, Normal ≥ 0.75, Weak ≥ 0.60. Ignore case unless a
`caseSensitive: true` argument is passed. **Size:** S

**Unit tests:** `IdenticalIsTrueForEveryTolerance`, `("MARTHA", "MARHTA", Strong, JaroWinkler)`
is true, `("abc", "xyz", Weak, all metrics)` is false, `NoMetricsThrows`,
`CaseInsensitiveByDefault`, `CustomThresholdBoundary` (exactly at the threshold is true).

---

## Phase 5 - Process

### #26 Scorers where lower is better (do this before #24)

Today `IRatioScorer` assumes a higher score is better, from 0 to 100, so you can't
search with `Levenshtein.Distance`. Add:

```csharp
public interface IScorerInfo
{
    double OptimalScore { get; }   // 100 for ratios, 0 for distances
    double WorstScore   { get; }   // 0 for ratios, +∞ (or max length) for distances
}
public static class Scorers
{
    public static IRatioScorer FromDistance(Func<string, string, int> distance); // e.g. Levenshtein.Distance
    public static IRatioScorer FromSimilarity(Func<string, string, double> similarity, double optimal, double worst);
}
```

The extractors use `OptimalScore > WorstScore` to decide the sort direction, and treat
`cutoff` as "maximum allowed distance" for distance scorers. Existing scorers default
to `(100, 0)`, so nothing changes for them. **Size:** M

**Implementation note (as built):** instead of `IScorerInfo`, distance scorers implement
`IDistanceScorer : IRatioScorer` with an `int? MaxDistance`. The maximum lives on the
scorer because the searches' `cutoff` parameter defaults to 0, which cannot mean both
"no limit" and "exact matches only" for a distance. Passing a non-zero `cutoff` with a
distance scorer throws. The searches run on the negated distance and flip the scores
back at each public entry point (`Extractor/DistanceScoring.cs`), so the four extractor
variants did not need to change. `Levenshtein.Distance` has optional cost parameters, so
pass a lambda: `Scorers.FromDistance((a, b) => Levenshtein.Distance(a, b))`.

**Unit tests**

| Test | Expected (oracle) |
|---|---|
| `ExtractOne("appel", ["apple","appel","banana","bandana"], Scorers.FromDistance(Levenshtein.Distance))` | `("appel", 0, index 1)` |
| `DistanceCutoffIsMaximum` | cutoff 2 keeps only results with distance ≤ 2 |
| `ExtractSortedAscendingForDistance` | sorted from smallest distance |
| `ExistingScorersUnchanged` | all existing `ProcessTests` still pass |

### #24 `Cdist`: every query against every choice

```csharp
public static double[,] Cdist(
    IReadOnlyList<string> queries, IReadOnlyList<string> choices,
    IRatioScorer scorer = null, Func<string, string> processor = null,
    double scoreCutoff = 0, int workers = 1);
```

- Build one cached scorer per query (reuses the pattern bit vectors), then score every
  choice.
- `workers = -1` uses all cores (`Parallel.For` over query rows); `1` runs sequentially.
- Values that fail the cutoff are stored as `WorstScore`.
- Default `processor` is `null` (no cleanup), matching RapidFuzz's `cdist`.
- Add a `float[,]` overload later if memory becomes an issue (rows × columns × 8 bytes).

**Port from:** RapidFuzz `process_py.py` (behavior), `process_cpp_impl.pyx` (how the
work is split). **Size:** L

**Unit tests** (oracle values; queries `["apple", "banana"]`, choices
`["apple", "appel", "banana", "bandana"]`)

| Scorer | Expected matrix |
|---|---|
| `Ratio` | `[[100, 80, 18.181818, 16.666667], [18.181818, 18.181818, 100, 92.307692]]` |
| `Levenshtein.Distance` | `[[0, 2, 5, 6], [5, 5, 0, 1]]` |

Plus `ParallelEqualsSequential` (200 × 500 random strings, `workers: -1` vs `1`),
`EmptyQueriesGivesEmptyMatrix`, `CutoffFillsWorstScore`, `CellEqualsSingleCall`
(random cells equal `scorer.Score(q, c)`), and a cancellation test if you add a
`CancellationToken` parameter.

### #25 `Cpdist`: pairs only

`double[] Cpdist(IReadOnlyList<string> a, IReadOnlyList<string> b, ...)` scores `a[i]`
against `b[i]`. Lists of different lengths throw `ArgumentException`. **Size:** M

**Unit tests** (oracle): `Cpdist(["apple", "banana"], ["appel", "bandana"], Ratio)`
gives `[80, 92.307692]`. Plus `LengthMismatchThrows` and `ParallelEqualsSequential`.

### #27 `Dedupe`

Port TheFuzz's `process.dedupe`: for each item, find everything scoring above the
threshold (default 70, scorer `TokenSetRatio`), keep the longest item in each group
(ties broken alphabetically), and return the distinct results. If nothing was
removed, return the original list.

```csharp
public static IReadOnlyList<string> Dedupe(IEnumerable<string> items, double threshold = 70, IRatioScorer scorer = null);
```

**Size:** S

**Unit tests**

| Input | Expected (oracle) |
|---|---|
| `["Frodo Baggin", "Frodo Baggins", "F. Baggins", "Samwise G.", "Gandalf", "Bilbo Baggins"]` | `["Frodo Baggins", "Gandalf", "Samwise G."]` (compare as sets) |

Plus `NoDuplicatesReturnsInput`, `EmptyReturnsEmpty`, `ThresholdHundredOnlyRemovesExact`,
and 50 oracle cases generated with `thefuzz.process.dedupe`.

---

## Phase 6 - Edit operations

### #28 `EditOps` and `OpCodes` collection types

```csharp
public sealed class EditOps : IReadOnlyList<EditOp>
{
    public int SourceLength { get; } public int DestLength { get; }
    public string Apply(string source, string dest);
    public EditOps Inverse();
    public EditOps RemoveSubsequence(EditOps subsequence);
    public OpCodes AsOpCodes();
    public List<MatchingBlock> AsMatchingBlocks();
    public static EditOps FromOpCodes(OpCodes opcodes);
}
public sealed class OpCodes : IReadOnlyList<OpCode> { /* the same members, mirrored */ }
```

Keep the current `EditOp[]`-returning methods for now and mark them `[Obsolete]`
pointing to the new types; remove them before 1.0. **Size:** M

**Implementation note (as built):** rather than keeping parallel `EditOp[]` methods,
`GetEditOps` now returns `EditOps`, which has `Length` and converts implicitly to
`EditOp[]`, so existing callers keep compiling. The old `AsOpCodes(this IEnumerable<EditOp>, ...)`
extension is unchanged; note that it ignores `REPLACE` operations (it was written for
Indel/LCS edits), so use `EditOps.AsOpCodes()` for Levenshtein edits.

**Unit tests** (oracle: `Levenshtein.editops("spam", "park")`)

| Test | Expected |
|---|---|
| `Levenshtein.GetEditOps("spam", "park")` | `delete(0,0)`, `insert(3,2)`, `replace(3,3)` |
| `.Inverse()` | `insert(0,0)`, `delete(2,3)`, `replace(3,3)` |
| `.Apply("spam", "park")` | `"park"` |
| `Levenshtein.GetOpCodes("spam", "park")` | `delete(0,1,0,0)`, `equal(1,3,0,2)`, `insert(3,3,2,3)`, `replace(3,4,3,4)` |
| `ApplyAlwaysGivesDest` | random pairs: `Apply(a, b) == b` |
| `InverseTwiceIsOriginal` | random pairs |
| `OpCodesRoundTrip` | `FromOpCodes(x.AsOpCodes())` equals `x` |

### #29 Indel edit operations

`Indel.GetEditOps` and `Indel.GetOpCodes`, implemented with the existing LCS code
(Indel edits are the LCS edits). **Size:** S

**Unit tests** (oracle): `Indel.GetEditOps("spam", "park")` gives `delete(0,0)`,
`insert(3,2)`, `insert(3,3)`, `delete(3,4)`. Plus `ContainsNoReplace` and
`CountEqualsDistance` on random pairs.

### #30 Levenshtein `scoreCutoff` and `scoreHint`

The `scoreHint` parameter exists but is documented as ignored. Implement both:

- `scoreCutoff`: if the distance would be above the cutoff, stop and return
  `scoreCutoff + 1` (RapidFuzz behavior). Use a banded search (Ukkonen) that only
  checks cells within the cutoff of the diagonal.
- `scoreHint`: start with a band of that size and double it until the result is
  within the band.

**Port from:** `Levenshtein_impl.hpp` (`levenshtein_hyrroe2003_small_band`,
`levenshtein_hyrroe2003_block` with `max`, and the doubling loop in
`levenshtein_distance`). **Size:** M

**Unit tests** (oracle)

| Test | Expected |
|---|---|
| `Distance("kitten", "sitting", scoreCutoff: 2)` | 3 (cutoff + 1) |
| `Distance("kitten", "sitting", scoreCutoff: 3)` | 3 |
| `HintDoesNotChangeResult` | random pairs, hints 0, 1, 5, 1,000 |
| `CutoffResultIsMinOfDistanceAndCutoffPlusOne` | random pairs, lengths up to 1,000 |
| `CutoffFasterOnFarStrings` | benchmark, not a unit test |

---

## Phase 7 - Unicode and text cleanup

### #31 Whole-character (code point) handling

.NET strings are UTF-16, so emoji and some rare CJK characters take two `char`s and
are compared as two halves. Python works on whole characters, so scores differ:

| Input | Python RapidFuzz | Fuzzion today |
|---|---|---|
| `Fuzz.Ratio("😀abc", "😀abd")` | **75** (oracle; 4 characters each) | **80** (5 UTF-16 units each) |

Add:

- `StringPreprocessor.FullUnicode`: like `Full`, but checks whole characters for
  letters and digits (`Rune` on .NET Core 3.0+, `char.ConvertToUtf32` plus
  `CharUnicodeInfo` on older frameworks).
- A code-point mode for scorers: convert both strings to `int[]` and use the existing
  generic `T` paths. Expose it as `Fuzz.Ratio(a, b, TextUnit.CodePoint)` or a
  `FuzzOptions` object (decide in API review).

**Size:** M

**Unit tests**

| Test | Expected |
|---|---|
| `EmojiCodePointMatchesPython` | `Ratio("😀abc", "😀abd", CodePoint)` is 75 |
| `EmojiDefaultModeUnchanged` | default mode still 80 (no silent behavior change) |
| `FullUnicodeKeepsSurrogatePairs` | `FullUnicode("𠀀abc")` keeps `𠀀` (U+20000, a CJK letter stored as two `char`s); `Full` turns its two halves into spaces |
| `BmpTextSameInBothModes` | random BMP strings give equal scores in both modes |
| `Oracle_CodePoint` | extend #35 with non-BMP strings for code-point mode only |

### #32 Accent removal

`StringPreprocessor.RemoveDiacritics`: normalize to Unicode NFD, drop characters of
category `NonSpacingMark`, normalize back to NFC. Letters that don't decompose need a
small map, in both cases: `ß→ss`, `æ→ae` / `Æ→Ae`, `œ→oe` / `Œ→Oe`, `ø→o` / `Ø→O`, `ł→l` / `Ł→L`, `đ→d` / `Đ→D`, `þ→th` / `Þ→Th`. Also add
`StringPreprocessor.Compose(params Func<string, string>[] steps)` so callers can chain
steps, for example `Compose(RemoveDiacritics, Full)`. **Size:** S

**Unit tests**

| Test | Expected |
|---|---|
| `Fuzz.Ratio("café", "cafe")` raw | 75 (oracle) |
| `Fuzz.Ratio("café", "cafe", RemoveDiacritics)` | 100 |
| `RemoveDiacritics("Ærøskøbing")` | `"Aeroskobing"` |
| `RemoveDiacritics("straße")` | `"strasse"` |
| `PrecomposedEqualsDecomposed` | `"é"` (U+00E9) and `"é"` give the same output |
| `NonLatinUnchanged` | Cyrillic, CJK and Arabic letters without marks pass through unchanged |
| `ComposeAppliesInOrder` | `Compose(RemoveDiacritics, Full)("Crème BRÛLÉE!")` is `"creme brulee"` |

---

## Phase 8 - Performance

### #33 SIMD batch scoring

RapidFuzz compares one query with 8 to 64 short choices at once by packing them into
the lanes of a 128- or 256-bit register (`details/simd_sse2.hpp`, `simd_avx2.hpp`).
Fuzzion has no SIMD; its only hardware-assisted operation is `PopCount`.

- Port the batched bit-parallel Indel/LCS and Levenshtein for choices of at most 8,
  16, 32 or 64 characters using `Vector128<T>` / `Vector256<T>`
  (`#if NET7_0_OR_GREATER`), with the current scalar code as fallback.
- Use it inside `Cdist` (#24) and the `Extract*` methods when the query is short.
- Check `Vector256.IsHardwareAccelerated` at runtime.

**Size:** L

**Unit tests**

| Test | Assertion |
|---|---|
| `SimdEqualsScalar_<Metric>` | 10,000 random short pairs: SIMD result equals scalar result exactly |
| `LaneBoundaries` | batches of 1, 7, 8, 9, 31, 32, 33 choices (partly filled registers) |
| `MixedLengthsFallBack` | choices longer than the lane width are scored by the scalar path |
| `ScalarForced` | an internal switch forces the scalar path, so tests cover both on any machine |

### Performance follow-ups from Phase 2

Phase 2 put correctness first. Rough BenchmarkDotNet short-run results on similar
random strings (time relative to `Levenshtein.Distance` on the same input):

| Metric | 8 chars | 64 | 256 | 1,024 |
|---|---|---|---|---|
| `Hamming` | 0.12x | 0.08x | 0.03x | 0.01x |
| `Jaro` / `JaroWinkler` | 1.1x | 3.9x | 5.1x | 7.0x |
| `Osa` | 1.0x | 1.0x | **38x** | **67x** |
| `DamerauLevenshtein` | 1.2x | **45x** | **146x** | **184x** |

| # | Follow-up | Port from | Size |
|---|---|---|---|
| P8.1 | Multi-block bit-parallel OSA for inputs over 64 characters (replaces the table fallback) | `OSA_impl.hpp`, `osa_hyrroe2003_block` | M |
| P8.2 | Bit-parallel Jaro (single and multi-block) | `Jaro_impl.hpp` | L |
| P8.3 | Damerau-Levenshtein: replace the `Dictionary` of last rows with an array for ASCII/Latin-1 plus a growing hash map, and use 16-bit rows when lengths allow | `DamerauLevenshtein_impl.hpp`, `GrowingHashmap.hpp` | S |
| P8.4 | Real caching in the `Jaro`, `JaroWinkler` and `DamerauLevenshtein` instance classes (today they store the source and call the static method) | `CachedJaro`, `CachedJaroWinkler` in rapidfuzz-cpp | M |

Each follow-up must keep every oracle and contract test passing.

### #34 Benchmarks

One benchmark class for every new metric and API (#1-#33), comparing Fuzzion with
FuzzySharp 2.0.2 and, for Levenshtein, Quickenshtein and Fastenshtein. Record results
in `docs/benchmarks.md` for each release. Optional: a CI job that runs a short
benchmark (`--job short`) and fails on a regression larger than 20%. **Size:** S per class

---

## Phase 9 - Release

| Step | Details |
|---|---|
| Versioning | Semantic versioning. Breaking changes are allowed in 0.x but always go in the CHANGELOG |
| CI | Rename paths in `build-nuget-packages.yml` (P0.6). Add a `publish` job that runs on tags `v*` and pushes to NuGet using a `NUGET_API_KEY` repository secret |
| NuGet ID | Publish `0.1.0-preview.1` early to claim `Fuzzion`. Then request the `Fuzzion.*` ID prefix reservation from NuGet for your account |
| Docs | README sections per feature, XML docs, optional DocFX site on GitHub Pages |
| API review before 1.0 | Check naming consistency, remove `[Obsolete]` members, decide #31's API shape, freeze |
| Upstream sync | Every few weeks, `git fetch upstream` and cherry-pick Raffinert fixes; record each in the CHANGELOG |

---

## Open decisions

| # | Decision | Recommendation |
|---|---|---|
| D1 | Should new distance classes live in the root namespace or in `Fuzzion.Distance`? | Root namespace, like the existing `Levenshtein`, `Indel` and `LongestCommonSubsequence` |
| D2 | Add `scoreCutoff` back (removed in v6)? | Yes, as an optional last parameter defaulting to 0 (#21, #30) |
| D3 | PR #31's public `char` overload: public or internal? | Internal until there's a need for it |
| D4 | Default text cleanup in `Process` | Keep Fuzzion's current default (`Full`) and document that RapidFuzz's default is none |
| D5 | Code-point mode API (#31) | An options object (`FuzzOptions`), so later options don't need more overloads |
| D6 | `Cdist` return type | `double[,]` first; add `float[,]` if memory use is a problem |
| D7 | Tanimoto definition | On q-gram counts, so it differs from Jaccard |
| D8 | Score of two empty strings in `Ratio` / `TokenSortRatio` | **Decided:** 100, as RapidFuzz (was 0) |
| D9 | `WeightedRatio` partial branch | **Decided:** partial token scores, as RapidFuzz |

---

## Master checklist

### Phase 0 - Rebrand
- [x] P0.1 Rename folders, projects, solution
- [x] P0.2 Rename namespaces (`Raffinert.FuzzySharp` → `Fuzzion`, and the 14 `FuzzySharp.Scorer` references)
- [x] P0.3 Update project properties
- [x] P0.4 LICENSE and THIRD-PARTY-NOTICES.md
- [x] P0.5 README
- [x] P0.6 FUNDING.yml, workflow paths, `.ai/context.md`, CHANGELOG, upstream remote
- [x] P0.7 Rebrand unit tests
- [x] P0.8 Build, all tests pass, package builds
- [x] P0.9 Single rebrand commit

### Phase 1 - Oracle and PR #31
- [x] #35 Python oracle generator, fixtures, loader, oracle tests for existing metrics
- [x] #36 Merge PR #31, convert to `double`, apply review fixes, tests

### Phase 2 - Distance metrics
- [x] #1 Hamming
- [x] #2 Jaro
- [x] #3 Jaro-Winkler
- [x] #4 OSA
- [x] #5 Damerau-Levenshtein
- [x] #6 Prefix and Postfix
- [x] #7 `Indel.Similarity`

### Phase 3 - Fuzz scorers
- [x] #17 QRatio
- [x] #18 TokenRatio
- [x] #19 PartialTokenRatio
- [x] #20 PartialRatioAlignment
- [x] #21 Score cutoff on scorers
- [x] #22 `ReadOnlySpan<char>` overloads
- [x] #23 Missing cached scorers

### Phase 4 - Set-based metrics
- [x] #8 Q-gram helper
- [x] #9 Jaccard
- [x] #10 Sørensen-Dice
- [x] #11 Overlap coefficient
- [x] #12 Tanimoto
- [x] #13 Ratcliff-Obershelp
- [x] #14 Longest common substring
- [x] #15 LCS as a string
- [x] #16 `ApproximatelyEquals` (optional)

### Phase 5 - Process
- [x] #26 Scorers where lower is better
- [x] #24 `Cdist`
- [x] #25 `Cpdist`
- [x] #27 `Dedupe`

### Phase 6 - Edit operations
- [x] #28 `EditOps` / `OpCodes` types
- [x] #29 Indel edit operations
- [x] #30 Levenshtein `scoreCutoff` / `scoreHint`

### Phase 7 - Unicode
- [ ] #31 Code-point mode and `FullUnicode`
- [ ] #32 `RemoveDiacritics` and `Compose`

### Phase 8 - Performance
- [ ] #33 SIMD batch scoring
- [ ] #34 Benchmarks for everything (Phase 2 metrics done: `DistanceMetricsBenchmarks`)
- [ ] P8.1-P8.4 Phase 2 performance follow-ups

### Definition of done (every item)
- [ ] Public API has XML docs
- [ ] Unit tests from this plan, plus the shared tests T1-T13 where they apply
- [ ] Oracle fixtures added and passing
- [ ] Builds for every target framework, including `netstandard2.0` and `net45`
- [ ] Benchmark class added
- [ ] README section and CHANGELOG line
- [ ] No new runtime dependencies
