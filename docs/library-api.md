# Library API index

> 中文对照：[library-api.zh-CN.md](library-api.zh-CN.md)

Public API index for `DevTrove.Crypto`. Generated manually from `src/DevTrove.Crypto.Core/**` — each entry names the namespace, type and a one-line description. **Namespace root is `DevTrove.Crypto`** for everything; the `DevTrove.Crypto.Core` project flattens namespaces so that consumers do not depend on the `Core` project name.

> **Known deviation** (`RM-0.0.2`): the metapackage project at `src/DevTrove.Crypto/` still ships a `Program.cs` and is not source-free. Once that file is gone, this index applies unchanged.

> **Planned restructure** (`RM-0.1.0-01`): namespaces change in `0.1.0`. `DevTrove.Crypto.Crypto.*` becomes `DevTrove.Crypto.Algorithms.*`, `DevTrove.Crypto.BouncyCastle.*` becomes `DevTrove.Crypto.Asn1`, algorithm proxies are renamed to `<Algorithm>Crypto` (`Sm2Crypto`, `Sm3Crypto`, `Sm4Crypto`, …), and BouncyCastle types leave the public surface. Section names below follow the code as it exists today — the target layout is in [architecture.md §4.1](architecture.md).

---

## 1. Algorithm primitives (`DevTrove.Crypto.Crypto`)

### 1.1 Asymmetric keys

| Type | Purpose |
|---|---|
| `AsymmetricKeyPair` | RSA / EC / SM2 / DSA key-pair generation; derive public from private |
| `AsymmetricKeyParameter` | Base for private/public key parameters; PEM / DER import-export base |
| `AsymmetricPrivateKeyParameter` | Private-key holder; encrypt / decrypt / sign; derives public key |
| `AsymmetricPublicKeyParameter` | Public-key holder; verify / encrypt (where applicable) |
| `PasswordFinder` | `IPasswordFinder` adapter for decrypting encrypted PEM |

### 1.2 Algorithm wrappers

| Type | Purpose |
|---|---|
| `RsaCrypto` | RSA encrypt / decrypt (OAEP-SHA256 default, PKCS#1 v1.5 optional); sign / verify (PSS default, PKCS#1 v1.5 optional) |
| `EcdsaCrypto` | ECDSA sign / verify (DER encoded); ECDH shared secret (same-curve validation) |
| `DsaCrypto` | DSA sign / verify (DER encoded); key lengths 1024 / 2048 / 3072 |
| `AesCrypto` | AES-CBC / CFB / OFB (auto IV) + AES-GCM (12-byte nonce + 16-byte tag). ECB is supported for interoperability but CBC is the default and ECB is documented as insecure |

### 1.3 ShangMi (`DevTrove.Crypto.Crypto.Sm`)

| Type | Purpose |
|---|---|
| `SM2` | Sign / verify, encrypt / decrypt, key exchange (curve `sm2p256v1`) |
| `SM3` | Hash (256-bit output) |
| `SM4` | Block cipher (CBC / CTR / GCM) |

---

## 2. X.509 (`DevTrove.Crypto.X509`)

### 2.1 Top-level types

| Type | Purpose |
|---|---|
| `Certificate` | Parse + generate (self-signed, sign-CSR, sign-public-key); extensions, fingerprints, validity, key usage |
| `CertificateSigningRequest` | Generate (with `X509ExtensionOptions`) + parse + verify signature |
| `CertificateRevocationList` | Generate (with revocation reasons) + parse + `IsRevoked` |
| `X509ExtensionBuilder` | Apply `X509ExtensionOptions` to a certificate generator or assemble a CSR extension set |

### 2.2 Models (`DevTrove.Crypto.X509.Models`)

| Type | Purpose |
|---|---|
| `X509DistinguishedName` | DN parse / access (RFC 2253 + OpenSSL slash style) |
| `GeneralName` | SAN entry (DNS / IP / URI / email / ...) |
| `X509ExtensionOptions` | Declarative container for KU / EKU / SAN / BasicConstraints / SKI / AKI / CRL DP |
| `BasicConstraintsOptions` | CA / path-length constraint parameters |
| `CertificateExtension` | Generic extension value descriptor |
| `PfxBundle` | PFX / PKCS#12 container (cert + chain + private key) |
| `RevokedCertificateInfo` | CRL entry |

### 2.3 Enums (`DevTrove.Crypto.X509.Enums`)

| Type | Purpose |
|---|---|
| `KeyUsage` | `[Flags]` per RFC 5280 §4.2.1.3 |
| `ExtendedKeyUsage` | Server / Client auth, Code signing, Email, etc. |
| `GeneralNameType` | DNS / IP / URI / email / ... |
| `CertificatePolicy` | OID-based policy identifiers |
| `CertificateRevocationReason` | RFC 5280 revocation reasons |

Each enum ships with `*Extensions.cs` (extension methods) and a `*Helper.cs` (`X509/Extensions/`) for resource-string display + parsing.

### 2.4 Utilities (`DevTrove.Crypto.X509.Utils`)

| Type | Purpose |
|---|---|
| `CertificateUtils` | Common helpers (parsing, format detection, etc.) |
| `PfxUtils` | `ToPfx` / `FromPfx` for PKCS#12 import-export |
| `X509NameParser` | DN parser (RFC 2253 + OpenSSL slash style) |

### 2.5 Extensions (`DevTrove.Crypto.X509.Extensions`)

| Type | Purpose |
|---|---|
| `X509ExtensionBuilder` | (also listed in §2.1; the implementation lives here) |
| `FingerprintHelper` | SHA-1 / SHA-256 / SHA-384 / SHA-512 / MD5 / SM3 thumbprint |
| `KeyUsageHelper` / `ExtendedKeyUsageHelper` / `CertificatePolicyHelper` / `CertificateRevocationReasonHelper` / `GeneralNameTypeHelper` | Enum ↔ string / resource-string helpers |

---

## 3. BouncyCastle interop (`DevTrove.Crypto.BouncyCastle.*`)

| Type | Purpose |
|---|---|
| `CertificationRequestInfoExtensions` | Extension methods on BouncyCastle `CertificationRequestInfo` |
| `CertificatePolicyObjectIdentifiers` | OID constants for certificate-policy extension |
| `ExtendedKeyUsageObjectIdentifiers` | OID constants for extended-key-usage extension |

These wrap BouncyCastle types to make `Core` self-contained without forcing consumers to `using Org.BouncyCastle.*` everywhere.

---

## 4. Helpers (`DevTrove.Crypto.Common`, `DevTrove.Crypto.Extensions`)

| Type | Purpose |
|---|---|
| `EnumDisplayNameCache<TEnum>` | Cached resource-string lookup for enum display |
| `ArgumentNullExceptionExtensions` | `ThrowIfNull(...)` convenience overloads |

---

## 5. Resource files

| File | Purpose |
|---|---|
| `Resources/CryptoUtilCore.resx` | Base resource set (Chinese) |
| `Resources/CryptoUtilCore.zh-hans.resx` | Simplified Chinese |
| `Resources/CryptoUtilCore.en-us.resx` | English (United States) |

---

## 6. Reserved namespace — `DevTrove.Crypto.Tls`

The namespace is reserved for the TLS probe engine. No public types exist today; the package is scheduled for `0.4.0` ([roadmap.md](roadmap.md) §6.17). The list below is a **design target**, not an API commitment.

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

## 7. Consuming the API

Consumers reference the metapackage:

```xml
<PackageReference Include="DevTrove.Crypto" Version="[0.1.0, )" />
```

The metapackage transitively pulls `DevTrove.Crypto.Core`. Use the namespaces:

```csharp
using DevTrove.Crypto;
using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.Crypto.Sm;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.X509.Enums;
```

For more on packaging and version constraints see [nuget.md](nuget.md).

---

## 8. Maintenance

This index is hand-maintained. When a new public type is added:

1. Add it to the matching section above.
2. Add a Chinese line in [library-api.zh-CN.md](library-api.zh-CN.md) at the matching section (sections, tables and descriptions stay one-to-one).
3. Verify with `grep -rn 'public (class|sealed class|record|enum|interface|struct) ' src/DevTrove.Crypto.Core --include='*.cs' | grep -v 'Resources/' | grep -v '.Designer\.cs'` — every match should appear in this index.

> The metapackage's `Program.cs` is excluded — once it is removed (`RM-0.0.2`), drop the corresponding note here.
