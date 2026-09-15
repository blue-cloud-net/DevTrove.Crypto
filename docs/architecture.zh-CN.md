# 架构

> 英文默认入口：[architecture.md](architecture.md)

本文档描述 `DevTrove.Crypto` 的库内分层、依赖方向、能力边界、已知限制与技术决策。

---

## 1. 仓库定位

`DevTrove.Crypto` 是 DevTrove 工具箱的**核心库**，**独立发布、独立版本号**：
基于 BouncyCastle 的纯托管 .NET 密码学与证书库，提供：

| 能力 | 示例 |
|---|---|
| 算法原语 | RSA、ECDSA、DSA、AES（CBC / CFB / OFB / GCM） |
| 国密 | SM2、SM3、SM4（经 BouncyCastle） |
| 密钥格式 | PEM、DER、PKCS#1、PKCS#8、SEC1、加密 PEM |
| ASN.1 / X.509 | 证书 / CSR / CRL / PKCS#12 的解析与生成 |
| PKCS#7 / CMS | SignedData 解析 —— **计划中，`0.3.0`** |
| OCSP | 响应解析 —— **计划中，`0.3.0`**（不构造请求，也不验签；验签在 `0.5.0` 评估） |
| TLS 探测引擎 | 协议 / 套件矩阵、扩展指纹、NTLS 检测、评级 —— **计划中，`0.4.0`–`0.5.0`** |

### 目标

| # | 目标 |
|---|---|
| G1 | **纯托管、零原生依赖。** 同一份程序集处处行为一致，不带 `runtimes/<rid>/native` 负载。 |
| G2 | **跨平台。** 服务端、桌面、嵌入式与 WebAssembly 都是一等宿主；库内任何能力都不依赖宿主 OS 策略。 |
| G3 | **AOT 与裁剪友好**（`net8.0` 及以后），使消费方可发布裁剪或提前编译的应用。 |
| G4 | **宽兼容面。** `netstandard2.0` / `netstandard2.1` 让更旧的运行时（含 .NET Framework、Unity）可用，`net8.0`–`net10.0` 承担现代面与 AOT 面。 |
| G5 | **零框架依赖。** 不引用 DI、日志、ASP.NET Core；库从不决定消费方如何装配。 |
| G6 | **独立发布、独立版本号**，并自带自洽的文档集。 |

本仓库独立发布、独立版本；库内任何内容都不依赖它被如何消费、被谁消费。各目标的当前进度见 [roadmap.md](roadmap.md)。

---

## 2. 仓内布局

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/         门面包（元包，无源码，见 §3）
│  ├─ DevTrove.Crypto.Core/    实现
│  └─ DevTrove.Crypto.Tls/     TLS 探测引擎（计划中 —— 见 [roadmap.md](roadmap.md) §6.17）
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   tongsuo CLI 封装
├─ scripts/                     夹具生成脚本
└─ docs/                        开发文档（英文默认 + `.zh-CN.md` 中文）
```

每个 `src/<Project>/` 对应一个 NuGet 包。

---

## 3. 包清单

| 包 ID | 角色 | 说明 |
|---|---|---|
| `DevTrove.Crypto` | **门面包（元包）**：仅 `ProjectReference` → Core；无源码、无公开 API 类型 | 使用方引用此包；`DevTrove.Crypto.Core` 自动引入 |
| `DevTrove.Crypto.Core` | **实现**：BouncyCastle 封装 —— 算法、密钥、ASN.1、X.509、CSR、PKCS#7/#12、CRL、OCSP 解析 | 真正承载代码的包 |
| `DevTrove.Crypto.Tls` | **TLS 探测引擎** | **计划中。** 命名空间已预留，包排期在 `0.4.0`；当前不存在任何类型（见 [roadmap.md](roadmap.md) §6.17）。 |

> **已知偏差**（见 [roadmap.md](roadmap.md) §6.2，`RM-0.0.2`）：门包项目 `src/DevTrove.Crypto/` 仍残留 `Program.cs`，而设计要求该项目无源码。门包同时固定 `net10.0`，Core 却是三个 TFM。在 TFM 集合统一（`RM-0.0.1`）且该文件删除之前，门包的构建与打包产物不可信。

### 依赖图

```mermaid
flowchart LR
    BC["BouncyCastle.Cryptography"] --> CORE["DevTrove.Crypto.Core"]
    CORE --> FACADE["DevTrove.Crypto"]
    FACADE --> TLS["DevTrove.Crypto.Tls"]
    TLS --> APP["应用项目（不发布）"]
```

---

## 4. Core 源码布局

```
src/DevTrove.Crypto.Core/
├─ Crypto/
│  ├─ AsymmetricKeyPair.cs              RSA / EC / DSA / SM2 密钥对生成
│  ├─ AsymmetricKeyParameter.cs         PEM/DER 导入导出基类
│  ├─ AsymmetricPrivateKeyParameter.cs
│  ├─ AsymmetricPublicKeyParameter.cs
│  ├─ RsaCrypto.cs                      RSA 加解密 / 签名验签
│  ├─ EcdsaCrypto.cs                    ECDSA 签名验签 + ECDH
│  ├─ DsaCrypto.cs                      DSA 签名验签
│  ├─ AesCrypto.cs                      AES-CBC / CFB / OFB / GCM（支持 ECB 但默认 CBC；见 D15）
│  └─ Sm/
│     ├─ SM2.cs                         签名验签 / 加解密 / 密钥交换
│     ├─ SM3.cs                         摘要
│     └─ SM4.cs                         分组密码（CBC / CTR / GCM）
├─ X509/
│  ├─ Certificate.cs                    解析 + 生成（自签 / 签 CSR / 签公钥）
│  ├─ CertificateSigningRequest.cs      生成（含扩展）+ 解析 + 验签
│  ├─ CertificateRevocationList.cs      生成 + 解析 + IsRevoked
│  ├─ Enums/                            KeyUsage / ExtendedKeyUsage / GeneralNameType / CertificatePolicy / CertificateRevocationReason + Helpers
│  ├─ Extensions/                       X509ExtensionBuilder + Helpers
│  ├─ Models/                           X509DistinguishedName / GeneralName / PfxBundle / X509ExtensionOptions / CertificateExtension / RevokedCertificate
│  └─ Utils/                            CertificateUtils / PfxUtils / X509NameParser
├─ BouncyCastle/
│  ├─ Asn1/X509/                        ASN.1 / X.509 扩展方法
│  └─ ObjectIdentifiers/                OID 常量
├─ Common/                              EnumDisplayNameCache
├─ Extensions/                          ArgumentNullExceptionExtensions
└─ Resources/                           zh-hans + en-us resx
```

### 4.1 目标布局（`0.1.0` 及以后）

`Crypto/` 下的内容将改为 `Algorithms/`（拆分为 `Asymmetric/`、`Symmetric/`、`Hash/`），每个算法代理统一命名为 `<Algorithm>Crypto`，BouncyCastle 类型退出公开面，其余目录随能力落地逐步出现。

| 目录 | 命名空间 | 落地版本 |
|---|---|---|
| `Algorithms/` | `DevTrove.Crypto.Algorithms` | `0.1.0` |
| `Asn1/` | `DevTrove.Crypto.Asn1` | `0.1.0` |
| `Interop/` | `DevTrove.Crypto.Interop` | `0.1.0` |
| `Compat/` | 仅内部 | `0.1.0` |
| `Formats/` | `DevTrove.Crypto.Formats` | `0.3.0` |
| `X509/Chain/` | `DevTrove.Crypto.X509.Chain` | `0.3.0` |
| `X509/Ocsp/` | `DevTrove.Crypto.X509.Ocsp` | `0.3.0` |

上面的目录树描述**当前**代码；本表描述它将要变成什么。逐项状态见 [roadmap.md](roadmap.md) §6。

---

## 5. 依赖方向（单向，强制）

| 来源 | 目标 | 是否允许 |
|---|---|---|
| `DevTrove.Crypto.Core` | `BouncyCastle.Cryptography` | ✅ |
| `DevTrove.Crypto`（门面包） | `DevTrove.Crypto.Core` | ✅（仅 `ProjectReference`） |
| `DevTrove.Crypto.Tls` | `DevTrove.Crypto.Core` | ✅ |
| `DevTrove.Crypto.Tls` | `DevTrove.Crypto.Core` | ✅ |
| 本仓库内任何项目 | 任何 DI / 日志 / ASP.NET Core / `Microsoft.Extensions.*` | ❌ |

库**零框架依赖**：不引用 DI、日志、ASP.NET Core。配合 §1 的目标集，消费方覆盖尽可能宽（服务端、桌面、嵌入式、WebAssembly、裁剪 / AOT 构建），把横切关注点的决定权留给消费方。

---

## 6. 能力边界

### 6.1 BouncyCastle 能做的

- **协议版本覆盖**：`ProtocolVersion.SSLv3`（`0x0300`）~ TLS 1.3（`0x0304`）；`CLIENT_EARLIEST_SUPPORTED_TLS = SSLv3` → 纯托管可枚举全范围，不受宿主 OS 策略影响
- **`TlsClient` 回调**：`GetClientExtensions`、`GetSupportedVersions`、`GetCipherSuites`、`GetEarlyKeyShareGroups`、`NotifyServerVersion`、`NotifySelectedCipherSuite`、`ProcessServerExtensions(IDictionary<int, byte[]>)`、`NotifyNewSessionTicket`
- **关键类型**：`CertificateStatus`（OCSP staple）、`CertificateStatusRequest`、`OcspStatusRequest`、`ExtensionType`、`TlsExtensionsUtilities`、`NamedGroup`（含 `curveSM2MLKEM768`）、`TlsServerCertificate`
- **RFC 8998**（SM2-TLS 1.3）已内建：`TLS_SM4_GCM_SM3 = 0x00C6`、`TLS_SM4_CCM_SM3 = 0x00C7`、`tls13_hkdf_sm3`
- `TlsProtocol` 为 abstract；`WriteRecord`、`SafeWriteRecord`、`WriteHandshakeMessage`、`ReadExtensionsData`/`WriteExtensionsData`、`OfferInput`/`ReadOutput`（非阻塞）可见 → 可子类化注入裸 record

### 6.2 BouncyCastle **做不到**的

| 做不到 | 原因 |
|---|---|
| Heartbleed 探测 | `TlsProtocol.ProcessRecord` 的 heartbeat 分支已被注释 |
| CCS Injection | 同上 —— 需要自研 record layer 与密钥派生 |
| NTLS 完整握手 | 见 §6.3 |

### 6.3 NTLS / GB/T 38636

NTLS 与标准 TLS 有三处**结构性差异**：

| 差异 | 值 |
|---|---|
| 协议版本（`legacy_version`） | `0x0101`（非 `0x0303`） |
| 套件 | `0xE011`、`0xE013`、`0xE051`、`0xE053` 等 |
| 双证书 | 一张签名证书 + 一张加密证书（均为 SM2） |

BouncyCastle 直接拒绝：

- `ProtocolVersion.IsSupportedTlsVersionClient` 约束 `FullVersion ∈ [0x0300, 0x0304]` → `0x0101` 被拒；`legacy_version` 由协议层写死
- `0xE0xx` 套件不被密钥交换工厂与 PRF 选择逻辑识别
- 无 GB/T 38636 ECC/ECDHE-SM2 密钥交换实现

**NTLS 检测可零密码学成本完成**：TLS 1.2 中 `ClientHello` / `ServerHello` / `Certificate` / `ServerKeyExchange` 均为明文，因此手工构造 `ClientHello`（`legacy_version=0x0101`、套件 `0xE011/0xE013/0xE051/0xE053`、SM2 签名算法、SNI）+ 手工解析即可得出版本、套件、双证书是否齐全、签名算法与曲线。

NTLS 完整握手（record layer + SM3 PRF + SM2 密钥交换 + SM4 记录保护 + 双证书处理）**不在 v1 范围** —— 需要自研 TLS 1.2 子集，国标细节（IV、padding、MAC 顺序、PRF 标签、签名编码）风险集中。

---

## 7. 已知限制与能力缺口

这里放两类不同的东西——把它们混为一谈，正是上一轮文档漂移的根源。

- **缺口**：库**尚未具备**的能力，带有目标版本号。
- **限制**：真实存在、有意保留的行为，必须在任何消费它的结果对象或 UI 中显式暴露。

### 7.1 能力缺口

| # | 缺口 | 影响 | 目标 |
|---|---|---|---|
| G1 | **完全没有**证书链构建与路径验证，只有 `Certificate.IsSignatureVerify` 的单级验签 | 调用方今天无法验证一条链 | `0.3.0`（`RM-0.3.0-01`） |
| G2 | 无 PKIX 路径校验（不校验 `AuthorityKeyIdentifier`、`KeyUsage`、`BasicConstraints`、路径长度、策略与名称约束） | 可能把 PKIX 下本应被拒的链判为有效 | `1.0.0`（`RM-1.0.0-02`） |
| G3 | 完全没有 OCSP 代码 | 无法给出吊销状态结论 | `0.3.0`（`RM-0.3.0-08`）；验签在 `0.5.0` 评估 |
| G4 | 不支持 PKCS#7 / CMS | 无法消费 CMS SignedData | `0.3.0`（`RM-0.3.0-07`） |
| G5 | 没有任何密钥派生函数（HKDF、PBKDF2、scrypt） | 调用方必须自行实现密钥派生 —— 最容易自研出错的一步 | `0.2.0`（`RM-0.2.0-05`） |
| G6 | 不支持任何 HMAC | 无法计算消息认证码 | `0.2.0`（`RM-0.2.0-03`） |
| G7 | 无 Ed25519 / X25519 系列 | 现代签名与密钥协商套件不可用 | `0.2.0`（`RM-0.2.0-01`） |
| G8 | 无格式自动识别（PEM / DER 等） | 调用方必须事先知道格式 | `0.3.0`（`RM-0.3.0-03`） |
| G9 | `netstandard2.0` / `netstandard2.1` 无法构建 | 文档宣称的兼容面实际不存在 | `RM-0.0.11` |

### 7.2 会保留的限制

| # | 限制 | 影响 | 说明 |
|---|---|---|---|
| L1 | NTLS 仅做**字节级指纹检测**，不做完整握手 | 无法验证国密栈的完整行为 | 需从零实现 TLS 1.2 子集（见 [tls-scanner.md §6.3](tls-scanner.md)） |
| L2 | 不做 L3 漏洞探测（Heartbleed、CCS Injection、Ticketbleed） | 对这些类别不给出漏洞结论 | 明确不做；仅 ROBOT 在范围内 |
| L3 | 非对称代理只暴露其算法真实具备的能力 | 并非每个非对称类型都能既签名又加密 | X25519 只做密钥协商，Ed25519 只做签名（见 [roadmap.md](roadmap.md) §6.15） |

每个缺口的当前状态见 [roadmap.md](roadmap.md)。

---

## 8. 技术决策

| # | 决策 | 理由 |
|---|---|---|
| D1 | 用 BouncyCastle 而非 .NET 原生 `SslStream` 做 TLS 探测 | `SslStream` 的 `CipherSuitesPolicy` 标 `[UnsupportedOSPlatform("windows")]`，不暴露裸握手字节 / 扩展 / OCSP staple / SCT，只能给出单次协商结果 —— 无法构造矩阵 |
| D2 | 用 BouncyCastle 而非原生 P/Invoke | 纯托管跨平台一致，含 WebAssembly 与裁剪 / AOT 构建；无 `runtimes/<rid>/native` 打包负担；CI 更简单 |
| D3 | 不做 L3 漏洞探测 | Heartbleed / CCS Injection 需自研 record layer + 密钥派生；BouncyCastle heartbeat 分支被注释，无法复用 |
| D4 | 不打包 `testssl.sh` | 其许可证为 GPLv2；仅作可选外部交叉验证工具 |
| D5 | NTLS 只做字节级指纹检测，不做完整握手 | 握手报文明文足以得出主要结论；完整握手需自研 TLS 1.2 子集，国标细节风险集中 |
| D6 | TLS 探测引擎与 Crypto 同仓，包名 `DevTrove.Crypto.Tls`，不再另建独立 TLS 仓库 | 引擎只有一条依赖（`DevTrove.Crypto`）；同仓可共用 TFM / polyfill / CI / 夹具，省去跨仓版本对齐成本。三个包仍各自独立版本号 |
| D7 | 独立仓库：自行开发、测试与发布，仓内保留 `src/` 分层 | `src/` 与 NuGet 包、命名空间一一对应；仓内任何内容都不依赖它被如何消费 |
| D8 | 版本线：`0.0.x` 仅为工作项编号（不发布），首次真实发布为 `0.1.0` | 与任何消费方版本解耦（见 [nuget.md](nuget.md) §3）；`0.x` 阶段允许破坏性变更 |
| D9 | 库自带结果模型 —— 依赖只向外 | 库可独立发布、独立使用 |
| D10 | 零框架依赖：不引用 DI / 日志 / ASP.NET Core | 最大化消费方覆盖（含 .NET Framework、Unity、WebAssembly、裁剪 / AOT）；测试面更干净 |
| D11 | 门面包无源码 | 仅 `ProjectReference` → Core；消费方只见门面包；将来可换实现而不破坏契约 |
| D12 | 不引入原生依赖，不打包 `runtimes/<rid>/native` | 单二进制部署、无平台矩阵、可编译进 WebAssembly，也兼容 AOT |
| D13 | RSA 加密默认 OAEP-SHA256，签名默认 PSS；PKCS#1 v1.5 保留为可选 | 现代默认 + 兼容互操作 |
| D14 | ECDSA / DSA 签名使用标准 DER 编码 | 与 OpenSSL 等工具互操作 |
| D15 | AES 与 SM4 **都支持 ECB**，但默认均为 CBC。ECB 在每一处 XML 注释与文档中都标注为不安全，仅用于互操作或遗留协议 | ECB 对新设计不安全，但互操作又必须支持，因此选择“支持并告警”而非“直接禁止”。GCM 用 12 字节随机 Nonce + 16 字节认证标签 |
| D16 | DSA 仅签名/验签（算法限制）；密钥长度 1024/2048/3072 | DSA 不能加密 |
| D17 | ECDH 返回原始共享密钥字节，KDF 由调用方决定 | 库不强加 KDF 选择 |
| D18 | 测试夹具**只生成、不入库**：`tests/data/` 被整体忽略，由 `scripts/generate-test-*.sh` 重建（测试程序集的 module initializer 会按需自动调用）。夹具短有效期（≈1 年）。脚本产不出的抓取类数据 —— NTLS 握手字节 —— 放在**已入库**的 `tests/fixtures/` | CI 不依赖远程生成；断言不得依赖「当前有效」，也不得依赖「某夹具文件存在于仓库中」 |
| D19 | 外部工具缺失时测试**失败**而非跳过（`CliToolGuard`） | 环境问题立刻暴露 |
| D20 | **自建密码学抽象**，仅在需要 `CryptoStream` / `SslStream` / `X509Certificate2` 互操作处提供 BCL 适配器 | BCL 基类无法表达 CTR 与 AEAD（`CipherMode` 是封闭枚举），而 `TryEncryptEcbCore` 这类 .NET 8 专属虚方法在两个 `netstandard` 目标上不存在 —— 继承它们等于把库的能力集绑死在特定 TFM 上。见 [roadmap.md](roadmap.md) §6.14 |
| D21 | BouncyCastle 类型不进入公开 API；互操作只经显式的 `Interop` 扩展方法 | 实现包应当可替换而不破坏消费方；此前有 9 个公开成员泄露了 BouncyCastle 类型 |
| D22 | tongsuo 是**唯一**的外部工具（夹具与互操作测试） | 只需安装、pin 与缓存一个工具，且它同时覆盖标准算法与国密。代价是不再断言与上游 OpenSSL 的互操作 —— 已记入 [roadmap.md](roadmap.md) §8 风险 |
| D23 | 算法代理统一命名为 `<Algorithm>Crypto`（`RsaCrypto`、`Sm2Crypto`、`Sm4Crypto` 等） | 一套命名规则覆盖全部算法；此前 `RsaCrypto` 与 `SM2` 的混搭毫无依据 |

### 已否决的方案

| 方案 | 否决原因 |
|---|---|
| 铜锁 P/Invoke 做 NTLS 完整握手 | 原生依赖与 WebAssembly 目标冲突 |
| 打包 `testssl.sh` | GPLv2 |
| 自研 record layer 做 Heartbleed / CCS Injection | 成本；BC heartbeat 分支已被注释 |

---

## 9. 当前状态速览

完整路线、逐项状态与证据见 [roadmap.md](roadmap.md)。

- **`DevTrove.Crypto.Core`**：已实现 RSA、ECDSA、DSA、AES、SM2、SM3、SM4，以及 X.509 / CSR / CRL / PKCS#12 的解析与生成。尚**不**支持两个 `netstandard` 目标，也没有证书链、OCSP、PKCS#7、KDF、MAC 与 Ed25519 / X25519 —— 见 §7.1。
- **`DevTrove.Crypto`（门包）**：项目存在，但仍残留 `Program.cs` 且固定 `net10.0`（`RM-0.0.2`、`RM-0.0.1`）。
- **`DevTrove.Crypto.Tls`**：未启动。命名空间已预留，排期 `0.4.0`。

定量表述（测试用例数、构建结果）刻意不写进本文档：读文档无法验证它们，而上一版已经证明这类数字腐坏得有多快。请从构建与测试输出获取，或看 [roadmap.md](roadmap.md) 的证据列。

公开 API 索引见 [library-api.md](library-api.md)。
