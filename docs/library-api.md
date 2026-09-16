# Library API index

> 中文对照：[library-api.zh-CN.md](library-api.zh-CN.md)

Public API index for `DevTrove.Crypto`. Generated manually from `src/**` — each entry names the namespace, type and a one-line description. **Namespace root is `DevTrove.Crypto`** for everything; the `DevTrove.Crypto.Core` project flattens namespaces so that consumers do not depend on the `Core` project name.

> **Sections 2–9 describe code that is being rebuilt.** `DevTrove.Crypto.Core` currently holds no source: the Web-era implementation was removed and is being reconstructed against the contracts in §1. Those sections are kept as the target index and are not a claim that the types exist today.

> **Contracts are the exception** — §1 names the assembly that `0.1.0` actually delivers.

---

## 1. Contracts (`DevTrove.Crypto.Abstractions`)

A **separate assembly** with **no package dependencies at all** — not even BouncyCastle. Consumers can compile against this surface alone. Directory layout is flat, so each namespace maps onto one directory: `Symmetric/`, `Asymmetric/`, `Hash/`, `X509/`.

> `RM-0.1.0-01`–`-05` deliver this assembly in `0.1.0`. BCL adapters sit next to the family they adapt rather than in a separate `Interop/` directory.

### 1.1 Symmetric (`DevTrove.Crypto.Abstractions.Symmetric`)

| Type | Kind | Purpose |
|---|---|---|
| `CipherModeKind` | enum | `Cbc` / `Cfb` / `Ofb` / `Ctr` / `Ecb` / `Gcm` — expresses CTR and AEAD, which the BCL's closed `CipherMode` enum cannot |
| `PaddingKind` | enum | `None` / `Pkcs7` / `Zeros` / `AnsiX923` |
| `ISymmetricBlockCipher` | interface | Block-size, key-size, mode, padding, nonce and tag sizes; `Encrypt` / `Decrypt` |
| `SymmetricBlockCipher` | abstract class | Mode dispatch and padding; defaults `Cbc` + `Pkcs7`; **ECB makes `Encrypt` throw `InvalidOperationException`**; `Init` / `EncryptBlock` / `DecryptBlock` are left to the concrete algorithm |
| `SymmetricBlockCipherInteropExtensions` | static class | `AsSymmetricAlgorithm()`. CBC / CFB / OFB / ECB bridge to the BCL; **GCM and CTR throw `NotSupportedException`** |

### 1.2 Hash (`DevTrove.Crypto.Abstractions.Hash`)

| Type | Kind | Purpose |
|---|---|---|
| `IDigest` | interface | `DigestSize` / `BlockSize` / `Reset()` / `Update(ReadOnlySpan<byte>)` / `Digest()` / `ComputeHash(ReadOnlySpan<byte>)` |
| `DigestBase` | abstract class | Buffer management plus a one-shot `ComputeHash` built on `Update` + `Digest` |
| `DigestInteropExtensions` | static class | `AsHashAlgorithm()` bridges to the BCL. On `netstandard2.0` the guard falls back to `HashCore(byte[], int, int)` |

### 1.3 Asymmetric (`DevTrove.Crypto.Abstractions.Asymmetric`)

| Type | Kind | Purpose |
|---|---|---|
| `ISigner` | interface | Sign / verify |
| `IKeyEncipherment` | interface | Encrypt / decrypt |
| `IKeyAgreement` | interface | Derive a shared secret (raw bytes; KDF is the caller's job) |
| `IAsymmetricKey` | interface | Key metadata: algorithm and size |
| `IPrivateKey` / `IPublicKey` | interface | Read-only key views |
| `AsymmetricKeyBase` | abstract class | Holds key material and **clears it on disposal** |
| `SignatureAlgorithmKind` | enum | The signature algorithms, so a default can be derived from the private key instead of hard-coding `SHA256WITHRSA` (`RM-0.3.0-03`) |

Capabilities are separate interfaces rather than one base class because X25519 only agrees keys and Ed25519 only signs.

### 1.4 X.509 (`DevTrove.Crypto.Abstractions.X509`)

| Type | Kind | Purpose |
|---|---|---|
| `ICertificate` | interface | Read-only certificate view: subject, issuer, serial, validity, encoded form |
| `ICertificateReader` | interface | Parse one or many certificates |
| `ICertificateWriter` | interface | Emit a certificate |
| `IDistinguishedName` | interface | Structured access to DN components |

**No implementation type ships in `0.1.0`.** These interfaces are declared now and implemented in `0.5.0`; the contract tests prove they are implementable by a stub.

---

## 2. Algorithm primitives (`DevTrove.Crypto.Crypto`)

### 2.1 Asymmetric keys

| Type | Purpose |
|---|---|
| `AsymmetricKeyPair` | RSA / EC / SM2 / DSA key-pair generation; derive public from private |
| `AsymmetricKeyParameter` | Base for private/public key parameters; PEM / DER import-export base |
| `AsymmetricPrivateKeyParameter` | Private-key holder; encrypt / decrypt / sign; derives public key |
| `AsymmetricPublicKeyParameter` | Public-key holder; verify / encrypt (where applicable) |
| `PasswordFinder` | `IPasswordFinder` adapter for decrypting encrypted PEM |

### 2.2 Algorithm wrappers

| Type | Purpose |
|---|---|
| `RsaCrypto` | RSA encrypt / decrypt (OAEP-SHA256 default, PKCS#1 v1.5 optional); sign / verify (PSS default, PKCS#1 v1.5 optional) |
| `EcdsaCrypto` | ECDSA sign / verify (DER encoded); ECDH shared secret (same-curve validation) |
| `DsaCrypto` | DSA sign / verify (DER encoded); key lengths 1024 / 2048 / 3072 |
| `AesCrypto` | AES-CBC / CFB / OFB (auto IV) + AES-GCM (12-byte nonce + 16-byte tag). ECB is supported for interoperability but CBC is the default and ECB is documented as insecure |

### 2.3 ShangMi (`DevTrove.Crypto.Crypto.Sm`)

| Type | Purpose |
|---|---|
| `SM2` | Sign / verify, encrypt / decrypt, key exchange (curve `sm2p256v1`) |
| `SM3` | Hash (256-bit output) |
| `SM4` | Block cipher (CBC / CTR / GCM) |

---

## 3. X.509 (`DevTrove.Crypto.X509`)

### 3.1 Top-level types

| Type | Purpose |
|---|---|
| `Certificate` | Parse + generate (self-signed, sign-CSR, sign-public-key); extensions, fingerprints, validity, key usage |
| `CertificateSigningRequest` | Generate (with `X509ExtensionOptions`) + parse + verify signature |
| `CertificateRevocationList` | Generate (with revocation reasons) + parse + `IsRevoked` |
| `X509ExtensionBuilder` | Apply `X509ExtensionOptions` to a certificate generator or assemble a CSR extension set |

### 3.2 Models (`DevTrove.Crypto.X509.Models`)

| Type | Purpose |
|---|---|
| `X509DistinguishedName` | DN parse / access (RFC 2253 + OpenSSL slash style) |
| `GeneralName` | SAN entry (DNS / IP / URI / email / ...) |
| `X509ExtensionOptions` | Declarative container for KU / EKU / SAN / BasicConstraints / SKI / AKI / CRL DP |
| `BasicConstraintsOptions` | CA / path-length constraint parameters |
| `CertificateExtension` | Generic extension value descriptor |
| `PfxBundle` | PFX / PKCS#12 container (cert + chain + private key) |
| `RevokedCertificateInfo` | CRL entry |

### 3.3 Enums (`DevTrove.Crypto.X509.Enums`)

| Type | Purpose |
|---|---|
| `KeyUsage` | `[Flags]` per RFC 5280 §4.2.1.3 |
| `ExtendedKeyUsage` | Server / Client auth, Code signing, Email, etc. |
| `GeneralNameType` | DNS / IP / URI / email / ... |
| `CertificatePolicy` | OID-based policy identifiers |
| `CertificateRevocationReason` | RFC 5280 revocation reasons |

Each enum ships with `*Extensions.cs` (extension methods) and a `*Helper.cs` (`X509/Extensions/`) for resource-string display + parsing.

### 3.4 Utilities (`DevTrove.Crypto.X509.Utils`)

| Type | Purpose |
|---|---|
| `CertificateUtils` | Common helpers (parsing, format detection, etc.) |
| `PfxUtils` | `ToPfx` / `FromPfx` for PKCS#12 import-export |
| `X509NameParser` | DN parser (RFC 2253 + OpenSSL slash style) |

### 3.5 Extensions (`DevTrove.Crypto.X509.Extensions`)

| Type | Purpose |
|---|---|
| `X509ExtensionBuilder` | (also listed in §2.1; the implementation lives here) |
| `FingerprintHelper` | SHA-1 / SHA-256 / SHA-384 / SHA-512 / MD5 / SM3 thumbprint |
| `KeyUsageHelper` / `ExtendedKeyUsageHelper` / `CertificatePolicyHelper` / `CertificateRevocationReasonHelper` / `GeneralNameTypeHelper` | Enum ↔ string / resource-string helpers |

---

## 4. BouncyCastle interop (`DevTrove.Crypto.Asn1`, `DevTrove.Crypto.Interop`)

| Type | Purpose |
|---|---|
| `CertificationRequestInfoExtensions` | Extension methods on BouncyCastle `CertificationRequestInfo` |
| `CertificatePolicyObjectIdentifiers` | OID constants for certificate-policy extension |
| `ExtendedKeyUsageObjectIdentifiers` | OID constants for extended-key-usage extension |

These wrap BouncyCastle types so that `Core` can keep them out of its own public surface — see decision D21. Interop goes through the explicit extensions in `Interop/`; there is no `GetBouncyCastle*` member.

---

## 5. Helpers (`DevTrove.Crypto.Common`, `DevTrove.Crypto.Extensions`)

| Type | Purpose |
|---|---|
| `EnumDisplayNameCache<TEnum>` | Cached resource-string lookup for enum display |
| `ArgumentNullExceptionExtensions` | `ThrowIfNull(...)` convenience overloads |

### 5.1 Signing-algorithm default rule (`RM-0.3.0-03`)

Any public signing method that accepts an optional `signatureAlgorithm` (e.g. `Certificate.GenerateSelfSigned`, `Certificate.SignCsr`, `Certificate.SignPublicKey`, `CertificateRevocationList.Generate`, `CertificateSigningRequest.Generate` overloads) **must derive the default from the private-key algorithm**, never hard-code `SHA256WITHRSA` or any other fixed OID:

| Private-key algorithm | Default signature algorithm |
|---|---|
| RSA | `SHA256WITHRSA` |
| ECDSA (P-256 / P-384 / P-521) | `SHA256WITHECDSA` / `SHA384WITHECDSA` / `SHA512WITHECDSA` |
| DSA | `SHA256WITHDSA` |
| SM2 (curve `sm2p256v1`) | `SM3WITHSM2` |
| Ed25519 / Ed448 | `Ed25519` / `Ed448` (intrinsic) |

Callers must remain able to override explicitly; the default only exists so that EC / DSA / SM2 / Ed25519 callers do not have to special-case it.

---

## 6. Resource files

Resource files are not shipped in the `0.1.x` restructuring. Enum display names go through the trimmed/AOT-aware resolver added by `RM-0.0.12`.

---

## 7. Reserved namespace — `DevTrove.Crypto.Tls`

The namespace is reserved for the TLS probe engine. No public types exist today; the package is scheduled for `0.6.0` ([roadmap.md](roadmap.md) §6.19). The list below is a **design target**, not an API commitment.

| Planned entity (per [tls-scanner.md §12](tls-scanner.md)) | Purpose |
|---|---|
| `TlsScanReport` | Top-level scan report |
| `ProtocolMatrix` | Per-version support + negotiation result |
| `CipherSuiteMatrix` | Per-suite support + grouping |
| `ExtensionFingerprint` | Server extension set + per-item verdict |
| `CertificateChainInfo` | Server-sent chain order + per-cert parse result |
| `OcspStaplingInfo` | Stapling presence + response parse |
| `NtlsFingerprint` | NTLS verdicts (version, suite, dual cert) |
| `GradeResult` | Grade + deduction list + reasons |
| `ClientSimulationMatrix` | Per-client simulation result |
| `ProbeDiagnostics` | Exceptions + downgrade records (**must be exposed**) |

These are **planned, not implemented**. Any code claiming to reference them today is wrong.

---

## 8. Consuming the API

Consumers reference the metapackage:

```xml
<PackageReference Include="DevTrove.Crypto" Version="[0.1.0, )" />
```

The metapackage transitively pulls `DevTrove.Crypto.Core` and `DevTrove.Crypto.Abstractions`. Use the namespaces:

```csharp
using DevTrove.Crypto;
using DevTrove.Crypto.Abstractions.Symmetric;
using DevTrove.Crypto.Abstractions.Asymmetric;
using DevTrove.Crypto.Abstractions.Hash;
using DevTrove.Crypto.Abstractions.X509;
```

A consumer that wants the contract surface **without** the BouncyCastle-backed implementation references `DevTrove.Crypto.Abstractions` directly — it is the one package here that depends on nothing.

For more on packaging and version constraints see [nuget.md](nuget.md).

---

## 9. Maintenance

This index is hand-maintained. When a new public type is added:

1. Add it to the matching section above.
2. Add a Chinese line in [library-api.zh-CN.md](library-api.zh-CN.md) at the matching section (sections, tables and descriptions stay one-to-one).
3. Verify with a `grep` for public type declarations across `src/**` — every match must appear in this index.

> The metapackage contributes no types by design, so it never appears in the `grep` result.
