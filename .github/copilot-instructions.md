# DevTrove.Crypto

纯托管 .NET 密码学与证书库（BouncyCastle 封装），发行 `DevTrove.Crypto`（门面包）、`DevTrove.Crypto.Core`（实现）、`DevTrove.Crypto.Tls`（TLS 探测引擎，Phase 0 引入）。

## 技术栈

| 层 | 技术 |
|---|---|
| 核心库 | .NET 10 + BouncyCastle 2.6.2 |
| 测试 | xUnit 2.9.2 + FluentAssertions 6.12.1 + CliWrap + OpenSSL 3.x 互操作 |
| 多目标 | `netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`（当前 3 TFM，polyfill 待补） |

## 文档

| 文档 | 说明 |
|---|---|
| [AGENTS.md](../AGENTS.md) | AI / 人贡献者入口（必读） |
| [架构](../docs/architecture.md) | 库内分层、依赖方向、能力边界、已知限制 |
| [规范](../docs/standards.md) | 编码规范、命名、Git 流程、评审清单 |
| [NuGet](../docs/nuget.md) | 包边界、版本策略、发布流程 |
| [TLS 探测](../docs/tls-scanner.md) | 引擎设计、矩阵、评级、国密 |
| [路线图](../docs/roadmap.md) | 库的阶段路线 + 待办 / 已知偏差 |
| [开发指南](../docs/development-guide.md) | 构建/测试/打包命令、TFM 与 polyfill 规则 |
| [库 API 索引](../docs/library-api.md) | 公开 API 清单 |

`docs/` 下每份文档成对存在：英文默认入口（`x.md`）+ 中文对照（`x.zh-CN.md`）。

## 关键约定

- **命名空间**：根命名空间 `DevTrove.Crypto`；`DevTrove.Crypto.Core` 项目导出 `DevTrove.Crypto.*` 命名空间
- **门面包 = 元包**：`DevTrove.Crypto` 项目**无源码**，仅 `ProjectReference` → `DevTrove.Crypto.Core`（注：当前 `src/DevTrove.Crypto/Program.cs` 仍存在，见 roadmap 待办）
- **零框架依赖**：核心库不引用 DI、日志、ASP.NET Core
- **不打日志**：核心库不产生日志
- **构建入口**：`DevTrove.Crypto.slnx`
- **多目标**（**目标** 5 TFM：`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`；**当前** Core 实为 `net8.0;net9.0;net10.0`，门包 `net10.0`，导致 slnx 构建 **NU1201**，见 roadmap 待办）

## 代码规范

1. 所有公开 `async` 方法签名末尾必须包含 `CancellationToken ct = default`
2. 库代码 `await` 调用须追加 `.ConfigureAwait(false)`
3. 库内**不使用**日志；若必须，统一通过返回值/异常向外暴露
4. 可空引用类型全量启用；公共 API 必须在方法体内做参数校验（`ArgumentNullException.ThrowIfNull` 等）
5. 通过接口编程；Core 层暴露服务接口
6. 不吞异常；捕获异常时必须显式处理或重新抛出，使用英文消息便于诊断
7. **公共 API 必须有 XML 注释**；实现类优先用 `<inheritdoc />`；注释中文，异常消息英文，句尾加句号
8. 严禁硬编码密钥、凭证、Token；敏感数据不写日志，不在异常消息中暴露；密钥操作后及时清理内存
9. 测试命名：`MethodName_Should_ExpectedBehavior_When_Condition`，AAA 模式，覆盖正常/边界/异常路径
10. **Conventional Commits 规范**：
    - 中文描述，一到三句话，首字母小写，句尾不加句号
    - 以 `type(scope): description` 格式开头，type ∈ `feat|fix|docs|refactor|test|build|chore|perf`
11. 密钥导入/导出必须指定格式参数（PKCS#1/PKCS#8/SEC1 等），禁止使用默认格式
12. 签名验证失败时返回特定错误码，禁止泄露原始异常信息
13. 随机数生成使用 `SecureRandom`，禁止使用 `Random`；对称加密包含 IV/Nonce，禁止 ECB 模式
14. 错误消息通过资源文件定义，支持 i18n（zh-CN / en-US）
15. 时间戳统一使用 UTC，展示时由调用方转换为本地时区
16. `Directory.Packages.props` 统一管理包版本；BouncyCastle 大版本更新需评估 API 兼容性
17. 使用 `Path.Join()` 拼接路径（.NET 6+），兼容 Linux，禁止硬编码反斜杠
18. 导出文件操作使用 `try-finally` 或 `using` 快速释放资源
