# Releasing Fuzzion

## One-time setup (repository owner)

1. **Enable GitHub Actions.** Actions are off by default on forks: *Settings → Actions →
   General → Allow all actions*. Until this is done no CI runs, including the Windows job
   that runs the .NET Framework 4.6.2 / 4.7.2 tests.
2. **Create a NuGet API key** at <https://www.nuget.org/account/apikeys> with the *Push*
   scope, limited to the glob `Fuzzion*`.
3. **Add it as a secret:** *Settings → Environments → New environment* named `nuget`, then
   add the secret `NUGET_API_KEY`. Optionally add yourself as a required reviewer, so each
   publish waits for your approval.
4. **Reserve the ID prefix** (optional, after the first publish): request the `Fuzzion`
   prefix at <https://learn.microsoft.com/nuget/nuget-org/id-prefix-reservation>, so only
   your account can publish `Fuzzion.*` packages.

## Each release

1. Make sure `master` is green in CI (both operating systems, and the oracle job).
2. In `CHANGELOG.md`, give the top section its version and date.
3. Set `Version`, `PackageVersion`, `AssemblyVersion` and `FileVersion` in
   `Fuzzion/Fuzzion.csproj`. `AssemblyVersion` changes only with the major version
   (`1.0.0.0` for every 1.x), so updates do not need binding redirects.
4. Commit, then tag and push:

   ```bash
   git tag v1.0.0-rc.1
   git push origin v1.0.0-rc.1
   ```

5. The **CI** workflow runs the tests, checks the oracle fixtures, packs with the version
   taken from the tag, and the **publish** job pushes `Fuzzion` and its symbols package
   to nuget.org.
6. Create a GitHub release from the tag and paste the CHANGELOG section.

## Version rules

[Semantic Versioning](https://semver.org/): breaking changes to the public API need a new
major version once 1.0.0 is out. The public API is frozen by
`Fuzzion.Tests/PublicApi/Fuzzion.api.txt`, so any API change shows up as a failing test
first (see `PublicApiTests`).

## Before 1.0.0 (final)

`1.0.0-rc.1` is a release candidate. Ship `1.0.0` when:

- CI has passed on Windows, including the .NET Framework tests, which cannot run in a
  Linux-only environment.
- The release candidate has been used by at least one real project, or a few weeks
  have passed without API change requests.
