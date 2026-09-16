# 公开 API 索引

> 英文默认入口：[library-api.md](library-api.md)

`DevTrove.Crypto` 的公开 API 索引。由 `src/DevTrove.Crypto.Core/**` **手工整理**：每项给出命名空间、类型与一句话说明。**根命名空间统一为 `DevTrove.Crypto`**；`DevTrove.Crypto.Core` 项目把命名空间拍平，消费方不依赖项目名 `Core`。

Public API index for `DevTrove.Crypto`. Generated manually from `src/**` — each entry names the namespace, type and a one-line description. **Namespace root is `DevTrove.Crypto`** for everything.

> **第 2–9 节描述的是正在重建的代码。** `DevTrove.Crypto.Core` 当前不承载任何源码：Web 时代的实现已被移除，正在按第 1 节的契约重建。那几节作为**目标索引**保留，不构成「这些类型今天已存在」的声明。

> **契约是例外** —— 第 1 节列的就是 `RM-0.0.14` 实际建成的程序集；它随首个发布 `0.1.0` 出货。

> **API 稳定性。** 库处于 `0.x`，本索引中的任何条目都不构成兼容性承诺：公开类型与成员可能在任何次版本中变更。`1.0.0` 冻结该面（`RM-1.0.0-01`）。

---

## 1. 契约（`DevTrove.Crypto.Abstractions`）

**独立程序集**，且**不依赖任何包** —— 连 BouncyCastle 都没有。消费方可仅对这个面编程。目录采用扁平布局，每个命名空间对应一个目录：`Symmetric/`、`Asymmetric/`、`Hash/`、`X509/`。

> `RM-0.0.14a`–`e` 建成该程序集；它随首个发布 `0.1.0` 出货。BCL 适配器放在它所适配的族旁边，不另建 `Interop/` 目录。

### 1.1 对称（`DevTrove.Crypto.Abstractions.Symmetric`）

| 类型 | 形态 | 用途 |
|---|---|---|
| `CipherModeKind` | enum | `Cbc` / `Cfb` / `Ofb` / `Ctr` / `Ecb` / `Gcm` —— 可表达 CTR 与 AEAD，而 BCL 的封闭 `CipherMode` 枚举做不到 |
| `PaddingKind` | enum | `None` / `Pkcs7` / `Zeros` / `AnsiX923` |
| `ISymmetricBlockCipher` | interface | 分组大小、密钥大小、模式、填充、Nonce 与 Tag 大小；`Encrypt` / `Decrypt` |
| `SymmetricBlockCipher` | abstract class | 模式分发与填充；默认 `Cbc` + `Pkcs7`；已实现 CBC / CFB / OFB 与 ECB —— **ECB 逐块独立加密、无 IV，XML 注释显式告警其不安全**；`Ctr` / `Gcm` 抛 `NotSupportedException`，待各族自行实现；`Init` / `EncryptBlock` / `DecryptBlock` 留给具体算法 |
| `SymmetricBlockCipherInteropExtensions` | static class | `AsSymmetricAlgorithm()`。CBC / CFB / OFB / ECB 桥接到 BCL；**GCM 与 CTR 抛 `NotSupportedException`** |

### 1.2 摘要（`DevTrove.Crypto.Abstractions.Hash`）

| 类型 | 形态 | 用途 |
|---|---|---|
| `IDigest` | interface | `DigestSize` / `BlockSize` / `Reset()` / `Update(ReadOnlySpan<byte>)` / `Digest()` / `ComputeHash(ReadOnlySpan<byte>)` |
| `DigestBase` | abstract class | 缓冲区管理，以及基于 `Update` + `Digest` 的一次性 `ComputeHash` |
| `DigestInteropExtensions` | static class | `AsHashAlgorithm()` 桥接到 BCL。在 `netstandard2.0` 上守卫会退回 `HashCore(byte[], int, int)` |

### 1.3 非对称（`DevTrove.Crypto.Abstractions.Asymmetric`）

| 类型 | 形态 | 用途 |
|---|---|---|
| `ISigner` | interface | 签名 / 验签 |
| `IKeyEncipherment` | interface | 加密 / 解密 |
| `IKeyAgreement` | interface | 派发共享密钥（裸字节；KDF 归调用方） |
| `IAsymmetricKey` | interface | 密钥元数据：算法与长度 |
| `IPrivateKey` / `IPublicKey` | interface | 只读密钥视图 |
| `AsymmetricKeyBase` | abstract class | 持有密钥材料，**并在释放时清零** |
| `SignatureAlgorithmKind` | enum | 签名算法枚举，使默认值可按私钥推导，而不硬编码 `SHA256WITHRSA`（`RM-0.2.0-03`） |

能力拆成独立接口而非单一基类，是因为 X25519 只做密钥协商、Ed25519 只做签名。

### 1.4 X.509（`DevTrove.Crypto.Abstractions.X509`）

| 类型 | 形态 | 用途 |
|---|---|---|
| `ICertificate` | interface | 只读证书视图：主体、签发者、序列号、有效期、编码形式 |
| `ICertificateReader` | interface | 解析单个或多个证书 |
| `ICertificateWriter` | interface | 输出证书 |
| `IDistinguishedName` | interface | 对 DN 组件的结构化访问 |

**`0.1.0` 不交付任何实现类型。** 这些接口现在声明、`0.5.0` 实现；契约测试证明它们可被 stub 实现。

---

## 2. 算法原语（`DevTrove.Crypto.Crypto`）

### 2.1 非对称密钥

| 类型 | 用途 |
|---|---|
| `AsymmetricKeyPair` | RSA / EC / SM2 / DSA 密钥对生成；由私钥推导公钥 |
| `AsymmetricKeyParameter` | 私钥 / 公钥参数的基类；PEM / DER 导入导出基类 |
| `AsymmetricPrivateKeyParameter` | 私钥载体；加 / 解密、签名；推导公钥 |
| `AsymmetricPublicKeyParameter` | 公钥载体；验签、（适用时）加密 |
| `PasswordFinder` | 解密加密 PEM 用的 `IPasswordFinder` 适配器 |

### 2.2 算法封装

| 类型 | 用途 |
|---|---|
| `RsaCrypto` | RSA 加 / 解密（默认 OAEP-SHA256，可选 PKCS#1 v1.5）；签名 / 验签（默认 PSS，可选 PKCS#1 v1.5） |
| `EcdsaCrypto` | ECDSA 签名 / 验签（DER 编码）；ECDH 共享密钥（同曲线校验） |
| `DsaCrypto` | DSA 签名 / 验签（DER 编码）；密钥长度 1024 / 2048 / 3072 |
| `AesCrypto` | AES-CBC / CFB / OFB（自动 IV）+ AES-GCM（12 字节 Nonce + 16 字节 Tag）。ECB 为互操作保留，但默认 CBC，且 ECB 已标注为不安全 |

### 2.3 国密（`DevTrove.Crypto.Crypto.Sm`）

| 类型 | 用途 |
|---|---|
| `SM2` | 签名 / 验签、加 / 解密、密钥交换（曲线 `sm2p256v1`） |
| `SM3` | 摘要（256 位输出） |
| `SM4` | 分组密码（CBC / CTR / GCM） |

---

## 3. X.509（`DevTrove.Crypto.X509`）

### 3.1 顶级类型

| 类型 | 用途 |
|---|---|
| `Certificate` | 解析 + 生成（自签 / 签 CSR / 签公钥）；扩展、指纹、有效期、密钥用法 |
| `CertificateSigningRequest` | 生成（带 `X509ExtensionOptions`）+ 解析 + 验签 |
| `CertificateRevocationList` | 生成（含吊销原因）+ 解析 + `IsRevoked` |
| `X509ExtensionBuilder` | 把 `X509ExtensionOptions` 应用到证书生成器或组装 CSR 扩展集 |

### 3.2 模型（`DevTrove.Crypto.X509.Models`）

| 类型 | 用途 |
|---|---|
| `X509DistinguishedName` | DN 解析 / 访问（RFC 2253 + OpenSSL 斜杠风格） |
| `GeneralName` | SAN 条目（DNS / IP / URI / 邮箱 / ...） |
| `X509ExtensionOptions` | KU / EKU / SAN / BasicConstraints / SKI / AKI / CRL DP 声明式容器 |
| `BasicConstraintsOptions` | CA / 路径长度约束参数 |
| `CertificateExtension` | 通用扩展值描述 |
| `PfxBundle` | PFX / PKCS#12 容器（证书 + 链 + 私钥） |
| `RevokedCertificateInfo` | CRL 条目 |

### 3.3 枚举（`DevTrove.Crypto.X509.Enums`）

| 类型 | 用途 |
|---|---|
| `KeyUsage` | `[Flags]`，RFC 5280 §4.2.1.3 |
| `ExtendedKeyUsage` | Server / Client 认证、代码签名、邮件等 |
| `GeneralNameType` | DNS / IP / URI / 邮箱 / ... |
| `CertificatePolicy` | 基于 OID 的策略标识 |
| `CertificateRevocationReason` | RFC 5280 吊销原因 |

每个枚举配套 `*Extensions.cs`（扩展方法）与 `*Helper.cs`（`X509/Extensions/` 下）做资源字符串显示 + 解析。

### 3.4 工具（`DevTrove.Crypto.X509.Utils`）

| 类型 | 用途 |
|---|---|
| `CertificateUtils` | 常用辅助（解析、格式识别等） |
| `PfxUtils` | PFX / PKCS#12 导入导出（`ToPfx` / `FromPfx`） |
| `X509NameParser` | DN 解析器（RFC 2253 + OpenSSL 斜杠风格） |

### 3.5 扩展（`DevTrove.Crypto.X509.Extensions`）

| 类型 | 用途 |
|---|---|
| `X509ExtensionBuilder` | 在 §2.1 也列出；实现位于此处 |
| `FingerprintHelper` | SHA-1 / SHA-256 / SHA-384 / SHA-512 / MD5 / SM3 指纹 |
| `KeyUsageHelper` / `ExtendedKeyUsageHelper` / `CertificatePolicyHelper` / `CertificateRevocationReasonHelper` / `GeneralNameTypeHelper` | 枚举 ↔ 字符串 / 资源字符串辅助 |

---

## 4. BouncyCastle 互操作（`DevTrove.Crypto.Asn1`、`DevTrove.Crypto.Interop`）

| 类型 | 用途 |
|---|---|
| `CertificationRequestInfoExtensions` | BouncyCastle `CertificationRequestInfo` 的扩展方法 |
| `CertificatePolicyObjectIdentifiers` | 证书策略扩展的 OID 常量 |
| `ExtendedKeyUsageObjectIdentifiers` | 扩展密钥用法扩展的 OID 常量 |

这些把 BouncyCastle 类型挡在 `Core` 自己的公开面之外 —— 见决策 D21。互操作只经 `Interop/` 中的显式扩展；不存在任何 `GetBouncyCastle*` 成员。

---

## 5. 辅助（`DevTrove.Crypto.Common`、`DevTrove.Crypto.Extensions`）

| 类型 | 用途 |
|---|---|
| `EnumDisplayNameCache<TEnum>` | 枚举显示名的资源字符串缓存查找 |
| `ArgumentNullExceptionExtensions` | `ThrowIfNull(...)` 便捷重载 |

### 5.1 签名算法默认值规则（`RM-0.2.0-03`）

任何接受可选 `signatureAlgorithm` 的公开签名方法（如 `Certificate.GenerateSelfSigned`、`Certificate.SignCsr`、`Certificate.SignPublicKey`、`CertificateRevocationList.Generate`、`CertificateSigningRequest.Generate` 的两个重载）**必须按私钥算法推导默认值**，不得硬编码 `SHA256WITHRSA` 或其它固定 OID：

| 私钥算法 | 默认签名算法 |
|---|---|
| RSA | `SHA256WITHRSA` |
| ECDSA（P-256 / P-384 / P-521） | `SHA256WITHECDSA` / `SHA384WITHECDSA` / `SHA512WITHECDSA` |
| DSA | `SHA256WITHDSA` |
| SM2（曲线 `sm2p256v1`） | `SM3WITHSM2` |
| Ed25519 / Ed448 | `Ed25519` / `Ed448`（内禀） |

调用方仍可显式覆盖；默认值的存在仅为了让 EC / DSA / SM2 / Ed25519 调用方不必特判。

---

## 6. 资源文件

资源文件随 `RM-0.0.14` 落地：`Resources/CryptoUtilCore*.resx` 与 `Common/EnumDisplayNameCache<TEnum>`，后者的反射入口带 `DynamicallyAccessedMembers` 标注，使经裁剪与 AOT 编译的消费方仍能解析枚举显示名（`RM-0.0.12`，决策 D25）。资源键由 `0.5.0` 落地的 X.509 枚举消费；在此之前该机制用测试内的枚举验证。

---

## 7. 预留命名空间 —— `DevTrove.Crypto.Tls`

该命名空间为 TLS 探测引擎预留。当前不存在任何公开类型；包排期在 `0.6.0`（[roadmap.md](roadmap.md) §6.18）。下表是**设计目标**，不是 API 承诺。

| 计划实体（见 [tls-scanner.md §12](tls-scanner.md)） | 用途 |
|---|---|
| `TlsScanReport` | 顶层扫描报告 |
| `ProtocolMatrix` | 各协议版本的支持情况与协商结果 |
| `CipherSuiteMatrix` | 各套件的支持情况与分组 |
| `ExtensionFingerprint` | 服务端扩展集合与各项判定 |
| `CertificateChainInfo` | 服务端实发链的顺序、每张证书的解析结果 |
| `OcspStaplingInfo` | 是否 stapling、响应解析结果 |
| `NtlsFingerprint` | NTLS 相关判定（版本、套件、双证书） |
| `GradeResult` | 评级、扣分项与依据 |
| `ClientSimulationMatrix` | 各客户端模拟结果 |
| `ProbeDiagnostics` | 探测过程中的异常与降级记录（**必须暴露**） |

这些**是计划项，尚未实现**。任何当前引用它们的代码都是错的。

---

## 8. 使用方式

消费方引用门面包：

```xml
<PackageReference Include="DevTrove.Crypto" Version="[0.1.0, )" />
```

门包自动引入 `DevTrove.Crypto.Core` 与 `DevTrove.Crypto.Abstractions`。使用命名空间：

```csharp
using DevTrove.Crypto;
using DevTrove.Crypto.Abstractions.Symmetric;
using DevTrove.Crypto.Abstractions.Asymmetric;
using DevTrove.Crypto.Abstractions.Hash;
using DevTrove.Crypto.Abstractions.X509;
```

若想只要契约面、**不要** BouncyCastle 实现，直接引用 `DevTrove.Crypto.Abstractions` —— 它是这里唯一不依赖任何包的包。

打包与版本约束详见 [nuget.md](nuget.md)。

---

## 9. 维护

本索引手工维护。新增公开类型须：

1. 加到上方对应小节
2. 在 [library-api.md](library-api.md) 的同位置补一行（章节、表格、说明一一对应）
3. 对 `src/**` 下的公开类型声明跑一次 `grep` 核对 —— 每个匹配都应在本索引出现

> 门包按设计不贡献任何类型，因此永远不会出现在 `grep` 结果里。
