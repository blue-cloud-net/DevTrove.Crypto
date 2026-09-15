# Changelog

All notable changes to `DevTrove.Crypto` will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> Chinese version: [CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)

## [Unreleased]

### Changed (in progress: Phase A of parent roadmap)

- **Renamed**: project namespaces / directory names / assembly names from `Crypto.Utils.*` → `DevTrove.Crypto.*`
- **Removed**: `Crypto.Utils.Api`, `Crypto.Utils.Host`, `Crypto.Utils.UI`, related NuGet packages, and the old HTTP API documentation (`docs/api-reference.md`, `docs/v0.2/*`, `docs/v1.x/*`)
- **Added**: `DevTrove.Crypto` metapackage (no source; only `ProjectReference` → Core)
- **Added**: `DevTrove.Crypto.slnx` (replaces `crypto-utils.slnx`)
- **Changed**: `Directory.Build.props` — root namespace, NuGet metadata (`RepositoryUrl` → `https://github.com/blue-cloud-net/DevTrove.Crypto`)
- **Changed**: `Directory.Packages.props` — dropped Web packages (`Swashbuckle`, `Microsoft.AspNetCore.OpenApi`, `SpaProxy`)
- **Added**: `global.json` (SDK 10.0.112, `rollForward: latestMajor`)
- **Added**: bilingual `README.md` / `README.zh-CN.md`
- **Added**: `AGENTS.md` for AI / human contributor workflow
- **Documentation**: `docs/architecture.md` rewritten for library layout; `docs/roadmap.md` and `docs/development-guide.md` updated
- **Note**: 5-TFM target (`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`) temporarily uses 3 TFM (`net8.0;net9.0;net10.0`) until the `netstandard2.0` polyfill is finalized

### Known limitations

- L1: Certificate chain **verification** is a simplified implementation (DN comparison + per-level signature check + trusted-root match); **not PKIX full path validation** — scheduled for v1.1
- L2: Certificate chain **building** may not terminate on mutually-signed constructed inputs — scheduled for v1.1
- L3: OCSP **parsing only** (no request construction, no signature verification) — v2 evaluation

[Unreleased]: https://github.com/blue-cloud-net/DevTrove.Crypto/compare/main...HEAD
