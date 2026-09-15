# Development guide

> 中文对照：[development-guide.zh-CN.md](development-guide.zh-CN.md)

Build, test, pack, and CI conventions for `DevTrove.Crypto`.

---

## 1. Quick reference

| Action | Command |
|---|---|
| Build all (Release) | `dotnet build DevTrove.Crypto.slnx -c Release` |
| Run all tests | `dotnet test DevTrove.Crypto.slnx -c Release` |
| Run unit tests only (skip interop) | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category!=Integration'` |
| Run interop tests only | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category=Integration'` |
| Pack Core | `dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts` |
| Pack Metapackage | `dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -o ./artifacts` |
| Generate PFX fixtures (CI also runs) | `./scripts/generate-test-pfx.sh` |

External dependency: **tongsuo**. Tests fail (not skip) when it is missing. See §5.2.

---

## 2. Project layout

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/         metapackage (no source — see roadmap `RM-0.0.2`)
│  ├─ DevTrove.Crypto.Core/    implementation
│  └─ DevTrove.Crypto.Tls/     TLS probe engine (planned, `0.4.0` — not present yet)
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   CLI helpers (tongsuo)
├─ scripts/                     fixture generation
├─ .github/workflows/build.yml  CI
├─ Directory.Build.props        global build props + NuGet metadata
├─ Directory.Packages.props     CPM
├─ DevTrove.Crypto.slnx         solution
├─ global.json                  SDK lock
└─ docs/                        this documentation
```

---

## 3. Build properties

`Directory.Build.props` sets:

```xml
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

<!-- NuGet metadata baseline (override per-project as needed) -->
<Authors>blue-cloud-net</Authors>
<Company>blue-cloud-net</Company>
<Copyright>Copyright © blue-cloud-net 2026</Copyright>
<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
<RepositoryUrl>https://github.com/blue-cloud-net/DevTrove.Crypto</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<PackageProjectUrl>https://github.com/blue-cloud-net/DevTrove.Crypto</PackageProjectUrl>
<PackageTags>crypto;x509;certificate;asn1;pem;pkcs12;crl;csr;sm2;sm3;sm4;gm;bouncycastle</PackageTags>
<PackageReadmeFile>README.md</PackageReadmeFile>
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>

<!-- Target frameworks (declared in the baseline; every csproj must agree) -->
<TargetFrameworks>netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0</TargetFrameworks>
```

Each csproj either inherits this baseline or overrides it. Today all csproj files inherit the 5-TFM baseline declared here.

### The `netstandard` targets

The two `netstandard` targets **have never produced an assembly**, and the story the README told about why was wrong twice over: the polyfill is not in a `Compat/` directory (it is `Extensions/ArgumentNullExceptionExtensions.cs`), and its guard `#if NETSTANDARD2_0` does not cover `netstandard2.1`. On top of that, `Convert.FromHexString`, `RandomNumberGenerator.GetBytes(int)` and `AsSpan` are used without guards on those targets.

Both targets are **staying** — this is a NuGet library and the compatibility surface is part of the product. Work is tracked as `RM-0.0.11`.

---

## 4. Consumer-side integration (out of scope)

How a consumer references the packages — `ProjectReference` during development, `PackageReference` from a feed, or a local folder feed for unreleased builds — is a consumer decision. This repository does not ship a switch for it and does not test one.

One consequence is worth stating because it bites at pack time: a `ProjectReference` does not turn into a NuGet dependency. A package built from project references will be missing its dependency declaration, and consumers will fail to restore. See [nuget.md §10](nuget.md).

---

## 5. Tests

### 5.1 Frameworks

| | |
|---|---|
| Test framework | **xUnit 2.9.2** + **FluentAssertions 6.12.1** |
| Mock | (not currently needed) |
| Coverage | coverlet |
| Test project naming | `<Subject>.Tests` |
| Method naming | `Method_Should_Behavior_When_Condition` |
| Structure | Arrange–Act–Assert, separated by blank lines |

> **Known deviation** (`RM-0.0.7`): `DevTrove.Crypto.TestSupport.csproj` does not declare `net9.0`, while the test project does. An xUnit v3 upgrade is under consideration but is not scheduled.

### 5.2 `TongsuoCli`

Interop tests (key / certificate / CSR / CRL / PKCS#12 generation and parsing, plus the ShangMi family) shell out to **tongsuo**, an OpenSSL distribution that carries the Chinese national algorithms. It is the **only** external tool this repository depends on.

| | |
|---|---|
| Override path | environment variable `TONGSUO_PATH` (default `/opt/tongsuo/bin/tongsuo`) |
| Version requirement | a build supporting SM2 / SM3 / SM4 |
| Purpose | (a) interop test: generate with tongsuo → parse with this library, and the reverse; (b) fallback generation for fixtures |

**Upstream OpenSSL is no longer used.** Tongsuo is a fork and is command-compatible, but it is not the same implementation — so interoperability against upstream OpenSSL is no longer asserted. That trade-off is recorded as risk R2 in [roadmap.md §8](roadmap.md). An optional, non-blocking cross-check against upstream OpenSSL is the documented escape hatch. `RM-0.1.0-06` merges the former `OpenSslCli` helper into `TongsuoCli`.

### 5.3 Tools-missing behavior (important)

The test base in `DevTrove.Crypto.TestSupport` is `CliToolGuard`: when the external tool is unavailable, tests **fail (Fail), not skip (Skip)**.

This means: **the CI image must provide tongsuo**, otherwise every interop test fails.

This is an intentional trade-off:

| Choice | Result |
|---|---|
| ✅ Fail | Environment issues surface immediately; never "tests green but actually untested" |
| ❌ Skip | Easy to mask issues; interop defects only show up post-release |

If running tests without tongsuo is ever needed, use an **explicit test category** (e.g. `[Trait("RequiresTongsuo", "true")]`) and filter by environment — never silently skip.

### 5.4 Cross-validation (not in CI)

Some scanner determinism checks have to be compared against independent implementations. The following steps run **manually and locally**, not in CI:

1. Scan public test sites covering expired certs, self-signed certs, hostname mismatch, weak suites, no SNI, old-protocol-only
2. Use `tongsuo s_client` to get the same target's protocol, suite and certificate chain
3. Optionally use `testssl.sh` to get the same target's verdict
4. Triangulate; resolve each difference

`testssl.sh` is GPLv2 — **never** ship it with this repository; it is an external cross-check tool only.

---

## 6. CI

`.github/workflows/build.yml`:

| Trigger | Action |
|---|---|
| Push to `main` / `dev` | Build + test |
| Pull Request into `main` / `dev` | Build + test |
| Push `v*` tag | Build + test + pack + publish (needs `NUGET_API_KEY` secret in the `nuget` environment) |

> The workflow currently triggers on `main` only; the `dev` trigger is part of `RM-0.0.6`.

Jobs:

| Job | Purpose | Note |
|---|---|---|
| `build` | Build + unit tests + interop tests + coverage + pack to `artifacts/` | Runs on `ubuntu-latest` with .NET 8.x / 9.x / 10.x. Must build tongsuo (pinned version, cached) before the interop stage — see `RM-0.0.9` |
| `publish` | Pack + push to nuget.org on tag | Tagged-triggered; needs `NUGET_API_KEY` |

### CI publish order

1. `DevTrove.Crypto.Core`
2. `DevTrove.Crypto`
3. `DevTrove.Crypto.Tls`

NuGet does not support atomic multi-package publishing. To avoid "dependency bumped but not yet published" windows, either (a) **publish the dependency first**, then update the consumer, or (b) follow the order above in CI.

### CI known deviations

- (`RM-0.0.6`, partial): publish-job build step, push globs, branch trigger and submodule checkout have been fixed; full tag-triggered end-to-end verification is still pending a CI run
- (`RM-0.0.9f`, partial): the `tongsuo` job builds and installs Tongsuo 8.4.0 with caching; the integration step now uses tongsuo. End-to-end verification still pending a CI run.
- (`RM-0.0.9b` / `RM-0.0.9c`): the SM2 self-signed-cert and SM2 CRL fixture sections in the generator scripts will land once the rebuilt Core ships the corresponding helpers.

---

## 7. Packaging

### Local pack

```
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts --include-symbols
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts --include-symbols
```

Output: `*.nupkg` + `*.snupkg` under `./artifacts/`.

### Pack-time concerns

- `IncludeSymbols=true` + `SymbolPackageFormat=snupkg` — both set in `Directory.Build.props`.
- `GenerateDocumentationFile=true` — set per library csproj.
- `PackageReadmeFile=README.md` — declared in `Directory.Build.props`; csproj must `<None Include="..\..\README.md" Pack="true" PackagePath="\" />`.
- `RepositoryUrl` / `RepositoryType` / `PackageProjectUrl` — pointing to `https://github.com/blue-cloud-net/DevTrove.Crypto`.

After pack, verify:

- The `.nupkg` contains XML docs (`lib/<tfm>/*.xml`).
- The `.nupkg` contains the README at the root.
- All declared TFM have `lib/<tfm>/` directories.
- No `runtimes/*/native/` content.

---

## 8. Fixture management

### 8.1 Layout

`tests/data/` is **entirely generated and entirely ignored by Git** (`.gitignore` line `tests/data/`). Nothing under it is version-controlled — not even the per-directory `README.md` files, so those are local scratch notes rather than documentation.

```
tests/data/                      ← generated, never committed
├── certs/        certificates (real site chains, self-signed chains, SM2 certs)
├── crls/         CRLs (including SM2)
├── csrs/         CSRs (with extension combinations, SM2)
├── keys/         keys (RSA / EC / DSA / SM2, PEM + DER, including encrypted private keys)
├── pfx/          PKCS#12 (password-protected)
└── ocsp/         OCSP response fixtures (planned — see `RM-0.0.10`)

tests/fixtures/                  ← version-controlled
└── ntls/         NTLS handshake bytes captured from public ShangMi sites (planned — `RM-0.0.10`)
```

The split follows one rule: **anything a script can regenerate is generated; anything captured from the outside is committed.** `TestData` currently exposes helpers for the five generated directories only.

### 8.2 Sources & policy

| Fixture | Source | Committed? | Note |
|---|---|---|---|
| RSA / EC / DSA keys and certificates | generated by `tongsuo` | ❌ | Rebuilt by script whenever missing |
| **SM2 keys, certificates, CSR, CRL** | generated by `tongsuo` | ❌ | Requires tongsuo on the machine; the script does not fall back to a weaker tool |
| PKCS#12 | generated by `tongsuo` | ❌ | `.gitignore` also ignores `*.pfx` separately |
| Real-site certificate chains | captured from public sites by `pull-website-certs.sh` | ❌ | Lands in the generated tree, so it is rebuilt rather than committed |
| **NTLS handshake bytes** | captured from public ShangMi sites | ✅ | The one exception — no script can produce these, so they live in `tests/fixtures/ntls/`. See §8.4 |

**Unified passphrase**: all encrypted private keys and PKCS#12 use the same test passphrase (currently `test1234`). This section is the tracked record of that convention — the per-directory `tests/data/<sub>/README.md` files are generated alongside the fixtures and are not version-controlled.

### 8.3 Fixture scripts

There are five scripts, one per algorithm family — ShangMi fixtures are generated **alongside** their standard counterparts, not by a separate script:

```
scripts/
├── generate-test-certs.sh       RSA / EC / DSA / SM2 self-signed + CA→leaf chains
├── generate-test-crl.sh         CRLs with two revoked entries plus reasons, including SM2
├── generate-test-pfx.sh         private key + cert / cert chain PFX (test1234)
├── generate-test-csrs.sh        CSRs with extension combinations, including SM2
├── generate-test-keys.sh        RSA / EC / DSA / SM2 keys (PEM + DER, encrypted variants)
└── pull-website-certs.sh        capture published site chains into tests/data/certs/
```

Today `generate-test-certs.sh` and `generate-test-crl.sh` have no SM2 sections at all, even though SM2 fixtures of both kinds exist in the working tree — meaning they were produced by something no longer in the repository, and cannot be reproduced today. `TestDataGenerator` also still references a `generate-test-sm-certs.sh` that does not exist. Both are `RM-0.0.9`.

### 8.4 NTLS handshake byte fixtures (key)

Since the library does not use a native national-crypto protocol stack, NTLS detection regression **cannot be exercised via a loopback server**. The replacement is:

| Mechanism | Note |
|---|---|
| **Static byte fixtures** | Capture real `ServerHello` / `Certificate` / `ServerKeyExchange` bytes from public ShangMi sites into `tests/fixtures/ntls/`; tests feed the parser directly — no national-crypto protocol stack is needed on the machine |
| **Fake server replay** | Spin up a minimal TCP listener in the test process, replay those bytes, and validate the full probe path, including timeout and exception handling |

Fixtures should cover:

| Variant | Purpose |
|---|---|
| Standard NTLS (dual cert + GCM suite) | Happy path |
| Single cert (only signing) | Verify "dual-cert missing" verdict |
| Non-SM2 cert | Verify algorithm-detection robustness |
| CBC suite negotiated | Verify suite classification |
| Server rejects (returns alert) | Verify "NTLS unsupported" verdict |
| Truncated response bytes | Verify parser boundary checks |

---

## 9. Local workflow

```bash
# 1. Clone
git clone https://github.com/blue-cloud-net/DevTrove.Crypto.git
cd DevTrove.Crypto

# 2. Make sure tongsuo is available (tests require it)
#    Default: /opt/tongsuo/bin/tongsuo — override with TONGSUO_PATH
export TONGSUO_PATH=/opt/tongsuo/bin/tongsuo

# 3. (Optional) regenerate fixtures if any drift
./scripts/generate-test-keys.sh
./scripts/generate-test-certs.sh
./scripts/generate-test-csrs.sh
./scripts/generate-test-crl.sh
./scripts/generate-test-pfx.sh

# 4. Build + test
#    Expect failures until RM-0.0.1 and RM-0.0.11 land
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  DevTrove.Crypto.slnx -c Release

# 5. Pack
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts
```

---

## 10. Related

| Document | Contents |
|---|---|
| [architecture.md](architecture.md) | Project layout, dependency direction, capability boundaries |
| [standards.md](standards.md) | Coding conventions |
| [nuget.md](nuget.md) | Package boundaries, versioning, release process |
| [tls-scanner.md](tls-scanner.md) | TLS probe engine design |
| [roadmap.md](roadmap.md) | Phase plan + pending / known deviations |
