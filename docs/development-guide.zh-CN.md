# 开发指南

> 英文默认入口：[development-guide.md](development-guide.md)

`DevTrove.Crypto` 的构建、测试、打包与 CI 约定。

---

## 1. 命令速查

| 动作 | 命令 |
|---|---|
| 全部构建（Release） | `dotnet build DevTrove.Crypto.slnx -c Release` |
| 跑契约测试（Linux） | `dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0` |
| 跑针对 `netstandard2.0` 资产的契约测试（Windows） | `dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net48` |
| 仅单元测试（跳过互操作） | `dotnet test tests/DevTrove.Crypto.Core.Tests -c Release --framework net10.0 --filter 'Category!=Integration'` |
| 仅互操作测试 | `dotnet test tests/DevTrove.Crypto.Core.Tests -c Release --framework net10.0 --filter 'Category=Integration'` |
| 打包 Abstractions | `dotnet pack src/DevTrove.Crypto.Abstractions/DevTrove.Crypto.Abstractions.csproj -c Release -o ./artifacts` |
| 打包 Core | `dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts` |
| 打包门面包 | `dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -o ./artifacts` |
| 生成 PFX 夹具（CI 也跑） | `./scripts/generate-test-pfx.sh` |

> **明确指出项目与框架是有意为之。** 测试项目的 `net48` 目标只在 Windows 上存在（见 §2），因此不带 `--framework` 的 `dotnet test DevTrove.Crypto.slnx` 在 Linux 上无法完成。两者都写明，才能使本文档的每条命令在两个平台上都可执行。

外部依赖：**tongsuo**。缺失时测试**直接失败而非跳过**。见 §5.2。

---

## 2. 项目布局

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto.Abstractions/  契约（零依赖 —— 计划中，`0.1.0`）
│  ├─ DevTrove.Crypto/              门面包（无源码 —— 见 roadmap `RM-0.0.2`）
│  ├─ DevTrove.Crypto.Core/         实现
│  └─ DevTrove.Crypto.Tls/          TLS 探测引擎（计划中 `0.6.0` —— 尚不存在）
├─ tests/
│  ├─ DevTrove.Crypto.Abstractions.Tests/  契约测试；仅在 Windows 上包含 `net48`
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/         CLI 封装（tongsuo）
├─ scripts/                     夹具生成
├─ .github/workflows/ci.yml     分支 CI + 抽象测试矩阵
├─ .github/workflows/release.yml tag 触发的发布
├─ Directory.Build.props        全局编译属性 + NuGet 元数据
├─ Directory.Packages.props     CPM
├─ DevTrove.Crypto.slnx         解决方案
├─ global.json                  SDK 锁定
└─ docs/                        本文档集
```

### 测试项目的目标框架

`DevTrove.Crypto.Abstractions.Tests` 就是使 `netstandard2.0` 资产获得**运行验证**的宿主：`net48` 项目解析 `lib/netstandard2.0/`，而 `net8.0` 及以后的项目解析自己的资产。由于 .NET Framework 无法在 Linux 上运行，`net48` 目标**按操作系统条件声明**：

```xml
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('Windows'))">net48;net8.0;net9.0;net10.0</TargetFrameworks>
<TargetFrameworks Condition="!$([MSBuild]::IsOSPlatform('Windows'))">net8.0;net9.0;net10.0</TargetFrameworks>
```

这样 `dotnet build DevTrove.Crypto.slnx` 在 Linux 上仍可工作，且不需要 `Microsoft.NETFramework.ReferenceAssemblies` 包 —— 否则在 Linux 上连构建 `net48` 目标都做不到。

---

## 3. 编译属性

`Directory.Build.props` 设：

```xml
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>

<!-- TreatWarningsAsErrors / EnforceCodeStyleInBuild 有意保持关闭：
     见 standards.md §2.3。新代码仍须 0 警告（§9.3）。 -->

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
<TargetFrameworks>netstandard2.0;net8.0;net9.0;net10.0</TargetFrameworks>
```

各 csproj 继承基线或自行覆盖。当前所有 csproj 都继承此 4 TFM 基线声明。

### 额外的包引用

`DevTrove.Crypto.Abstractions` 在它唯一的 `netstandard` 目标上需要 `Span<T>`，因此带一个条件引用：

```xml
<PackageReference Include="System.Memory" Condition="'$(TargetFramework)' == 'netstandard2.0'" />
```

版本号与其他所有包一样，只在 `Directory.Packages.props` 声明一次（见 [standards.md §2.2](standards.md)）。

### `netstandard2.0` 目标

该目标**保留** —— 这是 NuGet 库，兼容面本身就是产品的一部分。`netstandard2.1` 已移除：没有任何未 EOL 的宿主会解析该资产，它永远无法被运行验证（见 [nuget.md §5](nuget.md)）。

polyfill 落在 `0.1.0` 目标布局中命名的 `Compat/` 目录下，且守卫符号**按 API 分别选取** —— `Convert.FromHexString` 需要 `NETSTANDARD2_0`，而 `HashAlgorithm.HashCore(ReadOnlySpan<byte>)` 在 `netstandard2.1` 与 `netstandard2.0` 上并不一致。见 `RM-0.1.0-01`。

---

## 4. 消费方集成（不在本仓范围）

消费方如何引用这些包 —— 开发期用 `ProjectReference`、从 feed 用 `PackageReference`，或对未发布版本用本地文件夹 feed —— 属消费方决策。本仓库不提供这样的切换，也不测试它。

有一个后果值得写下来，因为它会在打包时咬人：`ProjectReference` 并不总能变成 NuGet 依赖。基于项目引用打出的包可能缺少依赖声明，使用方因此还原失败。`DevTrove.Crypto.Abstractions` 是本仓第一个把这件事变具体的地方 —— `DevTrove.Crypto.Core` 必须声明它。务必解包 `.nupkg` 读 `.nuspec`；清单项见 [nuget.md §8](nuget.md)。

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

### 5.1.1 抽象层的契约测试

`DevTrove.Crypto.Abstractions.Tests` 不携带任何外部依赖 —— 没有 BouncyCastle，也没有 tongsuo —— 因此跑得快，也不需要生成夹具。它的 stub 类型放在 `_TestStubs/`；这是测试 `abstract` 基类的唯一手段，且它们的目的是验证基类**真正实现的逻辑**，而不是 `abstract` 关键字本身：

| 测试对象 | 理由 |
|---|---|
| 默认 `Cbc` / `Pkcs7` | 默认值是真实交付的行为 |
| ECB 使 `Encrypt` 抛错 | 基类里的守卫，只能通过具体子类触发 |
| 四种 BCL 兼容模式经 `AsSymmetricAlgorithm()` | 适配器是全 concrete 的，也是 `RM-0.1.0-05` 的交付物 |
| GCM / CTR 抛 `NotSupportedException` | 明确 BCL 桥接的边界 |
| `DigestBase` 分块 `Update` 等于一次性 `ComputeHash` | 缓冲区管理是被继承的逻辑 |
| `AsymmetricKeyBase.Dispose` 清零密钥材料 | `standards.md §3.8` 把它定成了硬规则 |
| 四个 X.509 接口被单一 stub 实现 | 在 `0.5.0` 锁定它们之前证明接口可实现 |

### 5.2 `TongsuoCli`

互操作测试（密钥 / 证书 / CSR / CRL / PKCS#12 的生成与解析，以及国密系列）调用 **tongsuo** —— 携带中国国密算法的 OpenSSL 发行版。它是本仓库**唯一**的外部工具依赖。

| 项 | 说明 |
|---|---|
| 路径覆盖 | 环境变量 `TONGSUO_PATH`（默认 `/opt/tongsuo/bin/tongsuo`） |
| 版本要求 | 需支持 SM2 / SM3 / SM4 |
| 用途 | ① 互操作测试：用 tongsuo 生成 → 用本库解析（反向亦然）；② 夹具兜底生成 |

**不再使用上游 OpenSSL。** tongsuo 是其分支且命令兼容，但并非同一实现 —— 因此不再断言与上游 OpenSSL 的互操作。这一取舍已记为 [roadmap.md §8](roadmap.md) 的风险 R2；针对上游 OpenSSL 的可选、非阻塞交叉校验是备选出路。

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

CI 拆为两个 workflow，使分支检查与 tag 触发的发布相互独立、可分别审计。

### 6.1 `ci.yml` —— 分支检查

`.github/workflows/ci.yml`。触发器：**仅** `main` 的 push / pull_request，外加 `workflow_call`（被 `release.yml` 复用）和 `workflow_dispatch`（手动预热 tongsuo 缓存或在 feature 分支上做一次性验证）。

> **`dev` 故不覆盖。** dev 是集成/开发分支，CI 责任集中在 `main`。开发者需在本地用 `dotnet build DevTrove.Crypto.slnx -c Release` 与 `dotnet test tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0` 自行验证后再开 PR 进 `main`；也可在 feature 分支上手动 `workflow_dispatch` 跑一次。完整流水线（需 `tongsuo` 的集成测试与 `release.yml` 标签流程）只走另外两条路径。

顶部 `permissions: contents: read`。`concurrency: ci-<workflow>-<ref> cancel-in-progress: true`：同分支再次 push 时取消正在跑的旧实例。

Job：

| Job | 用途 | 说明 |
|---|---|---|
| `tongsuo` | 从源码编译并安装 Tongsuo 8.4.0，按 OS + 版本缓存（`actions/cache@v4`） | 见 `RM-0.0.9f`，`build` 必须等它完成 |
| `build` | 构建 + 单元测试 + 互操作测试 + 覆盖率；`push` 时（不含 PR）还会打包至 `./artifacts/` | `ubuntu-latest`，.NET 8.x / 9.x / 10.x |
| `test-abstractions` | 按目标框架矩阵跑契约测试 | `os ∈ {ubuntu-latest, windows-latest}` × `tfm ∈ {net8.0, net9.0, net10.0, net48}`，排除无效组合（`net48` 只在 Windows，其余只在 Linux）。**不需要 tongsuo** —— 契约测试无外部依赖 |

其中 `net48` 那一格就是使 `netstandard2.0` 资产获得**运行验证**的关键：`net48` 项目会解析 `lib/netstandard2.0/`。没有它，该资产就永远只能停在构建验证。

pack 步骤仅把 `*.nupkg` + `*.snupkg` 落到 `./artifacts/` 供本地检查；**不**推送到 nuget.org —— 推送由 `release.yml` 负责。

### 6.2 `release.yml` —— 标签触发的发布

`.github/workflows/release.yml`。仅在 `v*` 标签 push 时触发。`concurrency: release-<ref> cancel-in-progress: false`（发布进行中的运行不能被取消）。

workflow 含三个 job，严格按序依赖：

1. **`verify-version`** —— 守卫。读取 `Directory.Build.props`，与推送的标签（`${{ github.ref_name }}` 去前导 `v`）逐项比对四项不变量。**任一不匹配立即终止运行，绝不进入打包/推送阶段**：

   | 不变量 | 期望 |
   |---|---|
   | `<Version>` | 与标签版本一致（如 `v0.6.0` 对应 `<Version>0.6.0</Version>`） |
   | `<PackageLicenseExpression>` | `Apache-2.0`（与仓库 `LICENSE` 一致） |
   | `<TargetFrameworks>` | 包含全部 `netstandard2.0;net8.0;net9.0;net10.0` |
   | `<RepositoryUrl>` | 包含 `DevTrove.Crypto` |

   该 job 同时识别预发布标签（版本段含 `-`，如 `v1.0.0-rc.1`），把 `is-prerelease` 作为 job 输出暴露给下游。

   **版本不动态注入。** 维护者直接编辑 `Directory.Build.props` 中的 `<Version>`，机器负责把关。详见 `nuget.md §6.2`。

2. **`ci`** —— `needs: verify-version`，`uses: ./.github/workflows/ci.yml`。复用分支同等的编译 + 测试门禁，确保 tag 在 CI 通过前走不到 `release`。

3. **`release`** —— `needs: ci`，`environment: nuget`，`permissions: contents: write`。重新跑 `dotnet restore` + `dotnet build` + `dotnet pack`（故意重复 —— pack 几秒成本可控，省去跨 workflow 配 `upload-artifact` / `download-artifact` 的复杂度）。复用的是「CI 通过」这一事实，而非具体文件字节。

   顺序执行：

   1. `dotnet nuget push ./artifacts/DevTrove.Crypto.Abstractions.*.nupkg`（Abstractions 最先 —— Core 依赖它）
   2. `dotnet nuget push ./artifacts/DevTrove.Crypto.Core.*.nupkg`（Core —— 门包依赖它）
   3. `dotnet nuget push ./artifacts/DevTrove.Crypto.[0-9]*.nupkg`（门包）
   4. `softprops/action-gh-release@v2` 把 `artifacts/*.nupkg` + `artifacts/*.snupkg` 作为 GitHub Release 的附件上传；`fail_on_unmatched_files: true` 让缺失立即报错；`prerelease` 取自 `verify-version.outputs.is-prerelease`。

### 6.3 NuGet 发布顺序

（`DevTrove.Crypto.Core` 依赖 `DevTrove.Crypto.Abstractions`；`DevTrove.Crypto.Tls` 依赖 `DevTrove.Crypto`。）

1. `DevTrove.Crypto.Abstractions`
2. `DevTrove.Crypto.Core`
3. `DevTrove.Crypto`
4. `DevTrove.Crypto.Tls`

NuGet 不支持原子多包发布。为避免"依赖已升级但被依赖包尚未发布"的窗口期，应**先发被依赖包、后更新消费方**，或在 CI 中按上述顺序依次推送。

### 6.4 CI 已知偏差

- （`RM-0.0.6`）：release workflow 的 tag 触发端到端流程尚未用真实 tag 跑过
- （`RM-0.0.9f`，部分）：新增 `tongsuo` job，从源码编译并缓存 Tongsuo 8.4.0；集成步骤改用 tongsuo。端到端验证仍待 CI 实跑
- （`RM-0.0.9b` / `RM-0.0.9c`）：生成脚本中的 SM2 自签证书段与 SM2 CRL 段待 0.1.0 重建 Core 后的 SM2 生成流程落地

---

## 7. 打包

### 本地打包

```
dotnet pack src/DevTrove.Crypto.Abstractions/DevTrove.Crypto.Abstractions.csproj -c Release -o ./artifacts --include-symbols
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
- `DevTrove.Crypto.Core` 的 `.nuspec` 声明了 `<dependency id="DevTrove.Crypto.Abstractions" />`。解包看，不要假定 `ProjectReference` 已经变成了依赖。

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

目前 `generate-test-certs.sh` 与 `generate-test-crl.sh` **完全没有 SM2 段**，但这两类 SM2 夹具确实存在于工作区 —— 说明它们由某个已不在仓库里的东西产生，今天无法复现。这就是 `RM-0.0.9b` / `RM-0.0.9c`。

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
#    契约测试不需任何其他依赖；互操作测试需 tongsuo。
#    `netstandard2.0` 仍受 RM-0.1.0-01 阻塞。
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  tests/DevTrove.Crypto.Abstractions.Tests -c Release --framework net10.0

# 5. 打包（按依赖顺序）
dotnet pack src/DevTrove.Crypto.Abstractions/DevTrove.Crypto.Abstractions.csproj -c Release -o ./artifacts
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
