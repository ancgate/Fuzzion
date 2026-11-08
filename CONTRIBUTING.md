# Contributing to Fuzzion

## Build and test

```bash
dotnet build Fuzzion.slnx -c Release
dotnet test Fuzzion.Tests/Fuzzion.Tests.csproj -c Release -f net10.0
```

The library targets .NET Framework 4.5 through .NET 10. The .NET Framework test targets
run only on Windows (the CI does this).

## What every change needs

- **Correctness against RapidFuzz.** New or changed metrics get reference values from
  Python in `tools/oracle/generate.py` and an `Oracle_*` test. A deliberate difference
  must be listed in `docs/known-differences.md` and in `IsKnownDifference`; any other
  difference is a bug. The CI regenerates the fixtures and fails if they differ from the
  committed ones, so always commit regenerated fixtures:

  ```bash
  python3 -m venv .venv
  .venv/bin/pip install -r tools/oracle/requirements.txt
  .venv/bin/python tools/oracle/generate.py
  ```

- **The shared contract tests.** A new distance metric gets a subclass of
  `DistanceMetricContract` (empty inputs, symmetry, ranges, static versus instance,
  64-character boundaries, a textbook reference in `Support/Naive.cs`, thread safety).
- **The public API baseline.** Public API changes fail `PublicApiTests`. If intended, run
  the tests once with `FUZZION_UPDATE_PUBLIC_API=1`, review the diff of
  `Fuzzion.Tests/PublicApi/Fuzzion.api.txt`, and describe the change in `CHANGELOG.md`.
- **XML docs** on every new public member, a README section for new features, and a
  CHANGELOG line.
- **No new runtime dependencies.**
- **Performance claims** come with a BenchmarkDotNet class in `Fuzzion.Benchmarks` and
  numbers in `docs/benchmarks.md`. To check a change for slowdowns, run the *Benchmark
  regression check* workflow, which measures the base and your branch on the same machine.

## Keeping up with Raffinert.FuzzySharp

Fuzzion started from Raffinert.FuzzySharp v6.0.1. To bring over later upstream fixes:

```bash
git remote add upstream https://github.com/Raffinert/FuzzySharp.git   # once
git fetch upstream
git log --oneline master..upstream/master                              # what is new
git cherry-pick <commit>                                               # one fix at a time
```

Upstream uses the namespace `Raffinert.FuzzySharp` and folders `FuzzySharp*`; rename them
to `Fuzzion` while resolving the cherry-pick. Re-run the oracle tests afterwards: two
upstream bugs found by the oracle (Indel normalized distance, culture-sensitive token
sorting) are fixed here and may still be present upstream.
