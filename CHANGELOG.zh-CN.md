# 变更日志

本文件记录 `DevTrove.Crypto` 的所有重要变更。

格式遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/spec/v2.0.0.html)。

> 英文版本：[CHANGELOG.md](CHANGELOG.md)

## [未发布]

### 变更（进行中：父仓路线图 Phase A）

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

- L1：证书链**验证**为精简实现（DN 比对 + 逐级验签 + 信任根匹配），**非 PKIX 完整路径校验** —— v1.1 升级
- L2：证书链**构建**对互相签发的构造输入可能不终止 —— v1.1 修复
- L3：OCSP **仅解析响应**（不构造请求、不验证签名） —— v2 评估

[未发布]: https://github.com/blue-cloud-net/DevTrove.Crypto/compare/main...HEAD
