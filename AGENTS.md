# AGENTS.md

本文件面向 AI 编码代理，说明本仓库 **DevTrove.Crypto**（NuGet 库）的权威文档、不可违反的约束与提交前检查。
人类贡献者同样应阅读，但完整规范以 [`docs/standards.md`](docs/standards.md) 为准。

---

## 1. 仓库定位

`DevTrove.Crypto` 是**独立发布、独立版本号**的纯托管 .NET 密码学与证书库（BouncyCastle 封装），发行四个 NuGet 包：

| 包 | 角色 |
|---|---|
| `DevTrove.Crypto.Abstractions` | **契约**：对称 / 非对称 / 摘要 / X.509 四面的接口与抽象基类；**零包依赖**（工作项 `RM-0.0.14`；随首个发布 `0.1.0` 出货） |
| `DevTrove.Crypto.Core` | **实现**：BouncyCastle 封装（算法原语、密钥、ASN.1、X.509、CSR、CRL、PKCS#12）；依赖 Abstractions |
| `DevTrove.Crypto` | **门面包（metapackage）**：仅 `ProjectReference` → Core，对外只传递依赖 |
| `DevTrove.Crypto.Tls` | **TLS 探测引擎**（计划中，`0.6.0`；当前仅有预留命名空间） |

技术栈（版本以 [`Directory.Packages.props`](Directory.Packages.props) 为唯一权威）：

| 层 | 技术 |
|---|---|
| 目标框架 | `netstandard2.0;net8.0;net9.0;net10.0`（`netstandard2.1` 已废弃 —— 无未 EOL 宿主可解析该资产） |
| 核心依赖 | BouncyCastle.Cryptography |
| 测试 | xUnit + FluentAssertions + CliWrap；互操作依赖 **tongsuo**（唯一外部工具） |

仓内布局：

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto.Abstractions/  契约（零依赖，工作项 `RM-0.0.14`）
│  ├─ DevTrove.Crypto/              门面包
│  ├─ DevTrove.Crypto.Core/         实现
│  └─ DevTrove.Crypto.Tls/          TLS 探测引擎（计划中 `0.6.0`）
├─ tests/
│  ├─ DevTrove.Crypto.Abstractions.Tests/  契约测试（仅 Windows 含 `net48`）
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/
├─ scripts/                     夹具生成脚本
└─ docs/                        开发文档（英文默认 + `.zh-CN.md` 中文对照）
```

本仓库**独立发布、独立版本号**，可脱离任何消费方单独构建、测试与发布。
文档自洽：库自身设计的所有问题（架构、规范、包规划、TLS 探测引擎、版本线、测试策略）均在本仓 `docs/` 内回答，不引用任何外部仓库的路径或章节。

---

## 2. 权威文档（冲突时以此为准）

`docs/` 下每份文档成对存在：英文默认入口（`x.md`）+ 中文对照（`x.zh-CN.md`），章节一一对应。
列表均指向英文默认版，中文版请访问同名 `.zh-CN.md`。

| 文档 | 内容 |
|---|---|
| [docs/architecture.md](docs/architecture.md) | 库内分层、依赖方向、能力边界、已知限制 |
| [docs/standards.md](docs/standards.md) | 编码规范、命名、Git 流程、评审清单 |
| [docs/nuget.md](docs/nuget.md) | 包边界、版本策略、发布流程 |
| [docs/tls-scanner.md](docs/tls-scanner.md) | TLS 探测引擎设计 |
| [docs/roadmap.md](docs/roadmap.md) | 库的阶段路线 + 待办 / 已知偏差 |
| [docs/development-guide.md](docs/development-guide.md) | 构建/测试/打包命令、TFM 策略、polyfill 规则 |
| [docs/library-api.md](docs/library-api.md) | 公开 API 索引（与反射清单对齐） |

---

## 3. 语言策略（强制）

| 对象 | 语言 |
|---|---|
| `docs/*.md` | **英文（默认入口）** |
| `docs/*.zh-CN.md` | **中文**，与英文版章节、链接一一对应，顶部互相链接 |
| `README.md` / `CHANGELOG.md` | **英文（默认入口）** |
| `README.zh-CN.md` / `CHANGELOG.zh-CN.md` | 中文，**与英文版章节、链接一一对应** |
| `AGENTS.md`（本文件） | **中文** |
| 代码注释、XML 文档注释 | **中文**（国际化列入待办） |
| 日志消息、异常消息、代码标识符 | **英文**（异常消息句尾加句号） |
| Git 提交信息 | **英文类型前缀 + 中文描述** |

**规则**：英文默认版与 `.zh-CN.md` 中文版必须保持章节编号、表格、Mermaid、代码示例一一对应；
文内互链**同语互链**（英文版指向 `x.md`、中文版指向 `x.zh-CN.md`）。

---

## 4. 不可违反的约束

1. **门面包 = 元包**：`DevTrove.Crypto` 项目**无源码**，仅 `ProjectReference` → `DevTrove.Crypto.Core`
2. **零框架依赖**：核心库**不引用** DI、日志、ASP.NET Core、`Microsoft.Extensions.*`
3. **不打日志**：核心库**不产生**日志，由调用方决定如何记录
4. **命名空间与项目名一致**：根命名空间 `DevTrove.Crypto`；`DevTrove.Crypto.Core` 项目 → `DevTrove.Crypto.*` 命名空间
5. **4 TFM 策略**：`netstandard2.0;net8.0;net9.0;net10.0`。每个交付目标必须有在 CI 中真正运行它的宿主（`netstandard2.0` 靠 Windows 上的 `net48` 测试项目）
6. **依赖方向单向**：`DevTrove.Crypto.Core` → `DevTrove.Crypto.Abstractions`；`DevTrove.Crypto.Tls` → `DevTrove.Crypto.Core`；**禁止反向**，且 `Abstractions` 不得引用任何包（含 BouncyCastle）
7. **库内禁止** `.Result` / `.Wait()` / 空 `catch` / `Task.Run` 伪造异步
8. **库不得知道消费方**：可独立发布；不引用任何外部仓库的文档路径或章节号

---

## 5. 常用命令

```bash
# 构建（Release 多 TFM）
dotnet build DevTrove.Crypto.slnx -c Release

# 契约测试（无外部依赖；Linux 上必须显式指定 --framework）
dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0

# 全部测试（互操作需 tongsuo；缺失时直接失败而非跳过）
dotnet test tests/DevTrove.Crypto.Core.Tests -c Release --framework net10.0

# 仅核心库单元测试（跳过互操作）
dotnet test tests/DevTrove.Crypto.Core.Tests -c Release --framework net10.0 --filter 'Category!=Integration'

# 打包（按依赖顺序：Abstractions → Core → Crypto）
dotnet pack src/DevTrove.Crypto.Abstractions/DevTrove.Crypto.Abstractions.csproj -c Release -o ./artifacts
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts

# 生成 PFX 测试夹具（CI 也跑）
./scripts/generate-test-pfx.sh
```

外部依赖：`tongsuo`（互操作测试必需；缺失时测试**直接失败**而非跳过）。

---

## 6. 提交前检查

- [ ] `dotnet build` 通过（4 个 TFM 全绿；每新增类型应有中文 XML 注释）
- [ ] `dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0` 全绿
- [ ] `dotnet test tests/DevTrove.Crypto.Core.Tests -c Release --framework net10.0` 全绿（或明确标注失败用例为预存缺陷）
- [ ] 新增/修改的 public 成员有中文 XML 文档注释
- [ ] 触及 `README` / `CHANGELOG` 时，同步对应的 `.zh-CN.md`
- [ ] 触及 `docs/*.md` 时，同步对应 `*.zh-CN.md`（章节、表格、Mermaid、代码示例一一对应）
- [ ] 触及架构/包/命名/TFM/测试策略时，本仓 `docs/*.md` 与 `*.zh-CN.md` 内自洽
- [ ] 变更了 [docs/roadmap.md](docs/roadmap.md) 中任何条目的状态，已**在同一次提交内**更新该状态（中英两份）
- [ ] 日志与异常中**无密钥材料、口令、输入原文**

---

## 7. Git 规范

- 分支：`main` 保持可构建；功能用 `feature/<slug>`、修复用 `fix/<slug>`、仅文档用 `docs/<slug>`
- 提交信息：`<type>(<scope>): <中文描述>`，type ∈ `feat|fix|docs|refactor|test|build|chore|perf`
- 一次提交只做一件事；不混入无关格式化改动

---

## 8. 禁止事项

- ❌ 提交 `bin/`、`obj/`、`artifacts/`、`.vs/`、`.vshistory/`、`*.pfx`
- ❌ 把 `tests/data/` 强行纳入版本控制（它是生成物，见 `architecture.md` D18）；抓取类夹具放 `tests/fixtures/`
- ❌ 使用 `git add -A` / `git add .`（夹具与残留极易被误提交）
- ❌ 在 `docs/` 写英文文档、在 `README.zh-CN.md` 里省略英文版已有的章节
- ❌ 声称未实现的能力（文档与代码必须一致）
- ❌ 在核心库内打日志或抛出中文异常消息
- ❌ 在 `DevTrove.Crypto` 门面包内放置任何 API 类型
- ❌ 在 `DevTrove.Crypto.Abstractions` 内引用任何包（它必须保持零依赖，BouncyCastle 也不例外）
- ❌ 在 shell 脚本注释（`#` 行）或 `<< 'EOF'` 单引号 here-doc 中嵌入 `${VAR}` / `$VAR` 占位符：注释与单引号 here-doc **不会**被 shell 展开，写入后是字面文本，会误导读者；要引用工具名请直接写（如 `tongsuo`）
- ❌ 在面向用户的提示文本（`echo` 提示、生成的 README 等）中嵌入带路径的变量（如 `${TONGSUO_BIN}`）：会展开为绝对路径，用户无法直接复制使用；应使用命令名（`tongsuo`）
- ❌ 在代码与脚本的注释里嵌入 `roadmap.md` 的条目编号（如 `RM-0.0.9a`、`RM-0.0.9a/9e`）：roadmap 是文档内部追踪项，外泄到 `.cs` / `.csproj` / `.sh` / `.yml` / `.props` 的注释里会让条目号随版本变动而失真；roadmap ↔ 代码的对应关系由 `docs/roadmap.md` 自身维护，不在源码里反向引用

---

## 9. 相关文档

| 文档 | 内容 |
|---|---|
| [docs/standards.md](docs/standards.md) | 编码规范、Git 流程、评审清单 |
| [docs/architecture.md](docs/architecture.md) | 库内分层、依赖方向、能力边界 |
| [docs/nuget.md](docs/nuget.md) | 包边界、版本策略、发布流程 |
| [docs/tls-scanner.md](docs/tls-scanner.md) | TLS 探测引擎设计 |
| [docs/roadmap.md](docs/roadmap.md) | 库的阶段路线 + 待办 / 已知偏差 |
| [docs/development-guide.md](docs/development-guide.md) | 构建/测试/打包 |
| [docs/library-api.md](docs/library-api.md) | 公开 API 索引 |
