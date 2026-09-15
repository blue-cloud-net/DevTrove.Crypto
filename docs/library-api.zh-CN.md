# 公开 API 索引

> 英文默认入口：[library-api.md](library-api.md)

`DevTrove.Crypto` 的公开 API 索引。由 `src/DevTrove.Crypto.Core/**` **手工整理**：每项给出命名空间、类型与一句话说明。**根命名空间统一为 `DevTrove.Crypto`**；`DevTrove.Crypto.Core` 项目把命名空间拍平，消费方不依赖项目名 `Core`。

> **已知偏差**（roadmap B2）：门面包项目 `src/DevTrove.Crypto/` 仍带 `Program.cs`，并非无源码。B2 解决后，本索引仍适用。

---

## 1. 算法原语（`DevTrove.Crypto.Crypto`）

### 1.1 非对称密钥

| 类型 | 用途 |
|---|---|
| `AsymmetricKeyPair` | RSA / EC / SM2 / DSA 密钥对生成；由私钥推导公钥 |
| `AsymmetricKeyParameter` | 私钥 / 公钥参数的基类；PEM / DER 导入导出基类 |
| `AsymmetricPrivateKeyParameter` | 私钥载体；加 / 解密、签名；推导公钥 |
| `AsymmetricPublicKeyParameter` | 公钥载体；验签、（适用时）加密 |
| `PasswordFinder` | 解密加密 PEM 用的 `IPasswordFinder` 适配器 |

### 1.2 算法封装

| 类型 | 用途 |
|---|---|
| `RsaCrypto` | RSA 加 / 解密（默认 OAEP-SHA256，可选 PKCS#1 v1.5）；签名 / 验签（默认 PSS，可选 PKCS#1 v1.5） |
| `EcdsaCrypto` | ECDSA 签名 / 验签（DER 编码）；ECDH 共享密钥（同曲线校验） |
| `DsaCrypto` | DSA 签名 / 验签（DER 编码）；密钥长度 1024 / 2048 / 3072 |
| `AesCrypto` | AES-CBC / CFB / OFB（自动 IV）+ AES-GCM（12 字节 Nonce + 16 字节 Tag）；**禁用 ECB** |

### 1.3 国密（`DevTrove.Crypto.Crypto.Sm`）

| 类型 | 用途 |
|---|---|
| `SM2` | 签名 / 验签、加 / 解密、密钥交换（曲线 `sm2p256v1`） |
| `SM3` | 摘要（256 位输出） |
| `SM4` | 分组密码（CBC / CTR / GCM） |

---

## 2. X.509（`DevTrove.Crypto.X509`）

### 2.1 顶级类型

| 类型 | 用途 |
|---|---|
| `Certificate` | 解析 + 生成（自签 / 签 CSR / 签公钥）；扩展、指纹、有效期、密钥用法 |
| `CertificateSigningRequest` | 生成（带 `X509ExtensionOptions`）+ 解析 + 验签 |
| `CertificateRevocationList` | 生成（含吊销原因）+ 解析 + `IsRevoked` |
| `X509ExtensionBuilder` | 把 `X509ExtensionOptions` 应用到证书生成器或组装 CSR 扩展集 |

### 2.2 模型（`DevTrove.Crypto.X509.Models`）

| 类型 | 用途 |
|---|---|
| `X509DistinguishedName` | DN 解析 / 访问（RFC 2253 + OpenSSL 斜杠风格） |
| `GeneralName` | SAN 条目（DNS / IP / URI / 邮箱 / ...） |
| `X509ExtensionOptions` | KU / EKU / SAN / BasicConstraints / SKI / AKI / CRL DP 声明式容器 |
| `BasicConstraintsOptions` | CA / 路径长度约束参数 |
| `CertificateExtension` | 通用扩展值描述 |
| `PfxBundle` | PFX / PKCS#12 容器（证书 + 链 + 私钥） |
| `RevokedCertificateInfo` | CRL 条目 |

### 2.3 枚举（`DevTrove.Crypto.X509.Enums`）

| 类型 | 用途 |
|---|---|
| `KeyUsage` | `[Flags]`，RFC 5280 §4.2.1.3 |
| `ExtendedKeyUsage` | Server / Client 认证、代码签名、邮件等 |
| `GeneralNameType` | DNS / IP / URI / 邮箱 / ... |
| `CertificatePolicy` | 基于 OID 的策略标识 |
| `CertificateRevocationReason` | RFC 5280 吊销原因 |

每个枚举配套 `*Extensions.cs`（扩展方法）与 `*Helper.cs`（`X509/Extensions/` 下）做资源字符串显示 + 解析。

### 2.4 工具（`DevTrove.Crypto.X509.Utils`）

| 类型 | 用途 |
|---|---|
| `CertificateUtils` | 常用辅助（解析、格式识别等） |
| `PfxUtils` | PFX / PKCS#12 导入导出（`ToPfx` / `FromPfx`） |
| `X509NameParser` | DN 解析器（RFC 2253 + OpenSSL 斜杠风格） |

### 2.5 扩展（`DevTrove.Crypto.X509.Extensions`）

| 类型 | 用途 |
|---|---|
| `X509ExtensionBuilder` | 在 §2.1 也列出；实现位于此处 |
| `FingerprintHelper` | SHA-1 / SHA-256 / SHA-384 / SHA-512 / MD5 / SM3 指纹 |
| `KeyUsageHelper` / `ExtendedKeyUsageHelper` / `CertificatePolicyHelper` / `CertificateRevocationReasonHelper` / `GeneralNameTypeHelper` | 枚举 ↔ 字符串 / 资源字符串辅助 |

---

## 3. BouncyCastle 互操作（`DevTrove.Crypto.BouncyCastle.*`）

| 类型 | 用途 |
|---|---|
| `CertificationRequestInfoExtensions` | BouncyCastle `CertificationRequestInfo` 的扩展方法 |
| `CertificatePolicyObjectIdentifiers` | 证书策略扩展的 OID 常量 |
| `ExtendedKeyUsageObjectIdentifiers` | 扩展密钥用法扩展的 OID 常量 |

这些把 BouncyCastle 类型再封一层，让 `Core` 自洽，避免消费方到处写 `using Org.BouncyCastle.*`。

---

## 4. 辅助（`DevTrove.Crypto.Common`、`DevTrove.Crypto.Extensions`）

| 类型 | 用途 |
|---|---|
| `EnumDisplayNameCache<TEnum>` | 枚举显示名的资源字符串缓存查找 |
| `ArgumentNullExceptionExtensions` | `ThrowIfNull(...)` 便捷重载 |

---

## 5. 资源文件

| 文件 | 用途 |
|---|---|
| `Resources/CryptoUtilCore.resx` | 基线资源（中文） |
| `Resources/CryptoUtilCore.zh-hans.resx` | 简体中文 |
| `Resources/CryptoUtilCore.en-us.resx` | 英文（美国） |

---

## 6. 预留命名空间 —— `DevTrove.Crypto.Tls`

`DevTrove.Crypto.Tls` 命名空间**为未来的 TLS 探测引擎预留**，目前无公开类型。

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

## 7. 使用方式

消费方引用门面包：

```xml
<PackageReference Include="DevTrove.Crypto" Version="[0.1.0, )" />
```

门包自动引入 `DevTrove.Crypto.Core`。使用命名空间：

```csharp
using DevTrove.Crypto;
using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.Crypto.Sm;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.X509.Enums;
```

打包与版本约束详见 [nuget.md](nuget.md)。

---

## 8. 维护

本索引手工维护。新增公开类型须：

1. 加到上方对应小节
2. 在 [library-api.md](library-api.md) 的同位置补一行（章节、表格、说明一一对应）
3. 用 `grep -rn 'public (class|sealed class|record|enum|interface|struct) ' src/DevTrove.Crypto.Core --include='*.cs' | grep -v 'Resources/' | grep -v '.Designer\.cs'` 核对 —— 每个匹配应在本索引出现

> 门包的 `Program.cs` 暂排除；roadmap B2 移除后，把对应提示也去掉。
