# 变更日志

本文件记录 `DevTrove.Crypto` 的所有重要变更。

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/spec/v2.0.0.html)。

> 英文版本：[CHANGELOG.md](CHANGELOG.md)

## [未发布]

### 变更（仅库化重构）

- **重命名**：项目目录、程序集名、命名空间 `Crypto.Utils.*` → `DevTrove.Crypto.*`
- **删除**：`Crypto.Utils.Api`、`Crypto.Utils.Host`、`Crypto.Utils.UI`、相关 NuGet 包、旧 HTTP API 文档（`docs/api-reference.md`、`docs/v0.2/*`、`docs/v1.x/*`）
- **新增**：`DevTrove.Crypto` 门面包（无源码，仅 `ProjectReference` → Core）
- **新增**：`DevTrove.Crypto.slnx`（替换 `crypto-utils.slnx`）
- **变更**：`Directory.Build.props` —— 根命名空间、NuGet 元数据（`RepositoryUrl` → `https://github.com/blue-cloud-net/DevTrove.Crypto`）
- **变更**：`Directory.Packages.props` —— 删除 Web 包（`Swashbuckle`、`Microsoft.AspNetCore.OpenApi`、`SpaProxy`）
- **新增**：`global.json`（SDK 10.0.112，`rollForward: latestMajor`）
- **新增**：双语 `README.md` / `README.zh-CN.md`
- **新增**：`AGENTS.md`（AI / 人贡献者工作流）
- **文档**：重写 `docs/architecture.md` 为库内分层；更新 `docs/roadmap.md` 与 `docs/development-guide.md`
- **说明**：5 TFM（`netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0`）当前暂用 3 TFM（`net8.0;net9.0;net10.0`），待 netstandard2.0 polyfill 完成后再恢复

### 已知限制

能力缺口（证书链验证、OCSP、PKCS#7、KDF、MAC、Ed25519 / X25519）与会长期保留的限制见 [docs/architecture.md §7](docs/architecture.md)。`0.0.x` 是工作项编号而非版本；逐项状态见 [docs/roadmap.md](docs/roadmap.md)。

### 文档

- 整套文档按**独立库**口径重写：删除全部「库如何被消费、被谁消费」的描述，把阶段路线换成版本线，并引入逐项状态追踪（`✅` / `🚧` / `🟡` / `⬜`）
- 在 `docs/architecture.md §1` 补上明确的目标集（纯托管、跨平台、AOT 友好、宽兼容面）
- 修正与代码不符的宣称：OCSP 解析、PKCS#7/CMS、证书链构建与验证、`SM4` 的 CTR/GCM **均未实现**；两个 `netstandard` 目标从未产出程序集
- 把代码风格配置的矛盾（`.editorconfig` vs `docs/standards.md §2.4`）记入台账

[未发布]: https://github.com/blue-cloud-net/DevTrove.Crypto/compare/main...HEAD
