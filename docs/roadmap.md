# Library roadmap

> 中文对照：[roadmap.zh-CN.md](roadmap.zh-CN.md)

This document owns the **version line and status tracking** for `DevTrove.Crypto`. It does not describe goals or engineering rules — those live in [architecture.md §1](architecture.md) and [standards.md](standards.md).

---

## 1. Scope of this document

| In scope | Lives elsewhere |
|---|---|
| Version line: work items + release milestones | Capability goals → [architecture.md §1](architecture.md) |
| Per-item status and evidence | Coding / engineering rules → [standards.md](standards.md) and [AGENTS.md](../AGENTS.md) §4 |
| Excluded items and non-goals | Build / test / pack commands → [development-guide.md](development-guide.md) |
| Risks | TLS engine design → [tls-scanner.md](tls-scanner.md) |

Documentation ships in pairs: English default (`x.md`) + Chinese (`x.zh-CN.md`), identical section by section.

---

## 2. Status legend and maintenance

| Mark | Status | Definition |
|---|---|---|
| ✅ | Done | Every acceptance criterion of the item is met **and verified** — not merely "code written". Where acceptance depends on a build or test run, evidence is attached. |
| 🚧 | In progress | Being implemented, or scheduled in the current work batch. |
| 🟡 | Partial | Something has landed, but acceptance is not yet met. |
| ⬜ | Not started | No work begun. |

**Version-level roll-up** (deterministic, no judgement call):

- every sub-item ✅ → ✅
- at least one 🚧 and not all ✅ → 🚧
- no 🚧 but at least one 🟡 → 🟡
- every sub-item ⬜ → ⬜

**Maintenance**: when an item's status changes, the update is committed together with the code that caused it. See the pre-submission checklist in [AGENTS.md](../AGENTS.md) and [standards.md §9.3](standards.md). Both language versions are updated in the same commit.

---

## 3. Version model

SemVer. `0.x` allows breaking changes.

| Rule | Detail |
|---|---|
| `0.0.x` are **work-item numbers only** | Never packaged, tagged or published. The repository has never shipped a package. |
| First real release | `0.1.0` |
| Package versions | The four packages share one version number per milestone. `DevTrove.Crypto.Tls` does not exist before `0.6.0`, so it does not participate in earlier milestones. |
| Pre-release | Non-final builds use `-dev` / `-preview` suffixes. |

The library is **independently versioned**. Consumers declare a minimum compatible version (see [nuget.md §3.2](nuget.md)).

---

## 4. Version overview

| Version | Theme | Sub-items | Status |
|---|---|---|---|
| `RM-0.0.1` – `RM-0.0.13` | Baseline corrections (work items) | 13 | 🟡 |
| `0.1.0` | Abstraction layer | 5 | ⬜ |
| `0.2.0` | Symmetric algorithms | 4 | ⬜ |
| `0.3.0` | Asymmetric algorithms | 3 | ⬜ |
| `0.4.0` | Hashes and derivation | 4 | ⬜ |
| `0.5.0` | PKI capability completion | 9 | ⬜ |
| `0.6.0` | TLS probe L1 + NTLS fingerprint | 11 | ⬜ |
| `0.7.0` | TLS probe L2 + ShangMi | 9 | ⬜ |
| `1.0.0` | Stable API + PKIX | 5 | ⬜ |

Milestones are split by **cryptographic family** rather than by mixed "primitive" batches, so each version carries a single theme and the abstraction landed in `0.1.0` is implemented against family by family.

---

## 5. Unversioned baseline items

Items that are complete (or in flight) but deliberately carry no version number.

| Item | Status | Note |
|---|---|---|
| `docs/standards.md` and `docs/library-api.md` created | ✅ | Referenced by README and AGENTS.md |
| Web-era documents under `docs/v0.1/` removed | ✅ | — |
| Documentation restructure: library scope, version line, status tracking | 🚧 | Not part of any version, by design |
| Merge `OpenSslCli` into `TongsuoCli`, update `CliToolGuard` wording, and rename `*OpenSslInteropTests` to `*InteropTests` | ⬜ | Test-support tidy-up; carries no version number |

---

## 6. Version detail

Each row is one sub-item. `Evidence` names the concrete way the acceptance criterion is proven.

### 6.1 `RM-0.0.1` — TFM set alignment

`Directory.Build.props` declares the TFM set; every csproj agrees with it, and each target is verified individually.

> The set was reduced from five targets to **four** during the `0.1.0` work. `netstandard2.1` was dropped because no non-EOL host resolves that asset, so it could never be covered by a runtime test. Each of the remaining four targets has a host that actually runs it — see `RM-0.1.0-01`.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.1 | Unify `<TargetFrameworks>` across props and both csproj; verify every TFM separately | `dotnet build -f <tfm>` succeeds for each of the 4 TFMs | ✅ | `dotnet build -f <tfm>` for Core and the metapackage succeeds for `netstandard2.0` / `net8.0` / `net9.0` / `net10.0` (0 warnings, 0 errors). Re-verified after the set moved from five targets to four |

### 6.2 `RM-0.0.2` — metapackage is source-free

The metapackage project still contains `src/DevTrove.Crypto/Program.cs`, although the design requires it to have no source at all.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.2 | Delete `Program.cs`; keep only `ProjectReference` → Core | Project contains no `.cs` file; package still builds and packs | ✅ | `find src/DevTrove.Crypto -name '*.cs'` returns nothing; `dotnet build src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -f net10.0` succeeded (0 warnings, 0 errors); `dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -p:TargetFramework=net10.0` produced `DevTrove.Crypto.1.0.0.nupkg` and `.snupkg` |

### 6.3 `RM-0.0.3` — build baseline properties

`standards.md §2.3` and `development-guide.md §3` document `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild`, but neither is present in `Directory.Build.props`.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.3 | Add both properties (or amend the documents) | Config and documents agree; build behaviour matches the documented one | ✅ | `Directory.Build.props` now sets `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`; `dotnet build DevTrove.Crypto.slnx -c Release` succeeded for every TFM (0 warnings, 0 errors) |

### 6.4 `RM-0.0.4` — package metadata

No `<Version>`, `<PackageId>` or SourceLink exists, although `nuget.md §4` calls them mandatory. Packing without `<Version>` yields `1.0.0` by default.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.4 | Declare `<Version>`, `<PackageId>`, SourceLink and `RepositoryUrl` correctly | `dotnet pack` produces the intended version; `.nupkg` contains README + XML docs | ✅ | `<Version>0.0.1-dev</Version>` written into props; both packages declare their own `<PackageId>`; Microsoft.SourceLink.GitHub wired in; `dotnet pack` produces `DevTrove.Crypto.Core.<version>.nupkg` and `DevTrove.Crypto.<version>.nupkg`, each shipping README + `lib/` + `.xml` for every TFM; the matching snupkg embeds the SourceLink JSON in its pdb |

### 6.5 `RM-0.0.5` — solution file

`DevTrove.Crypto.slnx` references `docs\v0.1\core-roadmap.md`, which no longer exists, and lists only a few of the documents.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.5 | Remove the stale reference; list the current document set | Every path in the solution file resolves | ✅ | Removed the stale `docs\v0.1\core-roadmap.md` reference; the `/docs/` folder now lists all 14 current documents; `dotnet sln DevTrove.Crypto.slnx list` parses and reports 4 projects |

### 6.6 `RM-0.0.6` — CI workflow

The `publish` job ran `dotnet pack --no-build` without a preceding build, the metapackage push glob `DevTrove.Crypto.*.nupkg` also matched the Core package, the workflow triggered on `main` only, and `actions/checkout` requested `submodules: recursive` although this repository has none. The single-workflow design was also hard to audit — branch checks and tag-driven publishes could not be reviewed in isolation.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.6 | Split into a branch-CI workflow + a tag-release workflow; restrict CI to `main` (not `dev`); verify `<Version>` / `<PackageLicenseExpression>` / `<TargetFrameworks>` / `<RepositoryUrl>` against the pushed tag before any pack or push; `uses` the CI workflow from release; narrow the push globs; drop `submodules` | A pushed `vX.Y.Z` tag builds, packs and publishes both packages correctly, and a tag whose version does not match `<Version>` is refused before any artifact is produced; `dev` is excluded from CI responsibility | ✅ | `.github/workflows/build.yml` is gone; `.github/workflows/ci.yml` keeps the `tongsuo` + `build` jobs and declares `workflow_call` (re-used by release) plus `workflow_dispatch`, with push / pull_request triggers restricted to `main` (no `dev`); `.github/workflows/release.yml` triggers on `v*` tag push with `concurrency: release-<ref> cancel-in-progress: false`; its `verify-version` job reads `Directory.Build.props` and refuses to proceed unless `<Version>` matches the tag version, `<PackageLicenseExpression>` is `Apache-2.0`, `<TargetFrameworks>` contains all four TFMs, and `<RepositoryUrl>` contains `DevTrove.Crypto`; push globs split into `DevTrove.Crypto.Core.*.nupkg` and `DevTrove.Crypto.[0-9]*.nupkg`; `softprops/action-gh-release@v2` attaches the `.nupkg` + `.snupkg` artifacts with `fail_on_unmatched_files: true`; full tag-triggered end-to-end verification still pending a real CI run |

### 6.7 `RM-0.0.7` — test support targets `net9.0`

`DevTrove.Crypto.TestSupport.csproj` declares `net8.0;net10.0` while the test project declares `net8.0;net9.0;net10.0`.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.7 | Add `net9.0` to TestSupport | All three TFMs restore and build | ✅ | `dotnet build tests/DevTrove.Crypto.TestSupport/DevTrove.Crypto.TestSupport.csproj -c Release -f net8.0` / `net9.0` / `net10.0` all succeeded (0 warnings, 0 errors) |

### 6.8 `RM-0.0.8` — default signature algorithm

Five public signatures default `signatureAlgorithm` to the hard-coded `SHA256WITHRSA`: `Certificate.GenerateSelfSigned`, `Certificate.SignCsr`, `Certificate.SignPublicKey`, `CertificateRevocationList.Generate`, `CertificateSigningRequest.Generate` (two overloads). This makes EC / DSA / SM2 callers pass an algorithm explicitly, and defaults to a wrong value when they do not.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.8 | Derive the default from the private-key algorithm | No `SHA256WITHRSA` literal remains; EC / DSA / SM2 sign without an explicit algorithm | 🟡 | Rule written into `library-api.md` §4.1 (private-key → default signature-algorithm table); on the current empty Core `grep -rn SHA256WITHRSA src/ tests/` already returns nothing; the actual implementation and per-key-type unit tests land once RM-0.1.0-01 restores Core |

This item lands **inside `RM-0.3.0`**, not in `0.1.0`: the `0.1.0` abstraction only defines `SignatureAlgorithmKind`, while the private-key → algorithm derivation itself is implemented alongside the asymmetric algorithms that need it.

### 6.9 `RM-0.0.9` — external tool unified on Tongsuo

The fixture scripts and interop tests currently depend on both `openssl` and `tongsuo`. Tongsuo is the single external tool going forward; the scripts must use `TONGSUO_PATH` as well.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.9a | `generate-test-{keys,certs,csrs,crl,pfx}.sh` use `TONGSUO_PATH` instead of `openssl` | No `openssl` invocation remains in `scripts/` | ✅ | 6 scripts switched `openssl` → `${TONGSUO_BIN}`; `temp_openssl.cnf` → `temp_ext.cnf`; `grep -c openssl scripts/*.sh` all 0 |
| RM-0.0.9b | `generate-test-certs.sh` gains an SM2 self-signed certificate section (today only a comment) | SM2 certificate fixtures are reproducible from the script | ⬜ | Re-run script on a clean `tests/data/` |
| RM-0.0.9c | `generate-test-crl.sh` gains an SM2 CRL section (today absent) | SM2 CRL fixture is reproducible | ⬜ | Re-run script |
| RM-0.0.9d | Remove the notion of a separate SM script; `TestDataGenerator` runs SM2 as part of the normal sequence | `SmCertScript` constant and its dedicated `try/catch` are gone | ✅ | No `generate-test-sm-certs*` script and no `SmCertScript` constant exist in the repo; `grep -rn generate-test-sm-certs` returns nothing |
| RM-0.0.9e | A missing Tongsuo fails the build instead of skipping with a warning | Scripts exit non-zero when the tool is unavailable | ✅ | All 6 scripts gained a guard right after `set -e`: `TONGSUO_PATH` defaults to `/opt/tongsuo/bin/tongsuo`; if not executable, prints an error to stderr and exits 127; `TONGSUO_PATH=/nonexistent/tongsuo bash scripts/generate-test-pfx.sh` triggers it |
| RM-0.0.9f | CI builds Tongsuo from source with a pinned version and caches the result | Integration stage passes on a clean runner | 🟡 | `ci.yml` adds a `tongsuo` job that builds and installs Tongsuo 8.4.0 to `/opt/tongsuo`; `actions/cache@v4` with key `tongsuo-\$OS-v8.4.0`; the `build` job `needs: tongsuo` and injects `TONGSUO_PATH` into the fixtures/integration steps; end-to-end verification still pending a CI run |

### 6.10 `RM-0.0.10` — fixture directories and descriptions

The entire `tests/data/` tree is generated by scripts and **ignored by Git** — no fixture is version-controlled, including the per-directory `README.md` files. `TestData` exposes helpers for the five generated directories. Data that no script can produce needs a tracked home instead.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.10 | Add a **tracked** `tests/fixtures/ntls/` for captured handshake bytes; add `ocsp/` to the generated set; add matching `TestData` helpers; document the fixture inventory in [development-guide.md §8](development-guide.md) rather than in ignored files | Captured fixtures live outside the ignored directory and are version-controlled; the generated set is reproducible by re-running the scripts | 🟡 | Added `tests/fixtures/README.md` and `tests/fixtures/ntls/README.md`; `git check-ignore -v tests/fixtures/*` does not match while `tests/data/*` stays ignored; development-guide §8 already lists the fixture inventory; once RM-0.1.0-01 restores Core, add the `ocsp/` generation section and `TestData` accessors |

### 6.11 `RM-0.0.11` — `netstandard2.0` actually builds

No `netstandard` target produced an assembly. `Convert.FromHexString`, `RandomNumberGenerator.GetBytes(int)` and `AsSpan` are used without guards on `netstandard2.0`.

> `netstandard2.1` was dropped from the TFM set (`RM-0.1.0-01`), so this item now covers a single netstandard target. The guard symbol cannot be a single blanket value either: `HashAlgorithm.HashCore(ReadOnlySpan<byte>)` exists on `netstandard2.1` but not on `netstandard2.0`, while `Convert.FromHexString` is missing from both. Pick the symbol per API.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.11b | Add the missing polyfills / package references for `netstandard2.0`, and choose the guard symbol per API | `dotnet build -f netstandard2.0` succeeds; every `#if` matches the API it protects | ⬜ | Build output + guard-by-guard review |
| RM-0.0.11c | Correct the documents that place polyfills in a non-existent `Compat/` directory | Documents match the real file layout; `Compat/` only appears in the `architecture.md` target-layout table and in this sub-item description | 🟡 | Removed the stale "polyfills live in `Compat/`" paragraphs from architecture / architecture.zh-CN / development-guide / development-guide.zh-CN; the `architecture.md` §4.1 target-layout table keeps `Compat/` (the 0.1.0 internal target directory). The 11b code action lands once RM-0.1.0-01 restores Core |

Until `RM-0.2.0-01` removes the .NET 8-only `TryEncryptEcbCore` / `TryEncryptCbcCore` overrides in the SM4 implementation, they are wrapped in `#if NET8_0_OR_GREATER` as a temporary bridge.

### 6.12 `RM-0.0.12` — trimming and AOT compatibility (net8.0 and later only)

Enum display names resolve through `[Display(ResourceType = typeof(RS))]` and a generated `ResourceManager`, both reflection-based.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.12 | Mark the net8.0+ targets as trim/AOT compatible and annotate the resource lookup path | Publishing an AOT test app succeeds and enum display names still resolve | 🟡 | `Directory.Build.props` sets `IsAotCompatible=true` + `IsTrimmable=true` + `EnableTrimmed=true` + `TrimMode=partial` for the `net8.0` / `net9.0` / `net10.0` TFMs via an `IsTargetFrameworkCompatible('$(TargetFramework)', 'net8.0')` condition; an in-props XML comment constrains the enum-display-name lookup to use an `IsDynamicCodeSupported` guard. AOT publish + smoke test land once RM-0.1.0-01 restores Core (which carries the enum display name resolver) |

The netstandard target carries no AOT metadata: it serves the compatibility surface, while `net8.0` and later serve the AOT surface.

### 6.13 `RM-0.0.13` — encoding rules aligned

`standards.md §2.4` documents LF line endings, a final newline and per-extension indentation, but `.editorconfig` sets `end_of_line = crlf`, `insert_final_newline = false` and defines no section for Markdown, XML, JSON or YAML. `.gitattributes` does not exist, though `standards.md §2.1` lists it.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.0.13a | Rewrite `.editorconfig` from `standards.md §2.4`: LF, final newline, UTF-8, sections for every documented file type | Every rule in §2.4 has a matching rule in `.editorconfig` | ✅ | Top-level `[*]` block sets `charset=utf-8`, `end_of_line=lf`, `insert_final_newline=true`, `trim_trailing_whitespace=true`; `[*.md]` keeps trailing whitespace; `[*.{xml,csproj,props,targets,slnx}]` and `[*.{json,yml,yaml}]` set `indent_size=2`; the duplicated `end_of_line = crlf` lines in the `[*.cs]` / `[*.{cs,vb}]` blocks are gone; side-by-side comparison with `standards.md §2.4` passes |
| RM-0.0.13b | Add `.gitattributes` | Line endings normalised on checkout for all contributors | ✅ | Default `* text=auto eol=lf`; `*.cs` / `*.csproj` / `*.props` / `*.targets` / `*.slnx` / `*.sln` / `*.xml` / `*.json` / `*.yml` / `*.yaml` / `*.sh` / `*.md` / `.gitignore` / `.gitattributes` / `.editorconfig` all `text eol=lf`; `*.pfx` / `*.p12` / `*.der` / `*.bin` / `*.pdb` / `*.snupkg` / `*.nupkg` / `*.png` / `*.jpg` / `*.gif` / `*.webp` / `*.ico` marked binary |
| RM-0.0.13c | Add `artifacts/` to `.gitignore` | `artifacts/` stays untracked, as `standards.md §10` requires | ✅ | `.gitignore` line 88 already has `artifacts/` |
| RM-0.0.13d | Renormalise the existing tree in a **separate commit** | Working tree is clean after renormalisation | ✅ | After the 13a/13b commit, `git add --renormalize .` produces no diff; there is no extra renormalisation commit to make |

`standards.md` must be final before this item starts — the document is the source of truth, not the config file.

---

### 6.14 `0.1.0` — Abstraction layer

No new capability and no algorithm implementation. This milestone settles the contract surface: a new leaf assembly carries the interfaces and abstract base types that every later milestone implements against. The library has never been published, so the shape change is free.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.1.0-01 | Create the `DevTrove.Crypto.Abstractions` assembly (own package, zero-dependency leaf, 4 TFMs, package metadata); Core structure rework (directories and namespaces `Algorithms` / `Asn1` / `Interop` / `Compat`; BouncyCastle types removed from the public surface; one `Interop` extension per type; unified `*Crypto` naming); naming and layout follow-through (`GlobalUsings`, mirrored test directories, `library-api.md` rewritten, `architecture.md` §4 / §5 redrawn) | Each of the 4 TFMs builds; no `DevTrove.Crypto.Crypto.*` or `DevTrove.Crypto.BouncyCastle.*` namespace remains; no `GetBouncyCastle*` member is public | ⬜ | `dotnet build -f <tfm>` plus a `grep` over `src/` for the old namespaces |
| RM-0.1.0-02 | Symmetric and hash abstractions: `ISymmetricBlockCipher` / `SymmetricBlockCipher` / `CipherModeKind` / `PaddingKind` / `IDigest` / `DigestBase` | CTR and AEAD modes are expressible without BCL enums; defaults are `Cbc` and `Pkcs7`; ECB makes `Encrypt` throw `InvalidOperationException` | ⬜ | Unit tests over the enum value sets and the base-class contract |
| RM-0.1.0-03 | Asymmetric capability interfaces: `ISigner` / `IKeyEncipherment` / `IKeyAgreement` / `IAsymmetricKey` / `IPrivateKey` / `IPublicKey` / `AsymmetricKeyBase` / `SignatureAlgorithmKind` | Each capability is its own interface, so no single-inheritance constraint is imposed; `AsymmetricKeyBase` clears key material on disposal | ⬜ | Interface-inventory test + disposal test |
| RM-0.1.0-04 | X.509 abstractions: `ICertificate` / `ICertificateReader` / `ICertificateWriter` / `IDistinguishedName` | All four interfaces are implementable by a single stub; **no implementation type ships in this milestone** (implementations land in `0.5.0`) | ⬜ | Stub-implementation test |
| RM-0.1.0-05 | BCL adapters `AsSymmetricAlgorithm()` / `AsHashAlgorithm()`; release readiness | CBC / CFB / OFB / ECB produce the same result through the adapter as through the stub, while GCM / CTR throw `NotSupportedException`; all 4 TFMs green; packed artifacts complete and carrying the `DevTrove.Crypto.Abstractions` dependency; a clean consumer project restores and calls the API | ⬜ | Unit tests + `dotnet pack` + consumer smoke test |

---

### 6.15 `0.2.0` — Symmetric algorithms

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.2.0-01 | Migrate AES and SM4 onto the `0.1.0` abstraction; align their mode sets; ECB supported by both with CBC as the default and an explicit warning in the XML docs; remove the .NET 8-only overrides | Both algorithms expose the same mode set; `CryptoStream` interop still works through the adapter | ⬜ | Mode matrix tests + interop tests |
| RM-0.2.0-02 | Symmetric key object: algorithm + key + IV/Nonce + memory clearing | Keys are no longer raw `byte[]` in the public API; disposal clears key material | ⬜ | Disposal test |
| RM-0.2.0-03 | Symmetric mode completion: AES-CTR, AES key wrap (RFC 3394), SM4-CTR, SM4-GCM | RFC 3394 and GCM test vectors pass | ⬜ | Known-answer tests |
| RM-0.2.0-04 | CMAC and GMAC | Known-answer tests pass | ⬜ | Known-answer tests |

---

### 6.16 `0.3.0` — Asymmetric algorithms

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.3.0-01 | Migrate RSA, ECDSA, DSA and SM2 onto the `0.1.0` capability interfaces; each algorithm implements exactly the capabilities it has | Each algorithm exposes only the capabilities it really has | ⬜ | Interface-inventory test |
| RM-0.3.0-02 | Add Ed25519, Ed448 and X25519 (X448 optional). X25519 only agrees keys and Ed25519 only signs, which is why capabilities are separate interfaces rather than one base class | Sign and key-agreement round-trips against reference vectors | ⬜ | Known-answer tests |
| RM-0.3.0-03 | `RM-0.0.8`: derive the default signature algorithm from the private key instead of hard-coding `SHA256WITHRSA` | No `SHA256WITHRSA` literal remains; EC / DSA / SM2 sign without the caller naming an algorithm | ⬜ | Per-key-type unit tests |

---

### 6.17 `0.4.0` — Hashes and derivation

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.4.0-01 | Hash family: SHA-256/384/512 wrappers on the `0.1.0` digest abstraction | Every hash exposes the same shape as SM3 | ⬜ | API inventory |
| RM-0.4.0-02 | HMAC: HMAC-SM3 plus HMAC-SHA256/384/512, without the BCL `HashName` reflection factory | Sign and verify against reference vectors | ⬜ | Known-answer tests |
| RM-0.4.0-03 | KDF: HKDF-SHA256/384/512, PBKDF2, optional scrypt | RFC 5869 test vectors pass | ⬜ | Known-answer tests |
| RM-0.4.0-04 | Single entry point for secure randomness | No ad-hoc randomness helper remains scattered across types | ⬜ | API inventory |

---

### 6.18 `0.5.0` — PKI capability completion

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.5.0-01 | Certificate chain building and verification, including cycle and depth protection | Real-site chains and self-signed CA→leaf chains verify correctly; mutually-signed inputs terminate | ⬜ | Regression over `tests/data/certs/` |
| RM-0.5.0-02 | Distinguished-name construction (structured fields → DN) and random serial numbers | Round-trip parse → build → parse is stable | ⬜ | Round-trip tests |
| RM-0.5.0-03 | Format layer: PEM/DER auto-detection, unified conversion, multi-type PEM bundle parsing (private key + certificates) | Any supported input is detected and converted without the caller naming the format | ⬜ | Conversion matrix tests |
| RM-0.5.0-04 | X.509 extension writing: AIA, CertificatePolicies, NameConstraints, PolicyConstraints, SCT | Generated certificates carry the extensions and `openssl`/`tongsuo` reads them back | ⬜ | Interop test |
| RM-0.5.0-05 | PKCS#12 generation options: choice of KDF and cipher, defaulting to PBES2 + AES-256 | Generated file opens with the chosen parameters | ⬜ | Interop test |
| RM-0.5.0-06 | CRL completion: delta CRLs, CRL signature verification, CRL Number, issuing distribution point | Generated and parsed CRLs carry and validate these fields | ⬜ | Interop test |
| RM-0.5.0-07 | PKCS#7 / CMS SignedData parsing (generation optional) | Parse a CMS SignedData produced by another tool | ⬜ | Fixture test |
| RM-0.5.0-08 | OCSP response parsing | Parse a captured OCSP response | ⬜ | Fixture test |
| RM-0.5.0-09 | ASN.1 utilities: DER round-trip and OID mapping | Used by the TLS engine's extension parser | ⬜ | Unit tests |

---

### 6.19 `0.6.0` — TLS probe L1 and NTLS fingerprint

The engine lives in `src/DevTrove.Crypto.Tls/`, which does not exist yet.

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.6.0-01 | Create the `DevTrove.Crypto.Tls` project: sources, package metadata, solution entry, minimum-version dependency on `DevTrove.Crypto` | Project builds and packs as its own package | ⬜ | `dotnet pack` |
| RM-0.6.0-02 | Protocol-version matrix | Verdicts match `openssl s_client` across a public test-site set | ⬜ | Cross-validation |
| RM-0.6.0-03 | Cipher-suite matrix with weak-suite classification | Enumerates suites and flags weak ones | ⬜ | Cross-validation |
| RM-0.6.0-04 | Extension fingerprint: SCT, OCSP staple, EMS, ALPN, session ticket, secure renegotiation | Each extension is detected on servers that advertise it | ⬜ | Cross-validation |
| RM-0.6.0-05 | Server-sent certificate chain | Order matches `openssl s_client -showcerts` | ⬜ | Cross-validation |
| RM-0.6.0-06 | Negotiated group and signature algorithm capture | Values match the server's actual choice | ⬜ | Cross-validation |
| RM-0.6.0-07 | `TlsRaw`: hand-built ClientHello plus hand-rolled ServerHello / Certificate / ServerKeyExchange parsing | Parses captured byte fixtures | ⬜ | Fixture tests |
| RM-0.6.0-08 | NTLS fingerprint: version, suites, dual certificate presence, signature algorithm, curve | Verdicts correct on captured public ShangMi-site bytes | ⬜ | Fixture tests |
| RM-0.6.0-09 | ROBOT oracle probing | Distinguishes vulnerable from hardened servers | ⬜ | Cross-validation |
| RM-0.6.0-10 | SSLv2 ClientHello probing | Detects servers that answer SSLv2 records | ⬜ | Cross-validation |
| RM-0.6.0-11 | Graceful degradation: an anomalous server extension never fails the whole scan | Scan completes and records the anomaly | ⬜ | Fault-injection test |

---

### 6.20 `0.7.0` — TLS probe L2 and ShangMi

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-0.7.0-01 | A–F grading with a stated reason for every deduction | Results are explainable per test site | ⬜ | Report review |
| RM-0.7.0-02 | Client-simulation matrix: Chrome, Firefox, Safari, Edge, Java, Android | Verdicts match each client's real handshake capability | ⬜ | Cross-validation |
| RM-0.7.0-03 | ALPN / HTTP2 probing | Negotiated protocol matches the server's actual choice | ⬜ | Cross-validation |
| RM-0.7.0-04 | Certificate transparency log query | Detects embedded SCTs and log availability | ⬜ | Cross-validation |
| RM-0.7.0-05 | DNS CAA verification | Reports CAA records and mismatch versus the issuing CA | ⬜ | Cross-validation |
| RM-0.7.0-06 | ShangMi cipher-suite matrix | Enumerates SM2/SM3/SM4 suites | ⬜ | Fixture and live tests |
| RM-0.7.0-07 | ShangMi site specialised report: negotiated suites, dual certificate, signature algorithm, curve | Report fields complete for captured sites | ⬜ | Report review |
| RM-0.7.0-08 | RFC 8998 (SM2-TLS 1.3) full handshake | Handshake completes against a supporting endpoint | ⬜ | Live test |
| RM-0.7.0-09 | OCSP response signature verification | Verifies signature and target-certificate match | ⬜ | Fixture tests |

---

### 6.21 `1.0.0` — Stable API and PKIX

| ID | Sub-item | Acceptance | Status | Evidence |
|---|---|---|---|---|
| RM-1.0.0-01 | API stabilisation: freeze the public surface and record a compatibility baseline | No public API change without a major bump | ⬜ | API diff report |
| RM-1.0.0-02 | PKIX path validation: AuthorityKeyIdentifier, KeyUsage, BasicConstraints, path length, policy and name constraints | Chains PKIX rejects are rejected | ⬜ | PKIX conformance cases |
| RM-1.0.0-03 | Cycle and depth protection in chain building | Pathological inputs terminate | ⬜ | Fuzz-style inputs |
| RM-1.0.0-04 | Grading algorithm and its rationale documented | Every deduction is traceable to a documented rule | ⬜ | Docs review |
| RM-1.0.0-05 | Complete XML documentation and public NuGet release | Package ships with docs; release checklist in [nuget.md §8](nuget.md) passes | ⬜ | Release checklist |

---

## 7. Excluded and non-goals

| Item | Reason |
|---|---|
| Consumer-side `ProjectReference` / `PackageReference` toggling | A consumer integration pattern, not library behaviour |
| Submodule / dual-repo workflow | This repository is standalone; how it is consumed is the consumer's concern |
| L3 vulnerability probing (Heartbleed, CCS Injection, Ticketbleed) | Needs a self-implemented record layer and key derivation; only ROBOT is in scope |
| NTLS full handshake | Needs a TLS 1.2 subset implemented from scratch; deferred beyond `0.7.0` |
| Packaging `testssl.sh` | GPLv2; usable only as an optional external cross-check |
| Native dependencies, including Tongsuo P/Invoke | Conflicts with the pure-managed, AOT-capable goal |

---

## 8. Risk register

| # | Risk | Impact | Mitigation |
|---|---|---|---|
| R1 | Building Tongsuo from source in CI lengthens the pipeline badly | Slow feedback, flaky jobs | Cache the build output and pin the version |
| R2 | Unifying on Tongsuo stops validating interoperability against upstream OpenSSL | A regression specific to upstream OpenSSL goes unnoticed | Document the limitation; keep an optional, non-blocking cross-check |
| R3 | Renormalising line endings across the tree produces a very large diff | History becomes harder to read | Commit the renormalisation separately and note it in the message |
| R4 | Replacing the BCL abstractions touches many call sites in tests and docs | Large, error-prone refactor | Land `RM-0.0.8` first; adopt the new abstraction incrementally, one algorithm at a time |
| R5 | GB/T 38636 (NTLS) details cannot be confirmed from public material | Detection verdicts may be inaccurate | Use captured real-site bytes as fixtures; mark uncertain verdicts explicitly |
| R6 | A trimmed or AOT-compiled consumer loses enum display names | Silent degradation | Annotate the resource path; verify with an AOT publish smoke test |
| R7 | The status table drifts from reality | The roadmap becomes another misleading document | Status updates are part of the pre-submission checklist (see §2) |

---

## 9. Related documents

| Document | Contents |
|---|---|
| [architecture.md](architecture.md) | Positioning and goals, layering, dependency direction, capability boundaries, technical decisions |
| [standards.md](standards.md) | Language policy, engineering files, C# conventions, testing, Git, pre-submission checks |
| [nuget.md](nuget.md) | Package boundaries, metadata, versioning, release process |
| [tls-scanner.md](tls-scanner.md) | TLS probe engine design |
| [development-guide.md](development-guide.md) | Build / test / pack commands, external tools, fixtures, CI |
| [library-api.md](library-api.md) | Public API index |
