# DevTrove.Crypto

A pure-managed cryptography and certificate library for .NET, built on BouncyCastle, with first-class support for Chinese national cryptography (ShangMi / 国密) and X.509 / OCSP.

> 中文文档：[README.zh-CN.md](README.zh-CN.md)

---

## Highlights

- **Three NuGet packages**: `DevTrove.Crypto` (metapackage), `DevTrove.Crypto.Core` (implementation), `DevTrove.Crypto.Tls` (TLS probe engine, reserved for Phase 0)
- **Pure managed, zero native dependencies** — runs on every platform BouncyCastle supports, including WebAssembly
- **Algorithms**: RSA / ECDSA / DSA, AES (CBC / GCM), SM2 / SM3 / SM4
- **X.509 / PKCS**: certificates, CSR, CRL, PFX / PKCS#12, OCSP (parse only)
- **Multi-targeting**: `netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`
- **Apache-2.0** licensed

---

## Installation

```bash
# 门面包（推荐）：仅传递依赖，自动引入 Core
dotnet add package DevTrove.Crypto

# 或显式引用实现包
dotnet add package DevTrove.Crypto.Core
```

Requires .NET 8.0 or later at runtime; for older runtimes (e.g. .NET Framework, Unity), the `netstandard2.0` / `netstandard2.1` builds apply.

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
| `netstandard2.1` | .NET Core 3.x hosts |
| `net8.0` / `net9.0` | Current LTS / STS |
| `net10.0` | Latest features |

Restoring `netstandard2.0` triggers a compilation probe; missing APIs are polyfilled in `Compat/` (see [`docs/development-guide.md`](docs/development-guide.md)).

---

## Repository layout

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/          metapackage (no source)
│  ├─ DevTrove.Crypto.Core/     implementation
│  └─ DevTrove.Crypto.Tls/      TLS probe engine (Phase 0)
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   openssl / tongsuo CLI helpers
├─ scripts/                       fixture generation
└─ docs/                          development documentation (Chinese)
```

---

## Documentation

`docs/` ships each document in two languages: English default (`x.md`) + Chinese companion (`x.zh-CN.md`). Both have identical section / table / Mermaid structure.

| Document | Contents |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Internal layering, dependency direction, capability boundaries, known limitations |
| [docs/standards.md](docs/standards.md) | Coding standards, naming, Git workflow, review checklist |
| [docs/nuget.md](docs/nuget.md) | Package boundaries, versioning, release process |
| [docs/tls-scanner.md](docs/tls-scanner.md) | TLS probe engine design (scheduled for Phase 3 / Phase 4) |
| [docs/roadmap.md](docs/roadmap.md) | Library phase plan + pending / known deviations |
| [docs/development-guide.md](docs/development-guide.md) | Build / test / pack commands, TFM / polyfill rules, CI |
| [docs/library-api.md](docs/library-api.md) | Public API index |

---

## Build / test

```bash
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  DevTrove.Crypto.slnx -c Release
```

External dependency for interop tests: `openssl` 3.x. Missing `openssl` causes interop tests to **fail**, not skip (per [docs/standards.md](docs/standards.md)).

> The build currently fails with `NU1201` — see the pending-deviations table in [docs/roadmap.md](docs/roadmap.md) (items B1, B2).

---

## Status

**Early development.** First packages target `0.0.1-dev` and follow `0.0.X-dev → 0.1.0`. See [docs/roadmap.md](docs/roadmap.md).

---

## License

Apache-2.0 — see [LICENSE](LICENSE).
