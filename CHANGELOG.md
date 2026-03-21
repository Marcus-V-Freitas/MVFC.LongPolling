# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.9] - 2026-03-21

### Fixed

- Resolved a race condition in `WaitUntilReadyAsync` within `LongPollingService` that was causing integration test flakiness.

---

## [1.0.8] - 2026-03-21


### Changed
- CI/CD workflow refinements for automated publishing and coverage reporting
- Minor adjustments to Codecov configuration for status checks precision

## [1.0.7] - 2026-03-15

### Added

- Targeted tests reaching 100% line and branch coverage.
- Coordinated concurrency and reflection-based race condition tests.

### Fixed

- Fixed `SemaphoreFullException` during concurrent lock cancellation by separating lock release from reference decrement.
- Improved `ChannelLockEntry` reference counting safety to prevent memory leaks on failed acquisitions.

---

## [1.0.6] - 2026-03-13

### Fixed

- Fix CI/CD build scripts for improved coverage reporting automation.

---

## [1.0.5] - 2026-03-13

### Added

- Expanded test suite to achieve 100% code coverage.
- New test scenarios for Redis exception handling.

### Changed

- Refined CI/CD build scripts for improved coverage reporting automation.

---

## [1.0.4] — 2026-02-22

### Changed

- Applied C# / .NET 2026 best practices (`.editorconfig`, analyzers, naming conventions).

---

## [1.0.3] — 2026-02-22

### Changed

- Internal improvements to lock handling for safer concurrent scenarios.
- Reduced package icon size.

### Removed

- Unnecessary code cleanup.

---

## [1.0.2] — 2026-02-22

### Added

- New test scenarios illustrating more complex, real-world usage patterns.

---

## [1.0.1] — 2026-02-22

### Added

- CI/CD pipeline configured for automated build and NuGet publishing.

---

## [1.0.0] — 2026-02-22

### Added

- Initial commit of the `MVFC.LongPolling` project.

---

[1.0.9]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.8...v1.0.9
[1.0.8]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.7...v1.0.8
[1.0.7]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.6...v1.0.7
[1.0.6]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.5...v1.0.6
[1.0.5]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Marcus-V-Freitas/MVFC.LongPolling/releases/tag/v1.0.0
