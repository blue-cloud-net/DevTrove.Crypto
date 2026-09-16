# DevTrove.Crypto

A pure-managed cryptography and certificate library for .NET, built on BouncyCastle, with first-class support for Chinese national cryptography (ShangMi / 国密) and X.509 / OCSP.

> 中文文档：[README.zh-CN.md](README.zh-CN.md)

---

## Highlights

- **Four NuGet packages**: `DevTrove.Crypto.Abstractions` (contracts, zero dependencies), `DevTrove.Crypto.Core` (implementation), `DevTrove.Crypto` (metapackage — reference this one), `DevTrove.Crypto.Tls` (TLS probe engine — planned, `0.6.0`)
- **Pure managed, zero native dependencies** — runs on every platform BouncyCastle supports, including WebAssembly, trimmed and AOT-compiled builds
- **Cross-platform by design, AOT-friendly** on `net8.0` and later, with a `netstandard2.0` compatibility surface for older runtimes
- **Algorithms**: RSA / ECDSA / DSA, AES (CBC / GCM), SM2 / SM3 / SM4
- **X.509 / PKCS**: certificates, CSR, CRL, PFX / PKCS#12, OCSP (parse only)
- **Multi-targeting**: `netstandard2.0;net8.0;net9.0;net10.0` — every shipped target has a host that actually runs it in CI
- **Apache-2.0** licensed

---

## Installation

```bash
# 门面包（推荐）：仅传递依赖，自动引入 Core
dotnet add package DevTrove.Crypto

# 或显式引用实现包
dotnet add package DevTrove.Crypto.Core
```

Requires .NET 8.0 or later at runtime; for older runtimes (e.g. .NET Framework, Unity), the `netstandard2.0` build applies.

The `DevTrove.Crypto.Abstractions` package can be referenced on its own when you want the contract surface without the BouncyCastle-backed implementation — it depends on nothing.

> **Pre-release.** The library is in `0.x`, so the public API may change between minor versions. Pin the exact version and read [Status](#status) before depending on it.

---

## Quick start

```csharp
using DevTrove.Crypto;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.Crypto.Sm;

// 解析 PEM 证书
var cert = Certificate.FromPem(pemString);
Console.WriteLine($"Subject: {cert.Subject}");
Console.WriteLine($"NotAfter: {cert.NotAfter:yyyy-MM-dd}");
Console.WriteLine($"SHA-256: {cert.Sha256Thumbprint}");

// 生成 SM2 密钥对并自签名证书
using var sm2 = new SM2();
sm2.GenerateKeyPair();
```

More examples are provided in the test projects under `tests/DevTrove.Crypto.Core.Tests/`.

---

## Target frameworks

| Target | Purpose |
|---|---|
| `netstandard2.0` | .NET Framework 4.6.2+, Unity, etc. |
| `net8.0` / `net9.0` | Current LTS / STS |
| `net10.0` | Latest features |

`netstandard2.1` is deliberately not a target — no non-EOL host resolves that asset, so it could never be runtime-verified.

`Span<T>` on `netstandard2.0` comes from the `System.Memory` package; anything else missing is polyfilled in `Compat/` (see [`docs/development-guide.md`](docs/development-guide.md)).

---

## Repository layout

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto.Abstractions/  contracts (zero dependencies — work item `RM-0.0.14`)
│  ├─ DevTrove.Crypto/               metapackage (no source)
│  ├─ DevTrove.Crypto.Core/          implementation
│  └─ DevTrove.Crypto.Tls/           TLS probe engine (planned, `0.6.0`)
├─ tests/
│  ├─ DevTrove.Crypto.Abstractions.Tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   tongsuo CLI helpers
├─ scripts/                       fixture generation
└─ docs/                          development documentation (bilingual)
```

---

## Documentation

`docs/` ships each document in two languages: English default (`x.md`) + Chinese companion (`x.zh-CN.md`). Both have identical section / table / Mermaid structure.

| Document | Contents |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Internal layering, dependency direction, capability boundaries, known limitations |
| [docs/standards.md](docs/standards.md) | Coding standards, naming, Git workflow, review checklist |
| [docs/nuget.md](docs/nuget.md) | Package boundaries, versioning, release process |
| [docs/tls-scanner.md](docs/tls-scanner.md) | TLS probe engine design (scheduled for `0.6.0` / `0.7.0`) |
| [docs/roadmap.md](docs/roadmap.md) | Version line, per-item status and evidence, excluded items, risks |
| [docs/development-guide.md](docs/development-guide.md) | Build / test / pack commands, TFM / polyfill rules, CI |
| [docs/library-api.md](docs/library-api.md) | Public API index |

---

## Build / test

```bash
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  DevTrove.Crypto.slnx -c Release
```

External dependency for interop tests: **tongsuo**. A missing tool causes interop tests to **fail**, not skip (per [docs/standards.md](docs/standards.md)).

> Builds and packing are not trustworthy yet: the framework declarations disagree (`RM-0.0.1`) and the two `netstandard` targets have never produced an assembly (`RM-0.0.14a`). See [docs/roadmap.md](docs/roadmap.md).

---

## Status

**Early development — the public API is not stable yet.** The library is in `0.x` and no backward-compatibility promise applies until `1.0.0`:

- `0.0.1`–`0.0.14` are **work-item numbers only** — never packaged, tagged or published.
- The first public release is `0.1.0`, and it is the first version that is actually usable (symmetric algorithms). The abstraction layer (`RM-0.0.14`) ships with it rather than as a release of its own.
- **Breaking changes are allowed between minor versions** (`0.1.0` → `0.2.0`), and are highlighted in [CHANGELOG.md](CHANGELOG.md). They never land in a patch release.
- **Pin the exact version** while the library is `0.x`; do not rely on a floating range.
- `1.0.0` freezes the public type surface (`RM-1.0.0-01`).

Current status of every item is tracked in [docs/roadmap.md](docs/roadmap.md).

---

## License

Apache-2.0 — see [LICENSE](LICENSE).
