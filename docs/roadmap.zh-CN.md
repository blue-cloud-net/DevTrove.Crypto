# 库路线图

> 英文默认入口：[roadmap.md](roadmap.md)

本文档是本库的阶段路线，**独立于**消费方应用仓的 roadmap。仅当任务需要在本仓内完成时，跨仓任务才在此记录。

---

## 1. 版本策略

遵循 SemVer。`0.x` 阶段允许破坏性变更；`1.0.0` 起严格遵循。

| 版本 | 阶段 | 主题 |
|---|---|---|
| `0.0.1-dev` | — | 启动；TFM 修复（B1）、门包清理（B2） |
| `0.0.X-dev` | Phase A 延续 | 能力迭代 |
| `0.1.0` | Phase A 完成 | 5 TFM、门包无源码、OCSP 解析、RFC 8998 验证 |
| `0.2.0` | Phase 3 | TLS 探测 L1 + NTLS 指纹 |
| `0.3.0` | Phase 4 | TLS 探测 L2 + 国密 |
| `1.0.0` | Phase 5 | 稳定 API + 评级文档化 |

库**独立版本号**，不与应用版本对齐；消费方声明最低可兼容版本（见 [nuget.md §3.2](nuget.md)）。

---

## 2. 能力现状

### Phase A — 核心库改造

**目标**：把本仓库从应用型仓库改造为**纯类库**仓库，可独立发布 NuGet。

**起点**：仓内含 `Crypto.Utils.Core`（BC 封装，保留）、`Crypto.Utils.Api`、`Crypto.Utils.Host`、`Crypto.Utils.UI`（后三者待删除）。

**目标形态**：以**单一子模块** `lib/Crypto` 挂载到应用仓；仓内保留 `src/` 分层，三个项目对应三个 NuGet 包 —— `DevTrove.Crypto`（门面包）、`DevTrove.Crypto.Core`（实现）、`DevTrove.Crypto.Tls`（TLS 探测引擎，Phase A 建骨架、Phase 3 起实现）。全部项目、程序集与命名空间由 `Crypto.Utils.*` 重命名为 `DevTrove.Crypto.*`。

### 任务

| # | 任务 | 依赖 |
|---|---|---|
| A1 | 删除 `src/Crypto.Utils.{Api,Host,UI}` 及相关包引用、解决方案条目 | — |
| A2 | **迁移 7 项逻辑进 Core**，并修复 4 个已知缺陷（见下） | A1 |
| A3 | 新增门面包项目（`DevTrove.Crypto`，稳定对外 API，`Core` 降为内部实现） | A2 |
| A4 | 补 NuGet 元数据 + `GenerateDocumentationFile` + `global.json` + CI；统一 TFM；测试补 `net9.0` | A3 |
| A5 | 清理死代码；SM2 夹具提交入库；CI 增加 PFX 夹具生成 | A1 |
| A6 | 文档重写为库模式 + 新增双语 README/CHANGELOG + `docs/library-api.md` | A1、A2 |
| A7 | 新增单元测试；`dotnet pack` 产物验证 | A2、A4 |

### 删 Api 前必须迁移的 7 项

| 迁移内容 | 目标位置 |
|---|---|
| 证书链构建 + 验证 | `CertificateChainBuilder` / `CertificateChainVerifier` |
| DN 字符串构建器（结构化字段 → DN） | `X509DistinguishedName` 增加构建方向 |
| 随机序列号生成 | `CertificateUtils.GenerateSerialNumber()` |
| 数据类型自动识别 + PEM/DER 统一转换 | `FormatUtils` |
| 吊销原因字符串 → 枚举解析 | `CertificateRevocationReasonHelper` |
| 证书验证判定（有效期 + 签名 + 链 + 信任根） | 与链验证合并 |
| **OCSP 响应解析**（当前完全缺失） | `X509.Ocsp`（`DevTrove.Crypto.Tls` 需要） |

### 必须顺手修复的 4 个缺陷

| # | 缺陷 | 修法 |
|---|---|---|
| 1 | CSR 生成中手工重写"私钥→公钥"推导，**缺 DSA 分支**，EC 分支漏 `.Normalize()` | 改用 Core 已有的 `AsymmetricPrivateKeyParameter.GetPublicKey()` |
| 2 | 格式转换在 `Task.Run` 内用 `.Result` 调用自身的异步方法 | 改为同步实现，消除阻塞/死锁反模式 |
| 3 | 默认签名算法写死 RSA（`SHA256WITHRSA`） | 改为按私钥算法推导默认值 |

### 验收标准

- [ ] 5 个目标框架（`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`）均能编译（当前失败 —— 见 B1）
- [ ] `dotnet test` 全绿（含 `net9.0` 一旦补上 —— 见 B11）
- [ ] `dotnet pack` 产物包含 XML 文档与完整元数据
- [ ] 在 `netstandard2.0` 空项目中还原该包并成功调用 API（待 B3 polyfill 完成）
- [ ] `LICENSE`（Apache-2.0）与 `PackageLicenseExpression` 一致
- [ ] 仓库内不再引用铜锁（当前仍引用 —— 见 B4）
- [ ] 文档与实际代码能力一致（清理"宣称但未实现"的功能描述）

### 风险

| 风险 | 缓解 |
|---|---|
| 恢复 `netstandard2.0` 目标后，条件编译文件（使用 C# 14 `extension(...)` 语法）可能无法编译 | A4 中先单独实测；不可行则移除该文件 |
| 链验证迁移后行为与 Api 层不一致 | 用真实站点证书链与自签 CA→leaf 链做回归 |
| 文档漂移（原文档宣称的 JWT/哈希/在线检测等能力实际不存在） | A6 中逐条核对并删除不实描述 |

---

## 3. Phase 0（应用仓）—— WASM 验证 + 主仓骨架

**目标**：验证 WebAssembly 可行性，建立主仓代码骨架，打通最小闭环。

本库在此阶段的职责：

| # | 任务 | 责任方 |
|---|---|---|
| 0 | WASM 可行性验证（应用仓决定） | 应用仓 |
| 1 | 主仓骨架：`global.json`、`Directory.Build.props`、`Directory.Packages.props`、`.editorconfig`、`.slnx` | 应用仓 |
| 2 | 在 `lib/Crypto` 子仓内新建 `src/DevTrove.Crypto.Tls` 骨架（引用 `DevTrove.Crypto`） | **本库** |
| 3 | 打通最小闭环：工具抽象 + 示例工具（Base64）+ `DevTrove.Services` 装配 + Host 暴露 + Web.Client 渲染 + Desktop 进程内 | 应用仓 |

### 库任务 #2 细节

- 创建 `src/DevTrove.Crypto.Tls/` 项目：`ProjectReference` → `DevTrove.Crypto.Core`；命名空间 `DevTrove.Crypto.Tls.*`
- `DevTrove.Crypto.Tls` 对 `DevTrove.Crypto` 声明最低版本依赖（见 [nuget.md §3.2](nuget.md)）
- `Directory.Build.props` 自动覆盖新项目；slnx 包含
- 初期无源码：仅项目骨架 + [architecture.md §12](architecture.md) 中公开类型接口定义

---

## 4. Phase 1–2（应用仓）—— 通用工具、加密 / 证书工具

**本库职责**：无变更。Core 实现由应用仓的 `DevTrove.Core` / `DevTrove.Services` 消费。

本阶段可能浮现的库侧问题（每条在此跟踪）：

| 项 | 触发条件 |
|---|---|
| OCSP 解析边界用例被加密工具上报 | 任何测试失败即新增条目 |
| DN 构建方向扩展 | Phase A2 —— 验证行为符合预期 |
| 格式识别辅助（PEM/DER/...） | Phase A2 —— 用应用仓样例验证 |

---

## 5. Phase 3 —— TLS 探测 L1 + NTLS 检测

**目标**：交付 TLS 探测 L1 与国密 NTLS 指纹检测。

| # | 任务 | 责任方 |
|---|---|---|
| 1 | `DevTrove.Crypto.Tls/TlsProbe`：协议矩阵、套件矩阵、扩展指纹、服务端实发证书链、协商群/签名算法 | **本库** |
| 2 | `DevTrove.Crypto.Tls/TlsRaw`：裸字节路径 —— NTLS 指纹检测、ROBOT oracle、SSLv2 ClientHello | **本库** |
| 3 | `INetworkProbeService` 进程内实现与 HTTP 实现 + `Tools/Network` | 应用仓 |
| 4 | Host 安全基线：限流、SSRF、请求大小限制、超时、CORS 白名单 | 应用仓 |

### 验收

- [ ] 协议矩阵对 `badssl.com` 系列判定与 `openssl s_client` 一致
- [ ] 套件矩阵能枚举并识别弱套件
- [ ] 扩展指纹能识别 SCT、OCSP staple、EMS、ALPN、session ticket、secure renegotiation
- [ ] 服务端实发证书链顺序与 `openssl s_client -showcerts` 一致
- [ ] SSRF 用例（`127.0.0.1`、`10.x`、`169.254.169.254`、DNS rebinding）全部被拒（应用仓）
- [ ] NTLS 指纹检测对抓取的公开国密站点字节夹具判定正确
- [ ] 服务端异常扩展不会导致整场扫描失败（降级为记录异常）

---

## 6. Phase 4 —— TLS 探测 L2 + 国密

**目标**：交付评级、客户端模拟与国密 TLS 完整能力。

| # | 任务 | 责任方 |
|---|---|---|
| 1 | A~F 评级算法（参考公开思路，独立实现） | **本库** |
| 2 | 客户端模拟矩阵（Chrome / Firefox / Safari / Edge / Java / Android） | **本库** |
| 3 | ALPN/HTTP2 探测、CT 日志查询、DNS CAA 校验 | 混合 |
| 4 | RFC 8998（SM2-TLS 1.3）完整握手（依赖 V3 验证结论） | **本库** |
| 5 | 国密套件矩阵与国密站点专项报告 | **本库** |

### 验收

- [ ] 评级结果对测试站点合理且可解释（每个扣分项给出依据）
- [ ] 客户端模拟矩阵判定与各浏览器实际握手能力一致
- [ ] RFC 8998 完整握手成功（若 V3 可行）
- [ ] 国密站点报告能列出协商套件、双证书、签名算法、曲线

### 待 v2 评估

NTLS **完整握手**自研：需要实现 record 层、SM3 PRF、SM2 密钥交换、SM4 记录保护、双证书处理与 Finished 校验。仅在确有"模拟国密客户端"需求时启动。

---

## 7. 待办 / 已知偏差

制定本路线时发现的偏差；每条均附下一步。

| # | 文档 / 宣称 | 实际状态 | 下一步 |
|---|---|---|---|
| B1 | 5 TFM 全绿 | `Directory.Build.props` 全局设 5 TFM；Core csproj 覆盖为 `net8.0;net9.0;net10.0`；门包 `net10.0` → `slnx` 构建 **NU1201** | 统一 csproj `<TargetFrameworks>` 与全局集合；逐 TFM 单独验证 |
| B2 | 门面包无源码 | `src/DevTrove.Crypto/Program.cs` 仍在 | 删除该文件；确保门包项目无源码（仅 `ProjectReference`） |
| B3 | `netstandard2.0` polyfill 在 `Compat/` | `src/DevTrove.Crypto.Core/Compat/` **不存在**（README 中英双份都如此宣称） | 决定：放弃 `netstandard2.0` 目标，或实际创建 `Compat/` 与所需 polyfill |
| B4 | 已移除国密 CLI 依赖 | `tests/DevTrove.Crypto.TestSupport/TongsuoCli.cs` 仍在；`tests/data/*/README.md` 引用 tongsuo 与**不存在**的 `scripts/generate-test-sm-certs.sh` | 选其一：用 OpenSSL 重新生成 SM2 夹具（删 `TongsuoCli` 与脚本引用）；或保留 `TongsuoCli` 并补上缺失脚本 |
| B5 | `tests/data/` 已有 `ocsp/`、`ntls/` 夹具 | 实际只有 `certs/ crls/ csrs/ keys/ pfx/` | 添加空目录与首次运行抓取脚本；或删除该宣称 |
| B6 | 开发期 `ProjectReference` / 打包期 `PackageReference` 由 `Directory.Build.props` 条件属性切换 | 该条件属性**不存在** | 引入属性（`<UseCryptoProjectRef>true/false</UseCryptoProjectRef>` 等）并改造消费方 csproj；在 [development-guide.md §4](development-guide.md) 文档化 |
| B7 | CI 发布流程 | `build.yml` publish 缺 build 却 `--no-build`；推送 glob `DevTrove.Crypto.*.nupkg` 重复匹配 Core | publish 中加 `dotnet build` 步；metapackage 推送 glob 改为 `DevTrove.Crypto.*[!Core]*.nupkg` 或按确切文件名推 |
| B8 | `docs/library-api.md`、`docs/standards.md` 存在 | 不存在（README/AGENTS 已引用） | 本次修订中已新建（Phase 1g、1b） |
| B9 | 「单一子模块 `lib/Crypto`」 | 无 `.gitmodules`；父仓 `.gitignore` 仍忽略 `lib/Crypto/`（应用仓 Stage 0） | 文档如实写 Stage 0 现状；正式 `git submodule add` 由应用仓决策（不在本库范围） |
| B10 | 子仓文档为库模式 | `docs/{architecture,development-guide,roadmap}.md`+`v0.1/*` 仍是 Web 时代口径（写 `Crypto.Utils.UI/Api/Host`） | 本次修订已重写（Phase 1a、1f、1e）；`v0.1/` 删除（Phase 1h） |
| B11 | 测试已补 `net9.0`、xUnit v3 | TestSupport csproj 缺 `net9.0`；xUnit 2.9.2 | TestSupport 加 `net9.0`；计划升级 xUnit v3（影响断言风格） |
| B-cmnt | 代码注释与 XML 注释语言策略 | 当前中文；国际化未做 | 国际化列入路线 —— 一旦消费方有需要则改英文（或双语）并做一次 Major 版本号 |

---

## 8. 明确排除（Out of Scope）

| 项 | 原因 |
|---|---|
| 用户账号体系与登录 | 无服务端状态设计 |
| 服务端持久化（数据库、历史记录） | 隐私优先，不落盘用户输入 |
| 支付与商业化 | 当前阶段不涉及 |
| 原生依赖包（铜锁 P/Invoke） | 与"纯托管"目标冲突，且 WebAssembly 下不可用 |
| L3 漏洞探测（Heartbleed / CCS Injection / Ticketbleed 等） | 需自研 record 层 + 密钥派生；仅 ROBOT 在范围内 |
| NTLS 完整握手 | 需自研 TLS 1.2 子集；列 v2 评估 |
| 打包 `testssl.sh` | GPLv2；仅作可选外部交叉验证工具 |

---

## 9. 风险登记

| # | 风险 | 影响 | 缓解 |
|---|---|---|---|
| R1 | WebAssembly 下 BouncyCastle 部分能力不可用 | 本地计算能力受限，需回退到服务端 | 应用仓 Phase 0 V1/V2 提前验证；不可用时把受影响工具改为 `ServerProxy` 并明确提示 |
| R2 | 首屏体积超出预期（>2MB gzip） | 首次访问体验差 | 惰性程序集 + 裁剪 + Brotli + PWA；实测后再评估混合渲染方案（应用仓） |
| R3 | 服务端探测接口被滥用 | 资源耗尽、被当作攻击跳板 | 限流 + SSRF + 大小限制 + 超时 + 无状态设计（应用仓） |
| R4 | NTLS 国标细节无法确认 | 检测判定可能不准确 | 以公开国密站点真实字节为夹具回归；不确定项显式标注 |
| R5 | 证书链验证精简实现被误认为完备 | 用户低估风险 | UI 与文档明确标注「精简版」；PKIX 升级在路线上 |
| R6 | 两套 UI 导致行为不一致 | 体验割裂 | schema 驱动 + 共享 Core；双端各一个冒烟测试；业务逻辑不写在 UI 层（应用仓） |
| R7 | 库结果模型与消费方契约模型不一致 | 映射错误风险 | 库自带模型；消费方显式映射；[tls-scanner.md §12](tls-scanner.md) 列出实体 |

---

## 10. 相关文档

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 分层、依赖方向、能力边界 |
| [standards.md](standards.md) | 编码 / 测试 / Git 规范 |
| [nuget.md](nuget.md) | 包边界、版本策略、发布流程 |
| [tls-scanner.md](tls-scanner.md) | TLS 探测引擎设计 |
| [development-guide.md](development-guide.md) | 构建/测试/打包/CI |
| [library-api.md](library-api.md) | 公开 API 索引 |
