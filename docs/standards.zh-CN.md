# 开发规范

> 英文默认入口：[standards.md](standards.md)

本文档定义 `DevTrove.Crypto` 的编码规范、工程约定与协作流程。所有贡献者（含 AI 助手）在提交代码前必须遵守。

---

## 1. 语言策略

权威版见 [AGENTS.md](../AGENTS.md) §3。速查：

| 对象 | 语言 |
|---|---|
| `docs/*.md` | **英文（默认入口）** |
| `docs/*.zh-CN.md` | **中文**，与英文版一一对应 |
| `README.md` / `CHANGELOG.md` | **英文（默认入口）** |
| `README.zh-CN.md` / `CHANGELOG.zh-CN.md` | **中文** |
| `AGENTS.md`（本仓） | **中文** |
| 代码注释、XML 文档注释 | **中文**（国际化当前未排期） |
| 日志消息 | **英文** |
| 异常消息 | **英文，句尾加句号** |
| 代码标识符 | 英文 |
| Git 提交信息 | **英文类型前缀 + 中文描述** |

---

## 2. 工程文件规范

### 2.1 仓根文件

| 文件 | 作用 |
|---|---|
| `DevTrove.Crypto.slnx` | 解决方案（XML 格式，非旧版 `.sln`） |
| `global.json` | 锁定 .NET SDK 版本（`rollForward: latestMajor`） |
| `Directory.Build.props` | 全局编译属性（TFM、可空性、语言版本、NuGet 元数据） |
| `Directory.Packages.props` | 集中包版本管理（CPM） |
| `.editorconfig` | 代码风格 |
| `.gitignore` | 忽略规则 |
| `.gitattributes` | 行尾规范化 —— **尚未存在**，见 `RM-0.0.13` |
| `AGENTS.md` | 代理与贡献者入口：权威文档索引、硬性约束、提交前检查 |

### 2.2 集中包管理（CPM）

所有包版本**只能**在 `Directory.Packages.props` 中声明：

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
</PropertyGroup>
<ItemGroup>
  <PackageVersion Include="BouncyCastle.Cryptography" Version="2.6.2" />
</ItemGroup>
```

各项目只写 `<PackageReference Include="..." />`，**不得**写 `Version` 属性。

### 2.3 全局编译属性

```xml
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

库项目（`IsPackable=true`）另需：

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<IsPackable>true</IsPackable>
```

> **已知偏差**（`RM-0.0.3`）：本文件写明了 `TreatWarningsAsErrors` 与 `EnforceCodeStyleInBuild`，但 `Directory.Build.props` 中**并不存在**这两个属性。要么补属性，要么改文档 —— 两者不能继续不一致。

### 2.4 行尾与编码

| 类型 | 设置 |
|---|---|
| 所有文件 | UTF-8、LF、文件末尾保留换行、去除行尾空白 |
| `*.cs` | 4 空格缩进、Allman 大括号（`csharp_new_line_before_open_brace = all`） |
| `*.{xml,csproj,props,targets,slnx}` | 2 空格缩进 |
| `*.{json,yml,yaml}` | 2 空格缩进 |
| `*.md` | 保留行尾空白（Markdown 换行语义） |

> **已知偏差**（`RM-0.0.13`）：`.editorconfig` 当前设的是 `end_of_line = crlf` 与 `insert_final_newline = false`，且没有为 Markdown、XML、JSON、YAML 定义任何段落。**本表是权威** —— 改的是配置文件，不是这张表。

---

## 3. C# 编码规范

### 3.1 文件结构

- **一个文件一个顶级类型**（`enum` 与其扩展方法可同文件）
- **强制文件作用域命名空间**：

  ```csharp
  namespace DevTrove.Crypto.X509;
  ```

- 命名空间必须与目录结构对应
- `using` 放在文件顶部（`ImplicitUsings` 已启用的不必显式引入），`System.*` 优先

### 3.2 类型设计

| 规则 | 说明 |
|---|---|
| 默认 `sealed` | 除非明确为继承而设计 |
| 不可变优先 | 数据模型用 `record` 或只读属性 |
| `record` 用于值语义 | DTO、结果模型、元数据 |
| `class` 用于有行为与生命周期的对象 | 服务、执行器、引擎 |
| 优先 `IReadOnlyList<T>` / `IReadOnlyDictionary<TKey, TValue>` | 公开 API 不暴露可变集合 |
| 不公开 `List<T>` / `Dictionary<TKey, TValue>` 作为返回类型 | 防止调用方篡改内部状态 |

### 3.3 可空性

- `Nullable=enable`，不允许 `#nullable disable`
- 公共 API 参数用 `ArgumentNullException.ThrowIfNull(x)` 显式校验
- 不用 `!`（null-forgiving）压制警告；确需时加注释说明原因

### 3.4 异步编程

- 异步方法以 `Async` 结尾，返回 `Task` / `Task<T>` / `ValueTask`
- **所有公开异步方法接受 `CancellationToken ct = default`**，并向下传递
- 库代码中的 `await` 加 `.ConfigureAwait(false)`
- 同步可完成的热路径用 `ValueTask<T>`
- **禁止** `.Result` / `.Wait()` / `GetAwaiter().GetResult()`（死锁风险）
- **禁止**用 `Task.Run` 包裹同步实现来"伪造"异步

### 3.5 资源管理

- 持有非托管资源或 `IDisposable` 字段的类型实现 `IDisposable`，用 `using` / `using var`
- `Dispose` 保证幂等
- 不使用终结器，除非直接持有非托管句柄

### 3.6 异常

| 场景 | 做法 |
|---|---|
| 参数为 null | `ArgumentNullException.ThrowIfNull` |
| 参数越界 | `ArgumentOutOfRangeException` |
| 状态非法 | `InvalidOperationException` |
| 已知业务失败 | **返回失败结果，不抛异常** |
| 意外异常 | 允许冒泡，由宿主统一处理 |

- 异常消息**必须为英文并加句号**
- 不吞异常（空 `catch` 禁止）；确需吞掉时记录并注释原因
- 自定义异常置于 `Exceptions/` 目录，继承自语义最接近的系统异常

### 3.7 性能与内存

- 热路径使用 `Span<T>` / `ReadOnlySpan<T>` / `Memory<T>`
- 字节处理避免无谓复制
- 避免在循环内分配；必要时使用 `ArrayPool<T>`
- 字符串拼接在循环内使用 `StringBuilder`

### 3.8 密码学专用约束

| 规则 | 理由 |
|---|---|
| **不得**继承 `SymmetricAlgorithm`、`HashAlgorithm`、`AsymmetricAlgorithm`、`HMAC` | `CipherMode` 是封闭枚举，既无 CTR 也无 AEAD 概念；且两个 `netstandard` 目标没有 `net8.0` 的虚方法 —— 继承它们等于把库的能力集绑死在特定 TFM 上。使用自建抽象，BCL 互操作经适配器提供（见 [architecture.md §8](architecture.md) 决策 D20）。 |
| BouncyCastle 类型不得出现在公开签名中 | 实现必须可替换；互操作只经 `Interop/` 下的显式扩展方法（决策 D21）。 |
| 算法代理统一命名为 `<Algorithm>Crypto` | 一套命名规则覆盖全部算法（决策 D23）。 |
| 密钥材料在释放时清零 | 不得比拥有它的对象活得更久；见 [roadmap.md](roadmap.md) §6.15 的 `SymmetricKey`。 |
| 热路径优先用 `Span<T>`，但两个 `netstandard` 目标必须仍可构建 | 那里由 `System.Memory` 提供 span；仅存在于 `net8.0` 及以后的内容用 `#if` 包起来。 |
| 可能被裁剪或 AOT 编译的消费方触达的代码，须避开未经标注的反射 | 见 `RM-0.0.12`。 |

---

## 4. 命名规范

| 元素 | 规范 | 示例 |
|---|---|---|
| 命名空间 / 程序集 | PascalCase，点分层 | `DevTrove.Crypto` |
| 类 / 结构 / 记录 | PascalCase | `Certificate` |
| 接口 | `I` + PascalCase | `IDisposable` |
| 方法 | PascalCase | `GenerateSelfSigned` |
| 属性 | PascalCase | `NotAfter` |
| 公共字段 | **不使用**（改用属性） | — |
| 私有字段 | `_camelCase` | `_registry` |
| 局部变量 / 参数 | camelCase | `privateKey` |
| 常量 | PascalCase | `DefaultSerialNumberBytes` |
| 泛型参数 | `T` + 描述 | `TResult` |
| 测试项目 | `<被测项目>.Tests` | `DevTrove.Crypto.Core.Tests` |
| 测试方法 | `Method_Should_Behavior_When_Condition` | `GenerateSelfSigned_Should_Fail_When_KeyIsNull` |

---

## 5. 注释与 XML 文档

- **所有 public 成员必须有 XML 文档注释**（库开启 `GenerateDocumentationFile`，缺注释会触发 CS1591 警告）
- 注释用中文
- 优先使用 `<inheritdoc/>` 继承基类或接口文档
- `<summary>` 用一句话描述"做什么"，不重复方法名
- 复杂算法、非直观取舍、性能考量用 `//` 行内注释说明**为什么**，而非**是什么**
- 不要保留注释掉的代码；Git 保存历史
- `TODO` / `HACK` / `FIXME` 必须带上下文：

  ```csharp
  // TODO(tls): 支持 GOST 套件（RFC 9189），当前仅标准套件 + 国密
  ```

---

## 6. 日志规范

- **库不打日志**。不引用 `Microsoft.Extensions.Logging.Abstractions`，不内置 logger；由调用方决定记录方式
- 若库代码确实需要向调用方传递信息，通过返回值对象或回调参数
- 异常已自带足够上下文

---

## 7. 测试规范

- 框架：**xUnit** + **FluentAssertions** + coverlet 覆盖率
- 命名：`Method_Should_Behavior_When_Condition`
- 结构：Arrange–Act–Assert，三段用空行分隔
- 一个测试只验证一个行为
- 测试项目**镜像**被测项目的目录结构
- 测试夹具统一放在 `tests/data/`，通过相对路径定位。该目录**只生成、不入库**（见 [architecture.md §8](architecture.md) 的 D18）。脚本产不出的抓取类数据放 `tests/fixtures/`，那里是入库的。
- **不写**仅断言"不抛异常"的测试
- 依赖外部可执行文件（tongsuo）的测试必须有明确的失败语义（见 [development-guide.md §5.2](development-guide.md)）

> **已知偏差**（`RM-0.0.7`）：项目目前使用 xUnit 2.9.2 + FluentAssertions 6.12.1，且 `DevTrove.Crypto.TestSupport` 尚未声明 `net9.0`。升级到 xUnit v3 已在考虑，但**当前未排期**。

---

## 8. 仓库工作模式

本仓库**独立存在**：自行开发、测试与发布，仓内任何内容都不依赖它被如何消费、被谁消费。

| 模式 | 做法 |
|---|---|
| 日常开发 | `dotnet build DevTrove.Crypto.slnx -c Release` 与 `dotnet test` 完全在本仓内运行 |
| 发布 | CI 绿灯后打 `v*` 标签触发 publish job（见 [development-guide.md §6](development-guide.md)） |

消费方如何引用本包 —— 项目引用、包引用或本地 feed —— 属消费方决策，本文档有意不涉及。

---

## 9. Git 规范

### 9.1 分支

| 分支 | 用途 |
|---|---|
| `main` | 稳定分支，始终保持可构建 |
| `dev` | 集成分支；预发布变更先落在这里，再进 `main` |
| `feature/<slug>` | 功能开发 |
| `fix/<slug>` | 缺陷修复 |
| `docs/<slug>` | 仅文档变更 |

### 9.2 提交信息

Conventional Commits，**英文类型前缀 + 中文描述**：

```
<type>(<scope>): <中文描述>
```

| type | 含义 |
|---|---|
| `feat` | 新功能 |
| `fix` | 缺陷修复 |
| `docs` | 文档 |
| `refactor` | 重构（不改变行为） |
| `test` | 测试 |
| `build` | 构建/依赖 |
| `chore` | 杂项 |
| `perf` | 性能优化 |

一次提交只做一件事；不混入无关格式化改动。

### 9.3 提交前检查

- `dotnet build DevTrove.Crypto.slnx -c Release` —— 在 `RM-0.0.1` 与 `RM-0.0.11` 落地前预期会失败；新代码须 0 警告
- `dotnet test DevTrove.Crypto.slnx -c Release` —— 全绿（或明确标注为预存缺陷）
- 新增/修改的 public 成员有中文 XML 文档注释
- 触及 `README.md` / `CHANGELOG.md` ⇒ 同步 `.zh-CN.md`
- 触及 `docs/*.md` ⇒ 同步对应 `*.zh-CN.md`（章节、表格、Mermaid、代码示例一一对应）
- **变更了 [roadmap.md](roadmap.md) 中任何条目的状态** ⇒ 在同一次提交里更新，中英两份都改
- 触及架构 / 包 / 命名 / TFM / 测试策略 ⇒ 同步本仓 `docs/*` 与 `.zh-CN.md`
- 日志与异常中**无**密钥材料、口令、输入原文

---

## 10. 禁止事项

- ❌ 提交 `bin/`、`obj/`、`artifacts/`、`.vs/`、`.vshistory/`、`*.pfx`
- ❌ 把 `tests/data/` 强行纳入版本控制（它是生成物；见 D18）
- ❌ 使用 `git add -A` / `git add .`（夹具与残留极易被误提交）
- ❌ 在 `docs/*.md` 与 `*.zh-CN.md` 之间出现章节、链接或表格不一致
- ❌ 声称未实现的能力（文档与代码必须一致）
- ❌ 在核心库内打日志或抛出中文异常消息
- ❌ 在 `DevTrove.Crypto` 门面包项目内放置任何 API 类型

---

## 11. 相关文档

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 库内分层、依赖方向、能力边界 |
| [nuget.md](nuget.md) | 包边界、版本策略、发布流程 |
| [tls-scanner.md](tls-scanner.md) | TLS 探测引擎设计 |
| [roadmap.md](roadmap.md) | 库的阶段路线 + 待办 / 已知偏差 |
| [development-guide.md](development-guide.md) | 构建/测试/打包/CI/夹具 |
| [library-api.md](library-api.md) | 公开 API 索引 |
| [../AGENTS.md](../AGENTS.md) | 入口：权威文档、硬性约束、提交前检查 |
