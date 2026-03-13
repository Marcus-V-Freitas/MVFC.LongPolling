# Contributing to MVFC.LongPolling

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) running locally
- Git

## Running locally

```sh
git clone https://github.com/Marcus-V-Freitas/MVFC.LongPolling.git
cd MVFC.LongPolling
dotnet restore MVFC.LongPolling.slnx
dotnet build MVFC.LongPolling.slnx --configuration Release
```

## Running tests

The tests require Docker to be running.

```sh
dotnet test tests/MVFC.LongPolling.Tests/MVFC.LongPolling.Tests.csproj --configuration Release
```

## Adding a new helper

1. Create a new folder under `src/MVFC.LongPolling.{ServiceName}/`
2. Follow the structure of an existing helper (e.g. `MVFC.LongPolling.Mysql`)
3. Add the new project to `MVFC.LongPolling.slnx`
4. Add the package version to `Directory.Packages.props`
5. Add integration tests in `tests/MVFC.LongPolling.Tests/`
6. Update `README.md` and `README.pt-BR.md` with the new package entry

## Branch naming

- `feat/` — new feature or helper
- `fix/` — bug fix
- `chore/` — dependency update or maintenance
- `docs/` — documentation only
- `test/` — tests only
- `refactor/` — no feature change, no bug fix

Example: `feat/add-long-polling-helper`

## Commit convention

This project follows [Conventional Commits](https://www.conventionalcommits.org/):

- `feat: add long polling helper`
- `fix: fix long polling connection timeout`
- `docs: update README badges`
- `chore: bump SqlKata to 2.5.0`
- `test: add long polling integration tests`
- `refactor: simplify long polling commander setup`

## Pull Request process

1. Fork and create your branch from `main`
2. Make your changes and ensure all tests pass locally
3. Open a PR against `main` and fill in the PR template
4. Wait for the CI to pass
5. A maintainer will review and merge
