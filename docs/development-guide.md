# Development guide

> 中文对照：[development-guide.zh-CN.md](development-guide.zh-CN.md)

Build, test, pack, and CI conventions for `DevTrove.Crypto`.

---

## 1. Quick reference

| Action | Command |
|---|---|
| Build all (Release) | `dotnet build DevTrove.Crypto.slnx -c Release` |
| Run contract tests (Linux) | `dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0` |
| Run contract tests against the `netstandard2.0` asset (Windows) | `dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net48` |
| Run unit tests only (skip interop) | `dotnet test tests/DevTrove.Crypto.Core.Tests -c Release --framework net10.0 --filter 'Category!=Integration'` |
| Run interop tests only | `dotnet test tests/DevTrove.Crypto.Core.Tests -c Release --framework net10.0 --filter 'Category=Integration'` |
| Pack Abstractions | `dotnet pack src/DevTrove.Crypto.Abstractions/DevTrove.Crypto.Abstractions.csproj -c Release -o ./artifacts` |
| Pack Core | `dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts` |
| Pack Metapackage | `dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -o ./artifacts` |
| Generate PFX fixtures (CI also runs) | `./scripts/generate-test-pfx.sh` |

> **Naming the project and the framework is deliberate.** The `net48` target of the test project only exists on Windows (see §2), so `dotnet test DevTrove.Crypto.slnx` without `--framework` cannot complete on Linux. Naming both keeps every documented command runnable on both platforms.

External dependency: **tongsuo**. Tests fail (not skip) when it is missing. See §5.2.

---

## 2. Project layout

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto.Abstractions/  contracts (zero dependencies — planned, `0.1.0`)
│  ├─ DevTrove.Crypto/               metapackage (no source — see roadmap `RM-0.0.2`)
│  ├─ DevTrove.Crypto.Core/          implementation
│  └─ DevTrove.Crypto.Tls/           TLS probe engine (planned, `0.6.0` — not present yet)
├─ tests/
│  ├─ DevTrove.Crypto.Abstractions.Tests/  contract tests; targets `net48` on Windows only
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/         CLI helpers (tongsuo)
├─ scripts/                     fixture generation
├─ .github/workflows/ci.yml     branch CI + the abstraction test matrix
├─ .github/workflows/release.yml tag-driven publish
├─ Directory.Build.props        global build props + NuGet metadata
├─ Directory.Packages.props     CPM
├─ DevTrove.Crypto.slnx         solution
├─ global.json                  SDK lock
└─ docs/                        this documentation
```

### Test project target frameworks

`DevTrove.Crypto.Abstractions.Tests` is the host that makes the `netstandard2.0` asset runtime-verified: a `net48` project resolves `lib/netstandard2.0/`, while a `net8.0`+ project resolves its own asset. Because .NET Framework cannot run on Linux, the `net48` target is declared **conditionally on the operating system**:

```xml
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('Windows'))">net48;net8.0;net9.0;net10.0</TargetFrameworks>
<TargetFrameworks Condition="!$([MSBuild]::IsOSPlatform('Windows'))">net8.0;net9.0;net10.0</TargetFrameworks>
```

This keeps `dotnet build DevTrove.Crypto.slnx` working on Linux without the `Microsoft.NETFramework.ReferenceAssemblies` package, which would otherwise be required to even build a `net48` target there.

---

## 3. Build properties

`Directory.Build.props` sets:

```xml
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>

<!-- TreatWarningsAsErrors / EnforceCodeStyleInBuild are intentionally OFF:
     see standards.md §2.3. New code must still produce 0 warnings (§9.3). -->

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
<TargetFrameworks>netstandard2.0;net8.0;net9.0;net10.0</TargetFrameworks>
```

Each csproj either inherits this baseline or overrides it. Today all csproj files inherit the 4-TFM baseline declared here.

### Additional package references

`DevTrove.Crypto.Abstractions` needs `Span<T>` on its only `netstandard` target, so it carries a conditional reference:

```xml
<PackageReference Include="System.Memory" Condition="'$(TargetFramework)' == 'netstandard2.0'" />
```

The version is declared once in `Directory.Packages.props`, like every other package (see [standards.md §2.2](standards.md)).

### The `netstandard2.0` target

This target is **staying** — this is a NuGet library and the compatibility surface is part of the product. `netstandard2.1` is gone: no non-EOL host resolves that asset, so it could never be runtime-verified (see [nuget.md §5](nuget.md)).

Polyfills land under the path the `0.1.0` target layout names `Compat/`, and the guard symbol is chosen **per API** — `Convert.FromHexString` needs `NETSTANDARD2_0`, while `HashAlgorithm.HashCore(ReadOnlySpan<byte>)` differs between `netstandard2.1` and `netstandard2.0`. See `RM-0.0.11`.

---

## 4. Consumer-side integration (out of scope)

How a consumer references the packages — `ProjectReference` during development, `PackageReference` from a feed, or a local folder feed for unreleased builds — is a consumer decision. This repository does not ship a switch for it and does not test one.

One consequence is worth stating because it bites at pack time: a `ProjectReference` does not reliably turn into a NuGet dependency. A package built from project references can end up missing its dependency declaration, and consumers then fail to restore. `DevTrove.Crypto.Abstractions` is the first package in this repository to make that concrete — `DevTrove.Crypto.Core` must declare it. Always unpack the `.nupkg` and read the `.nuspec`; see [nuget.md §8](nuget.md) for the checklist entry.

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

### 5.1.1 Contract tests for the abstractions

`DevTrove.Crypto.Abstractions.Tests` carries no external dependency at all — no BouncyCastle, no tongsuo — so it runs fast and needs no fixture generation. Its stub types live in `_TestStubs/`; they are the only way to exercise an `abstract` base class, and they exist to test the behaviour the base class actually implements rather than the `abstract` keyword:

| Tested | Why |
|---|---|
| Default `Cbc` / `Pkcs7` | The defaults are real, shipped behaviour |
| ECB makes `Encrypt` throw | A guard in the base class, exercised only through a concrete subclass |
| The four BCL-compatible modes through `AsSymmetricAlgorithm()` | The adapter is fully concrete and is the deliverable of `RM-0.1.0-05` |
| GCM / CTR throw `NotSupportedException` | Documents where the BCL bridge stops |
| `DigestBase` chunked `Update` equals one-shot `ComputeHash` | Buffer management is inherited logic |
| `AsymmetricKeyBase.Dispose` clears key material | `standards.md §3.8` makes this a hard rule |
| All four X.509 interfaces implemented by one stub | Proves the interfaces are implementable before `0.5.0` commits to them |

### 5.2 `TongsuoCli`

Interop tests (key / certificate / CSR / CRL / PKCS#12 generation and parsing, plus the ShangMi family) shell out to **tongsuo**, an OpenSSL distribution that carries the Chinese national algorithms. It is the **only** external tool this repository depends on.

| | |
|---|---|
| Override path | environment variable `TONGSUO_PATH` (default `/opt/tongsuo/bin/tongsuo`) |
| Version requirement | a build supporting SM2 / SM3 / SM4 |
| Purpose | (a) interop test: generate with tongsuo → parse with this library, and the reverse; (b) fallback generation for fixtures |

**Upstream OpenSSL is no longer used.** Tongsuo is a fork and is command-compatible, but it is not the same implementation — so interoperability against upstream OpenSSL is no longer asserted. That trade-off is recorded as risk R2 in [roadmap.md §8](roadmap.md). An optional, non-blocking cross-check against upstream OpenSSL is the documented escape hatch. The former `OpenSslCli` helper is merged into `TongsuoCli` — tracked as an unversioned baseline item in [roadmap.md §5](roadmap.md).

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

CI is split across two workflows so that branch checks and tag-driven releases stay independently auditable.

### 6.1 `ci.yml` — branch checks

`.github/workflows/ci.yml`. Triggers: push / pull_request on **`main` only**, plus `workflow_call` (re-used by `release.yml`) and `workflow_dispatch` (manual warm-up of the tongsuo cache or ad-hoc verification on a feature branch).

> **`dev` is intentionally not covered.** `dev` is the integration / development branch; CI responsibility lives on `main`. Developers verify their work locally with `dotnet build DevTrove.Crypto.slnx -c Release` and `dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0` before opening a PR against `main`, or use `workflow_dispatch` for a one-off check on a feature branch. The integration tests (which require `tongsuo`) and the tag-driven `release.yml` flow are the only other paths that exercise the full pipeline.

Top-level `permissions: contents: read`. `concurrency: ci-<workflow>-<ref> cancel-in-progress: true` cancels a stale in-flight run when the same branch is pushed again.

Jobs:

| Job | Purpose | Note |
|---|---|---|
| `tongsuo` | Build & install Tongsuo 8.4.0 from source, cached by OS + version (`actions/cache@v4`) | See `RM-0.0.9f`. Must finish before `build` |
| `build` | Build + unit tests + interop tests + coverage; on `push` (not PR), also pack to `./artifacts/` | `ubuntu-latest`, .NET 8.x / 9.x / 10.x |
| `test-abstractions` | Matrix job running the contract tests per target framework | `os ∈ {ubuntu-latest, windows-latest}` × `tfm ∈ {net8.0, net9.0, net10.0, net48}`, with invalid combinations excluded (`net48` only on Windows, the rest only on Linux). **Needs no tongsuo** — the contract tests have no external dependency |

The `net48` leg is what makes the `netstandard2.0` asset runtime-verified: a `net48` project resolves `lib/netstandard2.0/`. Without it, that asset would only ever be build-verified.

The pack step writes `*.nupkg` + `*.snupkg` to `./artifacts/` for local inspection only. **It does not push to nuget.org** — pushing is `release.yml`'s job.

### 6.2 `release.yml` — tag-driven publish

`.github/workflows/release.yml`. Triggers on `push` of `v*` tags only. `concurrency: release-<ref> cancel-in-progress: false` (in-flight publishes must not be cancelled mid-flight).

The workflow has three jobs with strict ordering:

1. **`verify-version`** — guard. Reads `Directory.Build.props` and compares four invariants against the pushed tag (`${{ github.ref_name }}` minus the leading `v`). **Any mismatch fails the run before anything is packed or pushed**:

   | Invariant | Expected |
   |---|---|
   | `<Version>` | equals the tag version (e.g. tag `v0.4.0` → `<Version>0.4.0</Version>`) |
   | `<PackageLicenseExpression>` | `Apache-2.0` (matches repo `LICENSE`) |
   | `<TargetFrameworks>` | contains all of `netstandard2.0;net8.0;net9.0;net10.0` |
   | `<RepositoryUrl>` | contains `DevTrove.Crypto` |

   The job also detects prerelease tags (any `-` in the version segment, e.g. `v1.0.0-rc.1`) and exposes `is-prerelease` as a job output.

   **The version is not injected dynamically.** The maintainer edits `<Version>` in `Directory.Build.props`; the machine verifies. See `nuget.md §6.2` for the rationale.

2. **`ci`** — `needs: verify-version`, `uses: ./.github/workflows/ci.yml`. Re-runs the same compile + test gate that branches go through, so a tag cannot reach `release` unless CI is green.

3. **`release`** — `needs: ci`, `environment: nuget`, `permissions: contents: write`. Re-runs `dotnet restore` + `dotnet build` + `dotnet pack` (intentionally — the artifact stream is small enough that re-packing is cheaper than wiring `upload-artifact` / `download-artifact` across workflows). The re-used fact is "CI passed", not the file bytes.

   Then in order:

   1. `dotnet nuget push ./artifacts/DevTrove.Crypto.Core.*.nupkg` (Core first — Metapackage depends on it)
   2. `dotnet nuget push ./artifacts/DevTrove.Crypto.[0-9]*.nupkg` (Metapackage)
   3. `softprops/action-gh-release@v2` attaches `artifacts/*.nupkg` + `artifacts/*.snupkg` to a GitHub Release; `fail_on_unmatched_files: true` so a missing artifact fails loud; `prerelease` is sourced from `verify-version.outputs.is-prerelease`.

### 6.3 NuGet publish order

1. `DevTrove.Crypto.Core`
2. `DevTrove.Crypto`
3. `DevTrove.Crypto.Tls`

NuGet does not support atomic multi-package publishing. To avoid "dependency bumped but not yet published" windows, either (a) **publish the dependency first**, then update the consumer, or (b) follow the order above in CI.

### 6.4 CI known deviations

- (`RM-0.0.6`): the release workflow's tag-triggered end-to-end run has not been exercised against a real tag yet.
- (`RM-0.0.9f`, partial): the `tongsuo` job builds and installs Tongsuo 8.4.0 with caching; the integration step now uses tongsuo. End-to-end verification still pending a CI run.
- (`RM-0.0.9b` / `RM-0.0.9c`): the SM2 self-signed-cert and SM2 CRL fixture sections in the generator scripts will land once the rebuilt Core ships the corresponding helpers.

---

## 7. Packaging

### Local pack

```
dotnet pack src/DevTrove.Crypto.Abstractions/DevTrove.Crypto.Abstractions.csproj -c Release -o ./artifacts --include-symbols
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
- `DevTrove.Crypto.Core`'s `.nuspec` declares `<dependency id="DevTrove.Crypto.Abstractions" />`. Unpack and read it — do not assume a `ProjectReference` became a dependency.

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

Today `generate-test-certs.sh` and `generate-test-crl.sh` have no SM2 sections at all, even though SM2 fixtures of both kinds exist in the working tree — meaning they were produced by something no longer in the repository, and cannot be reproduced today. That is `RM-0.0.9b` / `RM-0.0.9c`.`

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
#    Contract tests need nothing else; interop tests need tongsuo.
#    RM-0.0.11 still gates `netstandard2.0`.
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0

# 5. Pack (dependency order)
dotnet pack src/DevTrove.Crypto.Abstractions/DevTrove.Crypto.Abstractions.csproj -c Release -o ./artifacts
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
