# Development guide

> 中文对照：[development-guide.zh-CN.md](development-guide.zh-CN.md)

Build, test, pack, and CI conventions for `DevTrove.Crypto`.

---

## 1. Quick reference

| Action | Command |
|---|---|
| Build all (Release) | `dotnet build DevTrove.Crypto.slnx -c Release` |
| Run all tests | `dotnet test DevTrove.Crypto.slnx -c Release` |
| Run unit tests only (skip OpenSSL interop) | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category!=Integration'` |
| Run interop tests only | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category=Integration'` |
| Pack Core | `dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts` |
| Pack Metapackage | `dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -o ./artifacts` |
| Generate PFX fixtures (CI also runs) | `./scripts/generate-test-pfx.sh` |

External dependency: `openssl` 3.x. Tests fail (not skip) when it is missing.

> **Known deviation** (see [roadmap.md §7 B1, B2](roadmap.md)): `dotnet build DevTrove.Crypto.slnx -c Release` is currently broken with `NU1201` because of the TFM mismatch between the global props and the per-project csproj settings. Tracked in the roadmap.

---

## 2. Project layout

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/         metapackage (no source — see roadmap B2)
│  ├─ DevTrove.Crypto.Core/    implementation
│  └─ DevTrove.Crypto.Tls/     TLS probe engine (Phase 0 scaffold)
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   OpenSSL CLI helpers
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

<!-- Target frameworks (target = 5; current = 3 + 1) -->
<TargetFrameworks>netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0</TargetFrameworks>
```

Each csproj either inherits this baseline or overrides it (the Core project today overrides to 3 TFM, the metapackage to 1 — see roadmap B1).

### `netstandard2.0` polyfill

Per roadmap B3, the `Compat/` polyfill directory **does not exist** today. The README claim that `netstandard2.0` missing APIs are polyfilled in `Compat/` is currently false.

Decide:

- **Drop `netstandard2.0`** from the global TargetFrameworks if no consumer needs it.
- **Or create `Compat/`** with the required polyfills (`System.ComponentModel.Annotations` etc.) and document the list.

Until this is resolved, the `[netstandard2.0]` build is not real.

---

## 4. `ProjectReference` ↔ `PackageReference` switch

> **Known deviation** (roadmap B6): the conditional property switch is **planned but not implemented**. Today every consumer uses `ProjectReference`; packaging produces packages whose dependency metadata is incomplete.

The planned form:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <UseCryptoProjectRef Condition="'$(UseCryptoProjectRef)' == ''">true</UseCryptoProjectRef>
</PropertyGroup>
```

```xml
<!-- Consumer csproj -->
<ItemGroup Condition="'$(UseCryptoProjectRef)' == 'true'">
  <ProjectReference Include="..\..\..\lib\Crypto\src\DevTrove.Crypto\DevTrove.Crypto.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(UseCryptoProjectRef)' != 'true'">
  <PackageReference Include="DevTrove.Crypto" Version="[1.2.0, )" />
</ItemGroup>
```

- Dev (default): `<UseCryptoProjectRef>true</UseCryptoProjectRef>` → `ProjectReference` to the submodule.
- Pack / release: `<UseCryptoProjectRef>false</UseCryptoProjectRef>` → restore from nuget.org.
- Local un-published version coupling: local NuGet feed (folder or local feed).

**Why this matters**: `ProjectReference` cannot automatically convert to a NuGet dependency. If `ProjectReference` is still in use at pack time, the produced package will lack a `DevTrove.Crypto` dependency declaration and consumers will fail to restore.

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

> **Known deviation** (roadmap B11): upgrading to xUnit v3 is on the roadmap. `DevTrove.Crypto.TestSupport.csproj` does not currently declare `net9.0`; once added, the integration filter `Category!=Integration` works for both `net9.0` and `net10.0`.

### 5.2 `OpenSslCli`

Interop tests (key / certificate / CSR / CRL / PKCS#12 generation & parsing) depend on the `openssl` executable.

| | |
|---|---|
| Override path | environment variable `OPENSSL_PATH` (the helper looks up default `openssl` on PATH) |
| Version requirement | must support SM2 / SM3 / SM4 (OpenSSL 3.x) |
| Purpose | (a) interop test: generate via OpenSSL → parse via this library (and reverse); (b) fixture fallback generation |

**Removed national-crypto CLI dependency** (planned, roadmap B4): the design intent is to drop the `TongsuoCli` requirement and generate SM2 fixtures with OpenSSL. Currently `TongsuoCli.cs` is still in the tree and several `tests/data/*/README.md` files reference `scripts/generate-test-sm-certs.sh`, which **does not exist**. Until B4 is resolved, either restore the script or replace the references.

### 5.3 Tools-missing behavior (important)

The test base in `DevTrove.Crypto.TestSupport` is `CliToolGuard`: when an external tool is unavailable, tests **fail (Fail), not skip (Skip)**.

This means: **the CI image must install `openssl`**, otherwise all interop tests fail.

This is an intentional trade-off:

| Choice | Result |
|---|---|
| ✅ Fail | Environment issues surface immediately; never "tests green but actually untested" |
| ❌ Skip | Easy to mask issues; interop defects only show up post-release |

If "no-openssl environment" support is needed in the future, use an **explicit test category** (e.g. `[Trait("RequiresOpenSsl", "true")]`) and filter by environment — never silently skip.

### 5.4 Cross-validation (not in CI)

Some scanner determinism checks must be compared against independent implementations. The following steps run **manually locally**, not in CI:

1. Scan public test sites covering expired certs, self-signed certs, hostname mismatch, weak suites, no SNI, old-protocol-only
2. Use `openssl s_client` to get the same target's protocol / suite / certificate chain
3. Use `testssl.sh` to get the same target's verdict
4. Triangulate; resolve each difference

`testssl.sh` is GPLv2 — **never** ship it with this repo; only as an external cross-check tool during development.

---

## 6. CI

`.github/workflows/build.yml`:

| Trigger | Action |
|---|---|
| Push to `main` / `dev` | Build + test |
| Pull Request into `main` / `dev` | Build + test |
| Push `v*` tag | Build + test + pack + publish (needs `NUGET_API_KEY` secret in the `nuget` environment) |

Jobs:

| Job | Purpose | Note |
|---|---|---|
| `build` | Build + unit tests + interop tests + coverage + pack to `artifacts/` | Runs on `ubuntu-latest` with .NET 8.x / 9.x / 10.x; installs `openssl` via `apt` for interop tests |
| `publish` | Pack + push to nuget.org on tag | Tagged-triggered; needs `NUGET_API_KEY` |

### CI publish order

1. `DevTrove.Crypto.Core`
2. `DevTrove.Crypto`
3. `DevTrove.Crypto.Tls`

NuGet does not support atomic multi-package publishing. To avoid "dependency bumped but not yet published" windows, either (a) **publish the dependency first**, then update the consumer, or (b) follow the order above in CI.

### CI known deviations (roadmap B7)

- `publish` job runs `dotnet pack ... --no-build` with no preceding `dotnet build` step. Add a `dotnet build` step in publish.
- Push Metapackage glob `DevTrove.Crypto.*.nupkg` **double-matches** the Core package. Restrict to `DevTrove.Crypto.*[!Core]*.nupkg` or push by exact filename.

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

```
tests/data/
├── certs/        certificates (real site chains, self-signed chains, SM2 certs)
├── crls/         CRLs (including SM2)
├── csrs/         CSRs (with extension combinations, SM2)
├── keys/         keys (RSA / EC / DSA / SM2, PEM + DER, including encrypted private keys)
├── pfx/          PKCS#12 (password-protected, NOT committed — generated locally / in CI)
├── ocsp/         OCSP response fixtures (planned — see roadmap B5)
└── ntls/         NTLS handshake byte fixtures (planned — see roadmap B5)
```

> Roadmap B5: `ocsp/` and `ntls/` directories do not exist yet.

### 8.2 Sources & policy

| Fixture | Source | Committed? | Note |
|---|---|---|---|
| RSA / EC / DSA keys and certificates | generated by `openssl` | ✅ | Reproducible via script |
| **SM2 keys, certificates, CSR, CRL** | generated by `openssl` (after B4) | ✅ | **Pre-generated fixtures committed**, to avoid CI depending on the national-crypto toolchain |
| PKCS#12 | generated by `openssl` | ❌ | `.gitignore` ignores `*.pfx`; **generated on-demand in CI** |
| Real-site certificate chains | captured from public sites | ✅ | Regression for chain parsing & verification |
| **NTLS handshake bytes** | captured from public ShangMi sites | ✅ | See [tls-scanner.md §8.4](tls-scanner.md) |

**Unified passphrase**: all encrypted private keys and PKCS#12 use the same test passphrase (currently `test1234`); documented in each `tests/data/<sub>/README.md`.

### 8.3 Fixture scripts

```
scripts/
├── generate-test-certs.sh       RSA / EC self-signed + CA+leaf chains
├── generate-test-crl.sh         CRL with two revoked entries + reasons
├── generate-test-pfx.sh         private key + cert / cert chain PFX (test1234)
├── generate-test-csrs.sh        CSRs with extension combinations
├── generate-test-keys.sh        RSA / EC / DSA keys (PEM + DER, encrypted variants)
└── generate-test-sm-certs.sh    SM2 certs / CSR via tongsuo (planned restoration — see B4)
```

> Roadmap B4: `generate-test-sm-certs.sh` does not exist; `tests/data/*/README.md` still references it.

### 8.4 NTLS handshake byte fixtures (key)

Since the library does not use a native national-crypto protocol stack, NTLS detection regression **cannot be exercised via a loopback server**. The replacement is:

| Mechanism | Note |
|---|---|
| **Static byte fixtures** | Capture real `ServerHello` / `Certificate` / `ServerKeyExchange` bytes from public ShangMi sites into `tests/data/ntls/`; tests feed the parser directly |
| **Fake server replay** | Spin up a minimal TCP listener in the test process, replay those bytes, and validate the full probe path (including timeout / exception handling) |

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
# 1. Clone (or use submodule inside the application repo)
git clone https://github.com/blue-cloud-net/DevTrove.Crypto.git
cd DevTrove.Crypto

# 2. Make sure `openssl` 3.x is on PATH (tests require it)
openssl version

# 3. (Optional) regenerate fixtures if any drift
./scripts/generate-test-certs.sh
./scripts/generate-test-keys.sh
./scripts/generate-test-pfx.sh

# 4. Build + test
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  DevTrove.Crypto.slnx -c Release

# 5. Pack
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts
```

> Today step 4 fails on `dotnet build DevTrove.Crypto.slnx -c Release` with `NU1201` (roadmap B1, B2).

---

## 10. Related

| Document | Contents |
|---|---|
| [architecture.md](architecture.md) | Project layout, dependency direction, capability boundaries |
| [standards.md](standards.md) | Coding conventions |
| [nuget.md](nuget.md) | Package boundaries, versioning, release process |
| [tls-scanner.md](tls-scanner.md) | TLS probe engine design |
| [roadmap.md](roadmap.md) | Phase plan + pending / known deviations |
