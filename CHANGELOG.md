# Changelog

All notable changes to `DevTrove.Crypto` will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> Chinese version: [CHANGELOG.zh-CN.md](CHANGELOG.zh-CN.md)

## [Unreleased]

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
- **Note**: 5-TFM target (`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`) temporarily uses 3 TFM (`net8.0;net9.0;net10.0`) until the `netstandard2.0` polyfill is finalized

### Known limitations

Capability gaps (chain verification, OCSP, PKCS#7, KDF, MAC, Ed25519 / X25519) and the limitations that will stay are listed in [docs/architecture.md §7](docs/architecture.md). `0.0.x` are work-item numbers, not releases; per-item status is in [docs/roadmap.md](docs/roadmap.md).

### Documentation

- Rewrote the whole document set for a **standalone library**: removed every reference to how or where the library is consumed, replaced the phase plan with a version line, and introduced per-item status tracking (`✅` / `🚧` / `🟡` / `⬜`)
- Added an explicit goal set (pure-managed, cross-platform, AOT-friendly, wide compatibility surface) to `docs/architecture.md §1`
- Corrected claims that did not match the code: OCSP parsing, PKCS#7/CMS, certificate-chain building and verification, and `SM4` CTR/GCM are **not** implemented; the `netstandard` targets never produced an assembly
- Documented the code-style config contradiction (`.editorconfig` vs `docs/standards.md §2.4`) as a tracked item

[Unreleased]: https://github.com/blue-cloud-net/DevTrove.Crypto/compare/main...HEAD
