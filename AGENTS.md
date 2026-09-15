# AGENTS.md

本文件面向 AI 编码代理，说明本仓库 **DevTrove.Crypto**（NuGet 库）的权威文档、不可违反的约束与提交前检查。
人类贡献者同样应阅读，但完整规范以 [`docs/standards.md`](docs/standards.md) 为准。

---

## 1. 仓库定位

`DevTrove.Crypto` 是 DevTrove 工具箱的**核心库仓库**（子仓），发行三个 NuGet 包：

| 包 | 角色 |
|---|---|
| `DevTrove.Crypto` | **门面包（metapackage）**：仅 `ProjectReference` → Core，对外只传递依赖 |
| `DevTrove.Crypto.Core` | **实现**：BouncyCastle 封装（算法原语、密钥、ASN.1、X.509、CSR、PKCS#7/#12、CRL、OCSP） |
| `DevTrove.Crypto.Tls` | **TLS 探测引擎**（暂仅预留命名空间；Phase 0 建骨架） |

仓内布局：

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/         门面包
│  ├─ DevTrove.Crypto.Core/    实现
│  └─ DevTrove.Crypto.Tls/     TLS 探测引擎（Phase 0 引入）
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/
├─ scripts/                     夹具生成脚本
└─ docs/                        开发文档（英文默认 + `.zh-CN.md` 中文对照）
```

应用仓以**单一子模块** `lib/Crypto` 挂载本仓，跟踪 `dev` 分支。**本仓库独立发布独立版本号**，
文档自洽：库内部设计的所有问题（架构、规范、包规划、TLS 探测引擎、路线、测试策略）均在本仓 `docs/` 内回答，不反向引用应用仓。

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
5. **5 TFM 策略（目标）**：`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`（当前实际见 [docs/roadmap.md](docs/roadmap.md) 的「待办 / 已知偏差」节）
6. **依赖方向单向**：`DevTrove.Crypto.Tls` 可引用 `DevTrove.Crypto.Core`；**禁止反向**
7. **库内禁止** `.Result` / `.Wait()` / 空 `catch` / `Task.Run` 伪造异步
8. **不反向依赖应用仓**：库可独立发布；不引用父仓文档路径或章节号

---

## 5. 常用命令

```bash
# 构建（Release 多 TFM）
dotnet build DevTrove.Crypto.slnx -c Release

# 测试（含 openssl/tongsuo 互操作；缺失时直接失败而非跳过）
dotnet test DevTrove.Crypto.slnx -c Release

# 仅核心库测试（跳过互操作）
dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category!=Integration'

# 打包（按顺序：Core → Crypto）
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts

# 生成 PFX 测试夹具（CI 也跑）
./scripts/generate-test-pfx.sh
```

外部依赖：`openssl`（3.x，互操作测试必需；缺失时测试**直接失败**而非跳过）。

---

## 6. 提交前检查

- [ ] `dotnet build` 通过（当前允许警告，但每新增类型应有中文 XML 注释）
- [ ] `dotnet test` 全绿（或明确标注失败用例为预存缺陷）
- [ ] 新增/修改的 public 成员有中文 XML 文档注释
- [ ] 触及 `README` / `CHANGELOG` 时，同步对应的 `.zh-CN.md`
- [ ] 触及 `docs/*.md` 时，同步对应 `*.zh-CN.md`（章节、表格、Mermaid、代码示例一一对应）
- [ ] 触及架构/包/命名/TFM/测试策略时，**不反向同步父仓**；本仓 `docs/*.md` 与 `*.zh-CN.md` 内自洽
- [ ] 日志与异常中**无密钥材料、口令、输入原文**

---

## 7. Git 规范

- 分支：`main` 保持可构建；功能用 `feature/<slug>`、修复用 `fix/<slug>`、仅文档用 `docs/<slug>`
- 提交信息：`<type>(<scope>): <中文描述>`，type ∈ `feat|fix|docs|refactor|test|build|chore|perf`
- 一次提交只做一件事；不混入无关格式化改动

---

## 8. 禁止事项

- ❌ 提交 `bin/`、`obj/`、`artifacts/`、`.vs/`、`.vshistory/`、`*.pfx`
- ❌ 使用 `git add -A` / `git add .`（夹具与残留极易被误提交）
- ❌ 在 `docs/` 写英文文档、在 `README.zh-CN.md` 里省略英文版已有的章节
- ❌ 声称未实现的能力（文档与代码必须一致）
- ❌ 在核心库内打日志或抛出中文异常消息
- ❌ 在 `DevTrove.Crypto` 门面包内放置任何 API 类型

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
