# DevTrove.Crypto

基于 BouncyCastle 的纯托管 .NET 密码学与证书库，原生支持中国商用密码（国密 SM2/SM3/SM4）与 X.509 / OCSP。

> 英文入口：[README.md](README.md)

---

## 特性

- **三个 NuGet 包**：`DevTrove.Crypto`（门面包）、`DevTrove.Crypto.Core`（实现）、`DevTrove.Crypto.Tls`（TLS 探测引擎，Phase 0 引入）
- **纯托管、零原生依赖** —— 可在 BouncyCastle 支持的所有宿主上运行，包括 WebAssembly
- **算法**：RSA / ECDSA / DSA、AES（CBC / GCM）、SM2 / SM3 / SM4
- **X.509 / PKCS**：证书、CSR、CRL、PFX / PKCS#12、OCSP（仅解析）
- **多目标**：`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`
- **Apache-2.0** 许可

---

## 安装

```bash
# 门面包（推荐）：仅传递依赖，自动引入 Core
dotnet add package DevTrove.Crypto

# 或显式引用实现包
dotnet add package DevTrove.Crypto.Core
```

运行时需 .NET 8.0+；旧宿主（.NET Framework、Unity 等）使用 `netstandard2.0` / `netstandard2.1` 版本。

---

## 快速上手

```csharp
using DevTrove.Crypto;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.Crypto.Sm;

// 解析 PEM 证书
var cert = Certificate.FromPem(pemString);
Console.WriteLine($"主体：{cert.Subject}");
Console.WriteLine($"到期：{cert.NotAfter:yyyy-MM-dd}");
Console.WriteLine($"SHA-256 指纹：{cert.Sha256Thumbprint}");

// 生成 SM2 密钥对并自签名证书
using var sm2 = new SM2();
sm2.GenerateKeyPair();
```

更多示例见 `tests/DevTrove.Crypto.Core.Tests/`。

---

## 目标框架

| 目标 | 用途 |
|---|---|
| `netstandard2.0` | .NET Framework 4.6.2+、Unity 等 |
| `netstandard2.1` | .NET Core 3.x 宿主 |
| `net8.0` / `net9.0` | 当前 LTS / STS |
| `net10.0` | 最新语言特性 |

恢复 `netstandard2.0` 目标时会做编译探针，缺失 API 放在 `Compat/` 手写补丁（详见 [`docs/development-guide.md`](docs/development-guide.md)）。

---

## 目录布局

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/          门面包（无源码）
│  ├─ DevTrove.Crypto.Core/     实现
│  └─ DevTrove.Crypto.Tls/      TLS 探测引擎（Phase 0 引入）
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   openssl / tongsuo CLI 封装
├─ scripts/                       夹具生成
└─ docs/                          开发文档（中文）
```

---

## 文档

`docs/` 下每份文档成对存在：英文默认入口（`x.md`）+ 中文对照（`x.zh-CN.md`），章节 / 表格 / Mermaid 一一对应。

| 文档 | 内容 |
|---|---|
| [docs/architecture.md](docs/architecture.md) | 库内分层、依赖方向、能力边界、已知限制 |
| [docs/standards.md](docs/standards.md) | 编码规范、命名、Git 流程、评审清单 |
| [docs/nuget.md](docs/nuget.md) | 包边界、版本策略、发布流程 |
| [docs/tls-scanner.md](docs/tls-scanner.md) | TLS 探测引擎设计（Phase 3 / Phase 4 排期） |
| [docs/roadmap.md](docs/roadmap.md) | 库的阶段路线 + 待办 / 已知偏差 |
| [docs/development-guide.md](docs/development-guide.md) | 构建/测试/打包命令、TFM / polyfill 规则、CI |
| [docs/library-api.md](docs/library-api.md) | 公开 API 索引 |

---

## 构建与测试

```bash
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  DevTrove.Crypto.slnx -c Release
```

互操作测试依赖外部工具 `openssl` 3.x。缺失时测试**直接失败而非跳过**（见 [docs/standards.md](docs/standards.md)）。

> 当前构建因 `NU1201` 失败 —— 见 [docs/roadmap.md](docs/roadmap.md) 的「待办 / 已知偏差」表（B1、B2）。

---

## 状态

**早期开发**。首发版本号 `0.0.1-dev`，按 `0.0.X-dev → 0.1.0` 迭代。详见 [`docs/roadmap.md`](docs/roadmap.md)。

---

## 许可

Apache-2.0，详见 [LICENSE](LICENSE)。
