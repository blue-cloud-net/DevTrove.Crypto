# Library roadmap

> 中文对照：[roadmap.zh-CN.md](roadmap.zh-CN.md)

This document is the library's own phase plan. It is independent from the consuming application repo's roadmap. Cross-repo tasks appear here only when they require work inside this library.

---

## 1. Version strategy

Follow SemVer. `0.x` allows breaking changes; `1.0.0` is strict.

| Version | Phase | Theme |
|---|---|---|
| `0.0.1-dev` | — | Bootstrap; TFM fix (B1), metapackage cleanup (B2) |
| `0.0.X-dev` | Phase A continuation | Capability iteration |
| `0.1.0` | Phase A complete | 5-TFM, metapackage is source-free, OCSP parse, RFC 8998 verification |
| `0.2.0` | Phase 3 | TLS probe L1 + NTLS fingerprint |
| `0.3.0` | Phase 4 | TLS probe L2 + ShangMi |
| `1.0.0` | Phase 5 | Stable API + grading documented |

The library is **independently versioned**; it does not align with the consuming application's version. Consumers declare a minimum compatible version (see [nuget.md §3.2](nuget.md)).

---

## 2. Capability status

### Phase A — Core library transformation

**Goal**: transform this repository from an application-style repo into a **pure-library** repo that can be published to NuGet independently.

**Starting state**: the repository contained `Crypto.Utils.Core` (BC wrapper, kept), `Crypto.Utils.Api`, `Crypto.Utils.Host`, `Crypto.Utils.UI` (all three to removed).

**Target form**: a **single submodule** `lib/Crypto` mounted in the application repo; `src/` layering inside, three projects for three NuGet packages — `DevTrove.Crypto` (metapackage), `DevTrove.Crypto.Core` (implementation), `DevTrove.Crypto.Tls` (TLS probe engine, scaffolded in Phase A, implemented from Phase 3). All projects / assemblies / namespaces renamed from `Crypto.Utils.*` to `DevTrove.Crypto.*`.

### Tasks

| # | Task | Depends on |
|---|---|---|
| A1 | Delete `src/Crypto.Utils.{Api,Host,UI}` + related NuGet references + slnx entries | — |
| A2 | **Migrate 7 logic items into Core**, fix 4 known defects (below) | A1 |
| A3 | Add metapackage project (`DevTrove.Crypto`, stable public API; `Core` becomes internal impl) | A2 |
| A4 | Add NuGet metadata + `GenerateDocumentationFile` + `global.json` + CI; unify TFM; tests gain `net9.0` | A3 |
| A5 | Clean dead code; SM2 fixtures committed; CI also generates PFX fixtures | A1 |
| A6 | Docs rewrite to library mode + bilingual README/CHANGELOG + `docs/library-api.md` | A1, A2 |
| A7 | Add unit tests; `dotnet pack` artifact validation | A2, A4 |

### Items that must migrate before deleting Api

| Migrated content | Target location |
|---|---|
| Certificate chain build + verify | `CertificateChainBuilder` / `CertificateChainVerifier` |
| DN string builder (structured fields → DN) | `X509DistinguishedName` gains a build direction |
| Random serial number generation | `CertificateUtils.GenerateSerialNumber()` |
| Data-type auto-detect + PEM/DER unified conversion | `FormatUtils` |
| Revocation reason string → enum parsing | `CertificateRevocationReasonHelper` |
| Certificate verification (validity + signature + chain + trust root) | Merged with chain verification |
| **OCSP response parsing** (currently missing) | `X509.Ocsp` (needed by `DevTrove.Crypto.Tls`) |

### Defects to fix while migrating

| # | Defect | Fix |
|---|---|---|
| 1 | CSR generation hand-rolls "private → public" derivation; **missing DSA branch**, EC branch lacks `.Normalize()` | Use `AsymmetricPrivateKeyParameter.GetPublicKey()` (RSA/EC/DSA branches complete) |
| 2 | Format conversion wraps an async call in `Task.Run` with `.Result` | Switch to sync implementation; remove blocking / deadlock antipattern |
| 3 | Default signature algorithm hard-coded to RSA (`SHA256WITHRSA`) | Derive default from private-key algorithm |

### Acceptance criteria

- [ ] 5 target frameworks (`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`) all build (currently failing — see B1)
- [ ] `dotnet test` green (including `net9.0` once added — see B11)
- [ ] `dotnet pack` output contains XML docs + complete metadata
- [ ] In a clean `netstandard2.0` empty project, restore the package and call API successfully (after B3 polyfill lands)
- [ ] `LICENSE` (Apache-2.0) matches `PackageLicenseExpression`
- [ ] Repo no longer references Tongsuo (currently still does — see B4)
- [ ] Docs match real capability (remove claims for un-implemented things)

### Risks

| Risk | Mitigation |
|---|---|
| Restoring `netstandard2.0` may break conditional-compiled files (uses C# 14 `extension(...)` syntax) | Probe standalone first in A4; remove the file if it doesn't compile |
| Chain-verification migration behavior diverges from Api layer | Use real site chains + self-signed CA→leaf chains as regression |
| Doc drift (original docs claim JWT/hash/online-detection etc. — none of which exists) | A6 audits line-by-line and removes the false claims |

---

## 3. Phase 0 (parent repo) — WASM verification + main-repo skeleton

**Goal**: validate WebAssembly feasibility, build the main-repo code skeleton, get the minimum loop working.

The library's role in this phase:

| # | Task | Owner |
|---|---|---|
| 0 | WASM feasibility verification (parent decides) | parent |
| 1 | Main-repo skeleton: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.slnx` | parent |
| 2 | Inside `lib/Crypto`: scaffold `src/DevTrove.Crypto.Tls` (referencing `DevTrove.Crypto`) | **library** |
| 3 | Minimum loop: tool abstractions + sample tool (Base64) + `DevTrove.Services` assembly + Host exposure + Web.Client rendering + Desktop in-process | parent |

### Library task #2 detail

- Create `src/DevTrove.Crypto.Tls/` project: `ProjectReference` → `DevTrove.Crypto.Core`; namespace `DevTrove.Crypto.Tls.*`.
- `DevTrove.Crypto.Tls` declares a dependency on `DevTrove.Crypto` with a minimum version (see [nuget.md §3.2](nuget.md)).
- `Directory.Build.props` picks up the new project; the slnx includes it.
- Initially empty of source: only the project skeleton + public types from [architecture.md §12](architecture.md) as interface definitions.

---

## 4. Phase 1–2 (parent repo) — General tools, crypto/cert tools

**Library's role**: no changes. The Core implementation is consumed by the application's `DevTrove.Core` / `DevTrove.Services`.

Library-side items that may surface during this phase (each tracked here):

| Item | Trigger |
|---|---|
| OCSP parse edge cases reported by crypto tools | Add to roadmap if any test fails |
| DN builder direction extension | Phase A2 — verify behavior matches expected |
| Format-detection helpers (PEM/DER/...) | Phase A2 — verify with parent-side samples |

---

## 5. Phase 3 — TLS probe L1 + NTLS detection

**Goal**: deliver TLS probe L1 and ShangMi NTLS fingerprint.

| # | Task | Owner |
|---|---|---|
| 1 | `DevTrove.Crypto.Tls/TlsProbe`: protocol matrix, cipher-suite matrix, extension fingerprint, server-sent cert chain, negotiated group/signature algorithms | **library** |
| 2 | `DevTrove.Crypto.Tls/TlsRaw`: raw-byte path — NTLS fingerprint detection, ROBOT oracle, SSLv2 ClientHello | **library** |
| 3 | `INetworkProbeService` in-process + HTTP implementations + `Tools/Network` | parent |
| 4 | Host security baseline: rate limiting, SSRF, body-size limit, timeout, CORS | parent |

### Acceptance

- [ ] Protocol matrix verdict for the `badssl.com` family matches `openssl s_client`
- [ ] Cipher-suite matrix can enumerate and identify weak suites
- [ ] Extension fingerprint recognizes SCT, OCSP staple, EMS, ALPN, session ticket, secure renegotiation
- [ ] Server-sent certificate-chain order matches `openssl s_client -showcerts`
- [ ] SSRF cases (`127.0.0.1`, `10.x`, `169.254.169.254` cloud metadata, DNS-rebinding domains) all rejected (parent)
- [ ] NTLS fingerprint detection verdicts are correct on captured public ShangMi-site byte fixtures
- [ ] Server-side extension anomalies don't fail the whole scan (downgraded to "record the exception")

---

## 6. Phase 4 — TLS probe L2 + ShangMi

**Goal**: deliver grading, client simulation, and ShangMi TLS full capability.

| # | Task | Owner |
|---|---|---|
| 1 | A–F grading algorithm (reference public approaches, implement independently) | **library** |
| 2 | Client-simulation matrix (Chrome / Firefox / Safari / Edge / Java / Android) | **library** |
| 3 | ALPN/HTTP2 probing, CT log query, DNS CAA verification | mixed |
| 4 | RFC 8998 (SM2-TLS 1.3) full handshake (depends on V3 verification) | **library** |
| 5 | ShangMi suite matrix + ShangMi site specialized report | **library** |

### Acceptance

- [ ] Grading result is reasonable and explainable (each deduction has a reason)
- [ ] Client-simulation verdicts match each browser's actual handshake capability
- [ ] RFC 8998 full handshake succeeds (if V3 says feasible)
- [ ] ShangMi site report lists negotiated suites, dual certificate, signature algorithm, curve

### Deferred to v2 evaluation

NTLS **full handshake** self-implementation: requires record layer + SM3 PRF + SM2 key exchange + SM4 record protection + dual-cert handling + Finished validation. Only kick off when there is a concrete "simulate ShangMi client" demand.

---

## 7. Pending / known deviations

These are the gaps surfaced while drafting this roadmap; each has a concrete next step.

| # | What docs / claims say | Actual state | Next step |
|---|---|---|---|
| B1 | 5 TFM all build (`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`) | `Directory.Build.props` sets 5 TFM globally; Core csproj overrides to `net8.0;net9.0;net10.0`; metapackage `net10.0` → `DevTrove.Crypto.slnx` build reports **NU1201** | Unify csproj `<TargetFrameworks>` with the global set; verify each TFM individually |
| B2 | Metapackage is source-free | `src/DevTrove.Crypto/Program.cs` still exists | Remove the file; ensure the metapackage project has no source files (only `ProjectReference`) |
| B3 | `netstandard2.0` missing APIs polyfilled in `Compat/` | `src/DevTrove.Crypto.Core/Compat/` **does not exist** (README en/zh both claim it does) | Decide: drop the `netstandard2.0` target, or actually create `Compat/` with the required polyfills |
| B4 | Removed national-crypto CLI dependency | `tests/DevTrove.Crypto.TestSupport/TongsuoCli.cs` still exists; `tests/data/*/README.md` references tongsuo and the **non-existent** `scripts/generate-test-sm-certs.sh` | Either restore the SM2 fixtures via OpenSSL (drop `TongsuoCli` + scripts reference) and update the data READMEs, or keep `TongsuoCli` and add the missing script |
| B5 | `tests/data/` already has `ocsp/`, `ntls/` fixtures | Reality: only `certs/ crls/ csrs/ keys/ pfx/` exist | Add the empty dirs and the script to capture fixtures on first run, or remove the claim |
| B6 | Dev-time `ProjectReference` / pack-time `PackageReference` toggled by `Directory.Build.props` conditional property | The conditional property is **not present** anywhere | Introduce the property (`<UseCryptoProjectRef>true/false</UseCryptoProjectRef>` etc.) and switch the consumer's csproj accordingly; document in [development-guide.md §4](development-guide.md) |
| B7 | CI publish workflow | `build.yml` publish job runs `dotnet pack ... --no-build` with no preceding `dotnet build`; the push glob `DevTrove.Crypto.*.nupkg` double-matches the Core package | Add a `dotnet build` step in publish; change the metapackage push glob to `DevTrove.Crypto.*[!Core]*.nupkg` or push by exact filename |
| B8 | `docs/library-api.md`, `docs/standards.md` exist | They do not (README/AGENTS already link them) | Created in this revision (Phase 1g, 1b) |
| B9 | "Single submodule `lib/Crypto`" | No `.gitmodules`; the parent repo's `.gitignore` still ignores `lib/Crypto/` (parent-side Stage 0) | Document the Stage 0 state honestly; actual `git submodule add` is the parent's decision (out of scope for this library) |
| B10 | Sub-repo docs in library mode | `docs/{architecture,development-guide,roadmap}.md` + `v0.1/*` are still Web-era content (mention `Crypto.Utils.UI/Api/Host`) | Rewritten in this revision (Phase 1a, 1f, 1e); `v0.1/` deleted (Phase 1h) |
| B11 | Tests already cover `net9.0`; xUnit v3 in use | TestSupport csproj lacks `net9.0`; xUnit still 2.9.2 | Add `net9.0` to TestSupport; plan xUnit v3 upgrade (impacts assertion style) |
| B-cmnt | Code comments / XML comments language policy | Currently Chinese; i18n not done | Internationalization is a roadmap item — once consumers push, switch to English (or bilingual) and ship a major-version bump |

---

## 8. Out of scope

| Item | Reason |
|---|---|
| User account / login / permissions | No server-side state by design |
| Server-side persistence (database, history) | Privacy first; never persist user input |
| Payment / monetization | Not in this phase |
| Native dependency packages (Tongsuo P/Invoke) | Conflicts with pure-managed goal; not usable in WebAssembly |
| L3 vulnerability probing (Heartbleed / CCS Injection / Ticketbleed etc.) | Requires self-implemented record layer + key derivation; only ROBOT is in scope |
| NTLS full handshake | Requires implementing a TLS 1.2 subset from scratch; deferred to v2 evaluation |
| Packaging `testssl.sh` | GPLv2; only as an optional external cross-check tool |

---

## 9. Risk register

| # | Risk | Impact | Mitigation |
|---|---|---|---|
| R1 | BouncyCastle capabilities in WebAssembly are partially unavailable | Local-compute limits; some tools must fall back to server-side | Verify early (parent's Phase 0 V1/V2); switch affected tools to `ServerProxy` with explicit messaging |
| R2 | Cold-start size exceeds expectation (>2MB gzip) | First-visit UX suffers | Lazy assembly + trim + Brotli + PWA; re-evaluate hybrid rendering only after measurement |
| R3 | Server-side probe interface abuse | Resource exhaustion; used as attack pivot | Rate limiting + SSRF + size limit + timeout + stateless design (parent) |
| R4 | GB/T 38636 national-standard details unverified | Detection verdict may be inaccurate | Use public ShangMi-site real bytes as fixtures; explicitly mark unverifiable items |
| R5 | Certificate-chain verification simplified implementation mistaken for complete | Users underestimate risk | UI and docs explicitly mark "simplified"; PKIX upgrade on roadmap |
| R6 | Two front ends cause behavior divergence | Experience split | Schema-driven + shared Core; one smoke test per end; no business logic in UI |
| R7 | Library result models vs consumer contract models | Mismatch risk | Library ships its own models; consumer maps explicitly; [tls-scanner.md §12](tls-scanner.md) lists the entities |

---

## 10. Related

| Document | Contents |
|---|---|
| [architecture.md](architecture.md) | Layering, dependency direction, capability boundaries |
| [standards.md](standards.md) | Coding / testing / Git conventions |
| [nuget.md](nuget.md) | Package boundaries, versioning, release process |
| [tls-scanner.md](tls-scanner.md) | TLS probe engine design |
| [development-guide.md](development-guide.md) | Build / test / pack / CI |
| [library-api.md](library-api.md) | Public API index |
