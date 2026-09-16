# Changelog

All notable changes to `DevTrove.Crypto` will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> Chinese version: [CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)

> **The library is in `0.x`: a breaking change is allowed between minor versions and is listed under `Changed` below. Nothing here is a compatibility promise until `1.0.0`.**

## [Unreleased]

### Changed (version model)

- **Version line renumbered.** The abstraction layer is a work item (`RM-0.0.14`) rather than a release, so the first public release — `0.1.0` — delivers symmetric algorithms, the first version that is actually usable. Later milestones moved down one: asymmetric `0.2.0`, hashes / MACs / randomness `0.3.0`, key derivation `0.4.0`. `0.5.0` and later are unchanged
- **`<Version>` advanced to `0.1.0-dev`** in preparation for the first release
- **`0.x` API instability stated explicitly** in `README.md` / `README.zh-CN.md` (the readme shipped inside every package), `docs/nuget.md` and `docs/roadmap.md`

### Changed (library-only restructuring)

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
- **Note**: the target set is four TFMs (`netstandard2.0;net8.0;net9.0;net10.0`); `netstandard2.1` was dropped because no non-EOL host resolves that asset

### Known limitations

Capability gaps (chain verification, OCSP, PKCS#7, KDF, MAC, Ed25519 / X25519) and the limitations that will stay are listed in [docs/architecture.md §7](docs/architecture.md). `0.0.x` are work-item numbers, not releases; per-item status is in [docs/roadmap.md](docs/roadmap.md).

### Documentation

- Rewrote the whole document set for a **standalone library**: removed every reference to how or where the library is consumed, replaced the phase plan with a version line, and introduced per-item status tracking (`✅` / `🚧` / `🟡` / `⬜`)
- Added an explicit goal set (pure-managed, cross-platform, AOT-friendly, wide compatibility surface) to `docs/architecture.md §1`
- Corrected claims that did not match the code: OCSP parsing, PKCS#7/CMS, certificate-chain building and verification, and `SM4` CTR/GCM are **not** implemented; the `netstandard` targets never produced an assembly
- Documented the code-style config contradiction (`.editorconfig` vs `docs/standards.md §2.4`) as a tracked item

[Unreleased]: https://github.com/blue-cloud-net/DevTrove.Crypto/compare/main...HEAD
