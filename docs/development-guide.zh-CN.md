# 开发指南

> 英文默认入口：[development-guide.md](development-guide.md)

`DevTrove.Crypto` 的构建、测试、打包与 CI 约定。

---

## 1. 命令速查

| 动作 | 命令 |
|---|---|
| 全部构建（Release） | `dotnet build DevTrove.Crypto.slnx -c Release` |
| 全部测试 | `dotnet test DevTrove.Crypto.slnx -c Release` |
| 仅单元测试（跳过互操作） | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category!=Integration'` |
| 仅互操作测试 | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category=Integration'` |
| 打包 Core | `dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts` |
| 打包门面包 | `dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -o ./artifacts` |
| 生成 PFX 夹具（CI 也跑） | `./scripts/generate-test-pfx.sh` |

外部依赖：**tongsuo**。缺失时测试**直接失败而非跳过**。见 §5.2。

> **已知偏差**（`RM-0.0.1`、`RM-0.0.11`）：三处目标框架声明彼此不一致，且两个 `netstandard` 目标从未产出过程序集。在这两项落地前，构建预期会失败。

---

## 2. 项目布局

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/         门面包（无源码 —— 见 roadmap `RM-0.0.2`）
│  ├─ DevTrove.Crypto.Core/    实现
│  └─ DevTrove.Crypto.Tls/     TLS 探测引擎（计划中 `0.4.0` —— 尚不存在）
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   CLI 封装（tongsuo）
├─ scripts/                     夹具生成
├─ .github/workflows/build.yml  CI
├─ Directory.Build.props        全局编译属性 + NuGet 元数据
├─ Directory.Packages.props     CPM
├─ DevTrove.Crypto.slnx         解决方案
├─ global.json                  SDK 锁定
└─ docs/                        本文档集
```

---

## 3. 编译属性

`Directory.Build.props` 设：

```xml
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

<!-- NuGet 元数据基线（按项目覆盖） -->
<Authors>blue-cloud-net</Authors>
<Company>blue-cloud-net</Company>
<Copyright>Copyright © blue-cloud-net 2026</Copyright>
<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
<RepositoryUrl>https://github.com/blue-cloud-net/DevTrove.Crypto</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<PackageProjectUrl>https://github.com/blue-cloud-net/DevTrove.Crypto</PackageProjectUrl>
<PackageTags>crypto;x509;certificate;asn1;pem;pkcs12;crl;csr;sm2;sm3;sm4;gm;bouncycastle</PackageTags>
<PackageReadmeFile>README.md</PackageReadmeFile>
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>

<!-- 目标框架（由基线声明；每个 csproj 必须一致） -->
<TargetFrameworks>netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0</TargetFrameworks>
```

各 csproj 继承基线或自行覆盖。当前 Core 覆盖为三个 TFM、门包覆盖为一个 —— 三处声明必须统一（`RM-0.0.1`）。

### 两个 `netstandard` 目标的现状

两个 `netstandard` 目标**从未产出过程序集**，而 README 对此的解释错了两处：polyfill 不在 `Compat/` 目录（它其实是 `Extensions/ArgumentNullExceptionExtensions.cs`），且它的守卫 `#if NETSTANDARD2_0` 并未覆盖 `netstandard2.1`。此外，`Convert.FromHexString`、`RandomNumberGenerator.GetBytes(int)` 与 `AsSpan` 在这两个目标上都没有保护。

两个目标**保留** —— 这是 NuGet 库，兼容面本身就是产品的一部分。推进项：`RM-0.0.11`。

---

## 4. 消费方集成（不在本仓范围）

消费方如何引用这些包 —— 开发期用 `ProjectReference`、从 feed 用 `PackageReference`，或对未发布版本用本地文件夹 feed —— 属消费方决策。本仓库不提供这样的切换，也不测试它。

有一个后果值得写下来，因为它会在打包时咬人：`ProjectReference` 不会变成 NuGet 依赖。基于项目引用打出的包会缺少依赖声明，使用方将还原失败。见 [nuget.md §10](nuget.md)。

---

## 5. 测试

### 5.1 框架

| 项 | 选择 |
|---|---|
| 测试框架 | **xUnit 2.9.2** + **FluentAssertions 6.12.1** |
| Mock | 当前不需要 |
| 覆盖率 | coverlet |
| 测试项目命名 | `<Subject>.Tests` |
| 方法命名 | `Method_Should_Behavior_When_Condition` |
| 结构 | Arrange–Act–Assert，三段用空行分隔 |

> **已知偏差**（`RM-0.0.7`）：`DevTrove.Crypto.TestSupport.csproj` 未声明 `net9.0`，而测试项目声明了。升级到 xUnit v3 已在考虑，但未排期。

### 5.2 `TongsuoCli`

互操作测试（密钥 / 证书 / CSR / CRL / PKCS#12 的生成与解析，以及国密系列）调用 **tongsuo** —— 携带中国国密算法的 OpenSSL 发行版。它是本仓库**唯一**的外部工具依赖。

| 项 | 说明 |
|---|---|
| 路径覆盖 | 环境变量 `TONGSUO_PATH`（默认 `/opt/tongsuo/bin/tongsuo`） |
| 版本要求 | 需支持 SM2 / SM3 / SM4 |
| 用途 | ① 互操作测试：用 tongsuo 生成 → 用本库解析（反向亦然）；② 夹具兜底生成 |

**不再使用上游 OpenSSL。** tongsuo 是其分支且命令兼容，但并非同一实现 —— 因此不再断言与上游 OpenSSL 的互操作。这一取舍已记为 [roadmap.md §8](roadmap.md) 的风险 R2；针对上游 OpenSSL 的可选、非阻塞交叉校验是备选出路。`RM-0.1.0-06` 会把原先的 `OpenSslCli` 助手并入 `TongsuoCli`。

### 5.3 工具缺失时的行为（重要）

`DevTrove.Crypto.TestSupport` 的测试基建在外部工具不可用时**直接失败（Fail），而非跳过（Skip）**。

这意味着：**CI 镜像必须提供 tongsuo**，否则所有互操作测试都会失败。

这是设计上的有意取舍：

| 做法 | 结果 |
|---|---|
| ✅ 失败 | 环境问题立刻暴露；不会出现"测试全绿但实际没测" |
| ❌ 跳过 | 容易长期掩盖问题；互操作缺陷在发布后才发现 |

若将来需要支持「无 tongsuo 环境」，应通过**显式的测试分类标记**（如 `[Trait("RequiresTongsuo", "true")]`）按环境过滤；不是静默跳过。

### 5.4 交叉验证（不进 CI）

扫描器判定需要与独立实现比对。以下步骤**本地手动执行**，不进 CI：

1. 对公开测试站点扫描（过期证书、自签证书、主机名不匹配、弱套件、无 SNI、仅旧协议）
2. 用 `tongsuo s_client` 获取相同目标的协议、套件与证书链
3. 可选：用 `testssl.sh` 获取相同目标的判定
4. 三者比对，差异逐条确认原因

`testssl.sh` 许可证为 GPLv2 —— **不**打包进本仓库；仅作开发期的外部交叉验证工具。

---

## 6. CI

`.github/workflows/build.yml`：

| 触发 | 动作 |
|---|---|
| Push 到 `main` / `dev` | 构建 + 测试 |
| Pull Request → `main` / `dev` | 构建 + 测试 |
| 推送 `v*` 标签 | 构建 + 测试 + 打包 + 发布（需在 `nuget` environment 中设置 `NUGET_API_KEY`） |

Job：

| Job | 用途 | 说明 |
|---|---|---|
| `build` | 构建 + 单元测试 + 互操作测试 + 覆盖率 + 打包至 `artifacts/` | `ubuntu-latest`，.NET 8.x / 9.x / 10.x。互操作阶段前必须先构建 tongsuo（pin 版本 + 缓存）—— 见 `RM-0.0.9` |
| `publish` | 标签触发：打包 + 推 nuget.org | 需 `NUGET_API_KEY` |

### CI 发布顺序

1. `DevTrove.Crypto.Core`
2. `DevTrove.Crypto`
3. `DevTrove.Crypto.Tls`

NuGet 不支持原子多包发布。为避免"依赖已升级但被依赖包尚未发布"的窗口期，应**先发被依赖包、后更新消费方**，或在 CI 中按上述顺序依次推送。

### CI 已知偏差（`RM-0.0.6`、`RM-0.0.9`）

- `publish` job 用 `dotnet pack ... --no-build`，但缺少先行的 `dotnet build` 步
- 推送门包的 glob `DevTrove.Crypto.*.nupkg` 也会匹配到 Core 包
- 工作流只在 `main` 触发；`actions/checkout` 仍请求 `submodules: recursive`，而本仓库没有子模块
- 未安装 tongsuo，干净 runner 上的集成阶段会失败

---

## 7. 打包

### 本地打包

```
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts --include-symbols
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts --include-symbols
```

产出：`./artifacts/*.nupkg` + `*.snupkg`。

### 打包注意事项

- `IncludeSymbols=true` + `SymbolPackageFormat=snupkg` —— `Directory.Build.props` 已设
- `GenerateDocumentationFile=true` —— 各库 csproj 设
- `PackageReadmeFile=README.md` —— `Directory.Build.props` 已声明；csproj 须 `<None Include="..\..\README.md" Pack="true" PackagePath="\" />`
- `RepositoryUrl` / `RepositoryType` / `PackageProjectUrl` —— 指向 `https://github.com/blue-cloud-net/DevTrove.Crypto`

打包后验证：

- `.nupkg` 含 XML 文档（`lib/<tfm>/*.xml`）
- `.nupkg` 根目录含 README
- 所有声明的 TFM 都有 `lib/<tfm>/` 目录
- 不含 `runtimes/*/native/` 内容

---

## 8. 夹具管理

### 8.1 布局

`tests/data/` **整体由脚本生成、整体被 Git 忽略**（`.gitignore` 中的 `tests/data/` 规则）。其下任何文件都不入库 —— 连各目录的 `README.md` 也不入库，所以那只是本地草稿，不算文档。

```
tests/data/                      ← 生成物，不入库
├── certs/        证书（真实站点链、自签链、SM2 证书）
├── crls/         CRL（含 SM2）
├── csrs/         CSR（含扩展组合、SM2）
├── keys/         密钥（RSA / EC / DSA / SM2，PEM + DER，含加密私钥）
├── pfx/          PKCS#12（口令保护）
└── ocsp/         OCSP 响应夹具（计划 —— 见 `RM-0.0.10`）

tests/fixtures/                  ← 入库
└── ntls/         从公开国密站点抓取的 NTLS 握手字节（计划 —— `RM-0.0.10`）
```

划分只按一条规则：**脚本能重建的一律生成；只能从外部抓取的一律入库。** `TestData` 目前只为 5 个生成目录提供访问方法。

### 8.2 来源与策略

| 夹具 | 来源 | 入库 | 说明 |
|---|---|---|---|
| RSA / EC / DSA 密钥与证书 | 由 `tongsuo` 生成 | ❌ | 缺失时由脚本重建 |
| **SM2 密钥、证书、CSR、CRL** | 由 `tongsuo` 生成 | ❌ | 需要机器上有 tongsuo；脚本不会回退到更弱的工具 |
| PKCS#12 | 由 `tongsuo` 生成 | ❌ | `.gitignore` 另外还单独忽略 `*.pfx` |
| 真实站点证书链 | 由 `pull-website-certs.sh` 从公开站点抓取 | ❌ | 落在生成目录内，因而也是重建而非入库 |
| **NTLS 握手字节** | 从公开国密站点抓取 | ✅ | 唯一的例外 —— 脚本产不出，因此放 `tests/fixtures/ntls/`。见 §8.4 |

**统一口令**：所有加密私钥与 PKCS#12 使用同一个测试口令（当前 `test1234`）。本节是该约定的**入库**记录 —— 各 `tests/data/<子目录>/README.md` 与夹具一同生成，并不入库。

### 8.3 夹具脚本

共五个脚本，每个对应一个算法族 —— 国密夹具与标准算法**在同一次运行中一起生成**，而不是靠单独脚本：

```
scripts/
├── generate-test-certs.sh       RSA / EC / DSA / SM2 自签 + CA→leaf 链
├── generate-test-crl.sh         含两个吊销条目与原因的 CRL（含 SM2）
├── generate-test-pfx.sh         私钥 + 证书 / 证书链 PFX（test1234）
├── generate-test-csrs.sh        含扩展组合的 CSR（含 SM2）
├── generate-test-keys.sh        RSA / EC / DSA / SM2 密钥（PEM + DER，加密变体）
└── pull-website-certs.sh        抓取公开站点链到 tests/data/certs/
```

目前 `generate-test-certs.sh` 与 `generate-test-crl.sh` **完全没有 SM2 段**，但这两类 SM2 夹具确实存在于工作区 —— 说明它们由某个已不在仓库里的东西产生，今天无法复现。`TestDataGenerator` 仍然引用一个不存在的 `generate-test-sm-certs.sh`。两者均见 `RM-0.0.9`。

### 8.4 NTLS 握手字节夹具（关键）

由于不使用原生国密协议栈，NTLS 检测的回归**无法通过 loopback 服务端完成**。替代方案：

| 手段 | 说明 |
|---|---|
| **静态字节夹具** | 从公开国密站点抓取真实的 `ServerHello` / `Certificate` / `ServerKeyExchange` 字节，存入 `tests/fixtures/ntls/`，测试直接喂给解析器 |
| **假服务端回放** | 测试进程内起极简 TCP 监听，回放上述字节，验证完整探测路径（含超时与异常处理） |

夹具应覆盖：

| 变体 | 目的 |
|---|---|
| 标准 NTLS（双证书 + GCM 套件） | 主路径 |
| 单证书（仅签名证书） | 验证"双证书缺失"的判定 |
| 非 SM2 证书 | 验证算法识别的健壮性 |
| 协商为 CBC 套件 | 验证套件分类 |
| 服务端拒绝（返回 alert） | 验证"不支持 NTLS"的判定 |
| 截断的响应字节 | 验证解析器的边界检查 |

---

## 9. 本地工作流

```bash
# 1. 克隆
git clone https://github.com/blue-cloud-net/DevTrove.Crypto.git
cd DevTrove.Crypto

# 2. 确保 tongsuo 可用（测试依赖它）
#    默认 /opt/tongsuo/bin/tongsuo，可用 TONGSUO_PATH 覆盖
export TONGSUO_PATH=/opt/tongsuo/bin/tongsuo

# 3. （可选）夹具漂移时重新生成
./scripts/generate-test-keys.sh
./scripts/generate-test-certs.sh
./scripts/generate-test-csrs.sh
./scripts/generate-test-crl.sh
./scripts/generate-test-pfx.sh

# 4. 构建 + 测试
#    在 RM-0.0.1 与 RM-0.0.11 落地前，预期会失败
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  DevTrove.Crypto.slnx -c Release

# 5. 打包
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts
```

---

## 10. 相关文档

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 项目布局、依赖方向、能力边界 |
| [standards.md](standards.md) | 编码规范 |
| [nuget.md](nuget.md) | 包边界、版本策略、发布流程 |
| [tls-scanner.md](tls-scanner.md) | TLS 探测引擎设计 |
| [roadmap.md](roadmap.md) | 版本线、逐项状态与证据 |
