# NuGet 包规划

> 英文默认入口：[nuget.md](nuget.md)

本文档描述 `DevTrove.Crypto` 的包边界、版本策略与发布流程。

---

## 1. 包清单

| 包 ID | 所在仓库 | 职责 | 目标框架 |
|---|---|---|---|
| `DevTrove.Crypto` | 本仓 | **门面包**：稳定的对外 API | `netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`（目标态） |
| `DevTrove.Crypto.Core` | 本仓 | **实现**：BouncyCastle 封装、算法原语、密钥、ASN.1、X.509、CSR、PKCS#7/#12、CRL、OCSP 解析 | 同上 |
| `DevTrove.Crypto.Tls` | 本仓 | TLS 探测引擎：协议 / 套件矩阵、扩展解析、评级、裸字节探测 | 同上 |

**注意**：消费本库的应用**不发布**（`IsPackable=false`）。本文档只涉及本仓产出的包。

> **已知偏差**（`RM-0.0.1`）：三处目标框架声明彼此不一致 —— `Directory.Build.props` 设 5 个，Core 覆盖为 `net8.0;net9.0;net10.0`，门包固定 `net10.0`。在它们统一之前，打包产物不可信。两个 `netstandard` 目标的现状见 `RM-0.0.11`。

---

## 2. 包边界原则

| # | 原则 |
|---|---|
| 1 | **数据格式层与网络协议层分离**：`DevTrove.Crypto` 只处理数据，不做任何网络访问 |
| 2 | **`DevTrove.Crypto.Tls` 依赖 `DevTrove.Crypto`**，不重复实现 ASN.1 / X.509 / PKCS 解析 |
| 3 | **不依赖应用层**：`DevTrove.Crypto.Tls` 自带结果模型，只依赖 `DevTrove.Crypto` |
| 4 | **核心库零框架依赖**：不引用 DI、日志、ASP.NET Core；日志由调用方决定 |
| 5 | **门面包与实现包分离**：使用方引用 `DevTrove.Crypto`；`DevTrove.Crypto.Core` 作为其依赖被自动引入，将来可替换实现而不破坏外部契约 |
| 6 | **纯托管**：不引入任何原生依赖，不打包 `runtimes/<rid>/native` |

### 依赖关系

```mermaid
flowchart LR
    BC["BouncyCastle.Cryptography"] --> CORE["DevTrove.Crypto.Core"]
    CORE --> FACADE["DevTrove.Crypto"]
    FACADE --> TLS["DevTrove.Crypto.Tls"]
    TLS --> APP["应用项目（不发布）"]
```

---

## 3. 版本策略

### 3.1 每个里程碑一个版本号

三个包**在每个里程碑内共用同一个版本号**，不存在每包独立版本：一个里程碑要么全部发布，要么只发布已经存在的包（`DevTrove.Crypto.Tls` 从 `0.4.0` 开始出现）。

| 场景 | 版本动作 |
|---|---|
| 新增 API（向后兼容） | Minor 递增 |
| 缺陷修复 | Patch 递增 |
| 破坏性变更 | Major 递增（`1.0.0` 之前允许在 Minor 中做破坏性变更，但需在 CHANGELOG 中显著标注） |
| 仅文档/注释变更 | 不递增（或在 Patch 中说明） |

### 3.2 依赖版本声明

`DevTrove.Crypto.Tls` 对 `DevTrove.Crypto` 声明**最低可兼容版本**：

```xml
<PackageReference Include="DevTrove.Crypto" Version="[1.2.0, )" />
```

- 使用**下界约束**（`[x.y.z, )`），允许消费方升级到兼容的更高版本
- 若某版本引入了不兼容变更，则同时提升下界并递增自身的 Major

### 3.3 预发布版本

早期版本使用 `-dev` / `-preview` 后缀（如 `0.3.0-dev`），避免被生产环境误引用。

### 3.4 版本线起点

`0.0.1`–`0.0.13` **仅为工作项编号**：不打包、不打 tag、不发布。首次真实发布为 `0.1.0`。`0.x` 期间允许破坏性变更，但必须在 CHANGELOG 中显著标注。见 [roadmap.md §3](roadmap.md)。

---

## 4. 包元数据要求（强制）

每个待发布的包**必须**在 `Directory.Build.props` 中声明以下属性：

| 属性 | 要求 |
|---|---|
| `PackageId` | `DevTrove.*` 命名 |
| `Version` | 显式声明，不使用隐式默认 |
| `Authors` / `Company` | 必填 |
| `Description` | 必填，说明该包做什么 |
| `PackageTags` | 便于检索（如 `tls`、`x509`、`crypto`、`sm2`、`sm3`、`sm4`、`gm`、`certificate`） |
| `PackageLicenseExpression` | `Apache-2.0`，**必须与仓库 `LICENSE` 文件一致** |
| `RepositoryUrl` / `RepositoryType` | `git` |
| `PackageProjectUrl` | 项目主页 |
| `PackageReadmeFile` | 指向随包发布的 README |
| `GenerateDocumentationFile` | `true` —— **否则包内不含 XML 文档，使用方无 IntelliSense 提示** |
| `IncludeSymbols` + `SymbolPackageFormat` | `true` + `snupkg`，便于调试 |
| `IsPackable` | `true` |

**建议**：启用 SourceLink，使使用方可直接跳转到源码。

> **已知偏差**（`RM-0.0.4`）：`<Version>`、`<PackageId>` 与 SourceLink **均未声明**，尽管上表称其为必需项。不写 `<Version>` 打包会静默产出 `1.0.0`。

### 常见错误

| 错误 | 后果 |
|---|---|
| 只写 `PackageId` 而漏掉 `Description` / `License` | 包页信息缺失，且可能被 nuget.org 校验拒绝 |
| 忘记 `GenerateDocumentationFile` | 包内无 XML 注释，DX 显著下降 |
| `PackageLicenseExpression` 与 `LICENSE` 文件不一致 | 许可证表述矛盾，存在合规风险 |
| 未声明 `PackageReadmeFile` 却引用了 README | 打包失败 |

---

## 5. 多目标框架（目标态）

三个包统一使用（目标态）：

```
netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0
```

| 目标 | 目的 |
|---|---|
| `netstandard2.0` | 覆盖 .NET Framework 4.6.2+、Unity 等宿主 |
| `netstandard2.1` | 覆盖较早的 .NET Core 3.x 宿主 |
| `net8.0` / `net9.0` | 覆盖当前的 LTS / STS 宿主 |
| `net10.0` | 应用主目标，可使用最新 API |

### `netstandard2.0` 注意事项

- 部分类型与 API 不可用，需条件编译或引入兼容包
- 恢复该目标后，此前被条件编译排除的文件会重新参与编译，**必须先单独实测**
- 测试矩阵需覆盖该目标（至少构建通过）

> 两个 `netstandard` 目标**从未产出过程序集**（`RM-0.0.11`）。在修好之前，打包只能产出三个框架的包，与上表描述不符。

---

## 6. 发布流程

### 6.1 本地打包

```
dotnet pack <project> -c Release -o ./artifacts --include-symbols
```

产出：`*.nupkg` + `*.snupkg`

### 6.2 CI 发布

| 触发 | 动作 |
|---|---|
| 推送到 `main` | 构建 + 测试（不发布） |
| Pull Request | 构建 + 测试（不发布） |
| 推送 `v*` 标签 | 构建 + 测试 + 打包 + 发布到 nuget.org（`--skip-duplicate`） |

**发布顺序**（`DevTrove.Crypto.Tls` 依赖 `DevTrove.Crypto`）：

1. `DevTrove.Crypto.Core`
2. `DevTrove.Crypto`
3. `DevTrove.Crypto.Tls`

由于 NuGet 不支持原子多包发布，新版本发布时应**先发包、后打标签**，或在 CI 中按上述顺序依次推送，避免出现"依赖已升级但被依赖的包尚未发布"的窗口期。

> **已知偏差**（`RM-0.0.6`）：`.github/workflows/build.yml` 的 `publish` job 当前用 `dotnet pack ... --no-build`，却缺少先行的 `dotnet build`；推送步的 glob `DevTrove.Crypto.*.nupkg` 也会匹配到 Core 包。

### 6.3 密钥管理

- API Key 通过 CI 的加密 Secret 注入
- **不**将 API Key 写入仓库、脚本或日志
- 定期轮换 API Key

---

## 7. 前缀预留

NuGet 支持申请 **Package ID 前缀预留**，防止他人在同一前缀下发布包（造成混淆或仿冒）。

申请 `DevTrove.*` 需要验证对该前缀的所有权（通常通过域名或仓库所有权验证）。

**建议**：先注册与产品名一致的域名，再申请前缀预留。

---

## 8. 发布产物验证清单

每次发布前逐项确认：

- [ ] `dotnet pack` 成功，产出 `.nupkg` 与 `.snupkg`
- [ ] 包内包含 XML 文档文件（`lib/<tfm>/*.xml`）
- [ ] 包内含 README（若声明了 `PackageReadmeFile`）
- [ ] `PackageLicenseExpression` 与仓库 `LICENSE` 一致
- [ ] 所有目标框架均有 `lib/<tfm>/` 目录
- [ ] **包内不含任何原生库**（无 `runtimes/*/native/`）
- [ ] 在干净环境（如 `netstandard2.0` 空项目）中还原并调用 API 成功
- [ ] 依赖声明为下界约束而非精确版本
- [ ] CHANGELOG 已更新（中英双份）
- [ ] 版本号符合语义化版本规则

---

## 9. 消费方式

使用方只需引用门面包：

```
dotnet add package DevTrove.Crypto     # 加密与证书能力
dotnet add package DevTrove.Crypto.Tls # TLS 探测能力（自动引入 DevTrove.Crypto）
```

`DevTrove.Crypto.Core` 作为 `DevTrove.Crypto` 的依赖自动引入，一般无需显式引用。

---

## 10. 使用方如何引用

| 场景 | 做法 |
|---|---|
| 引用包 | 对 `DevTrove.Crypto`（探测能力则加 `DevTrove.Crypto.Tls`）加 `PackageReference`，版本约束用下界，例如 `[0.1.0, )` |
| 联调未发布版本 | 发布到本地文件夹 feed，让消费方指向它 |

消费方如何在项目引用与包引用之间取舍，属消费方决策，本文档有意不涉及。**注意**：`ProjectReference` 不会自动变成 NuGet 依赖 —— 包必须基于 `PackageReference` 元数据产出，否则依赖声明缺失，使用方会还原失败。

---

## 11. 相关文档

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 包在整体架构中的位置与依赖方向 |
| [standards.md](standards.md) | 工程文件与元数据规范 |
| [roadmap.md](roadmap.md) | 版本线、逐项状态与证据 |
| [development-guide.md](development-guide.md) | 构建/测试/打包命令 |
| [library-api.md](library-api.md) | 公开 API 索引 |
