# 开发指南

> 英文默认入口：[development-guide.md](development-guide.md)

`DevTrove.Crypto` 的构建、测试、打包与 CI 约定。

---

## 1. 命令速查

| 动作 | 命令 |
|---|---|
| 全部构建（Release） | `dotnet build DevTrove.Crypto.slnx -c Release` |
| 全部测试 | `dotnet test DevTrove.Crypto.slnx -c Release` |
| 仅单元测试（跳过 OpenSSL 互操作） | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category!=Integration'` |
| 仅互操作测试 | `dotnet test DevTrove.Crypto.slnx -c Release --filter 'Category=Integration'` |
| 打包 Core | `dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts` |
| 打包门面包 | `dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -o ./artifacts` |
| 生成 PFX 夹具（CI 也跑） | `./scripts/generate-test-pfx.sh` |

外部依赖：`openssl` 3.x。缺失时测试**直接失败而非跳过**。

> **已知偏差**（见 [roadmap.md §7 B1、B2](roadmap.md)）：`dotnet build DevTrove.Crypto.slnx -c Release` 当前因 TFM 矛盾报 `NU1201`。

---

## 2. 项目布局

```
DevTrove.Crypto/
├─ src/
│  ├─ DevTrove.Crypto/         门面包（无源码 —— 见 roadmap B2）
│  ├─ DevTrove.Crypto.Core/    实现
│  └─ DevTrove.Crypto.Tls/     TLS 探测引擎（Phase 0 骨架）
├─ tests/
│  ├─ DevTrove.Crypto.Core.Tests/
│  └─ DevTrove.Crypto.TestSupport/   OpenSSL CLI 封装
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

<!-- 目标框架（目标 5；当前 3 + 1） -->
<TargetFrameworks>netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0</TargetFrameworks>
```

各 csproj 继承基线或自行覆盖（Core 当前覆盖为 3 TFM，门包覆盖为 1 TFM —— 见 roadmap B1）。

### `netstandard2.0` polyfill

roadmap B3：`Compat/` polyfill 目录**目前不存在**。README 中"在 `Compat/` 手写补丁"的宣称目前不成立。

选其一：

- 从全局 `<TargetFrameworks>` 中**移除** `netstandard2.0`（若没有消费方需要）
- 或**实际创建** `Compat/` 与所需 polyfill（`System.ComponentModel.Annotations` 等）并文档化清单

在此之前，`[netstandard2.0]` 的构建不是真的。

---

## 4. `ProjectReference` ↔ `PackageReference` 切换

> **已知偏差**（roadmap B6）：条件属性切换**计划中尚未实现**。当前所有消费方都使用 `ProjectReference`；打包产出的依赖元数据不完整。

计划形式：

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <UseCryptoProjectRef Condition="'$(UseCryptoProjectRef)' == ''">true</UseCryptoProjectRef>
</PropertyGroup>
```

```xml
<!-- 消费方 csproj -->
<ItemGroup Condition="'$(UseCryptoProjectRef)' == 'true'">
  <ProjectReference Include="..\..\..\lib\Crypto\src\DevTrove.Crypto\DevTrove.Crypto.csproj" />
</ItemGroup>
<ItemGroup Condition="'$(UseCryptoProjectRef)' != 'true'">
  <PackageReference Include="DevTrove.Crypto" Version="[1.2.0, )" />
</ItemGroup>
```

- 开发（默认）：`<UseCryptoProjectRef>true</UseCryptoProjectRef>` → 子模块 `ProjectReference`
- 打包/发布：`<UseCryptoProjectRef>false</UseCryptoProjectRef>` → 从 nuget.org 还原
- 本地联调未发布版本：本地 NuGet 源（文件夹或本地 feed）

**为何重要**：`ProjectReference` 无法自动转换为 NuGet 依赖。打包时若仍使用 `ProjectReference`，生成的包会缺少对 `DevTrove.Crypto` 的依赖声明，消费方还原失败。

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

> **已知偏差**（roadmap B11）：升级到 xUnit v3 在路线上。`DevTrove.Crypto.TestSupport.csproj` 当前未声明 `net9.0`；补上后 `Category!=Integration` 在 `net9.0` 与 `net10.0` 都生效。

### 5.2 `OpenSslCli`

互操作测试（密钥 / 证书 / CSR / CRL / PKCS#12 的生成与解析）依赖 `openssl` 可可执行。

| 项 | 说明 |
|---|---|
| 路径覆盖 | 环境变量 `OPENSSL_PATH`（助手默认从 PATH 查找 `openssl`） |
| 版本要求 | 必须支持 SM2 / SM3 / SM4（OpenSSL 3.x） |
| 用途 | ① 互操作测试：用 OpenSSL 生成 → 用本库解析（反向亦然）；② 夹具兜底生成 |

**已移除国密 CLI 依赖**（计划，roadmap B4）：设计目标是去掉对 `TongsuoCli` 的依赖、改用 OpenSSL 生成 SM2 夹具。当前 `TongsuoCli.cs` 仍在仓内；`tests/data/*/README.md` 多处引用 `scripts/generate-test-sm-certs.sh`，该脚本**不存在**。B4 解决前，需补脚本或改写引用。

### 5.3 工具缺失时的行为（重要）

`DevTrove.Crypto.TestSupport` 的测试基建在外部工具不可用时**直接失败（Fail），而非跳过（Skip）**。

这意味着：**CI 镜像必须安装 `openssl`**，否则所有互操作测试都会失败。

这是设计上的有意取舍：

| 做法 | 结果 |
|---|---|
| ✅ 失败 | 环境问题立刻暴露；不会出现"测试全绿但实际没测" |
| ❌ 跳过 | 容易长期掩盖问题；互操作缺陷在发布后才发现 |

若将来需要支持"无 openssl 环境"，应通过**显式的测试分类标记**（如 `[Trait("RequiresOpenSsl", "true")]`）按环境过滤；不是静默跳过。

### 5.4 交叉验证（不进 CI）

扫描器判定需要与独立实现比对。以下步骤**本地手动执行**，不进 CI：

1. 对公开测试站点扫描（过期证书、自签证书、主机名不匹配、弱套件、无 SNI、仅旧协议）
2. 用 `openssl s_client` 获取相同目标的协议 / + 证书链
3. 用 `testssl.sh` 获取相同目标的判定
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

Job： |

| Job | 用途 | 说明 |
|---|---|---|
| `build` | 构建 + 单元测试 + 互操作测试 + 覆盖率 + 打包至 `artifacts/` | `ubuntu-latest`，.NET 8.x / 9.x / 10.x；通过 `apt` 装 `openssl` 用于互操作 |
| `publish` | 标签触发：打包 + 推 nuget.org | 需 `NUGET_API_KEY` |

### CI 发布顺序

1. `DevTrove.Crypto.Core`
2. `DevTrove.Crypto`
3. `DevTrove.Crypto.Tls`

NuGet 不支持原子多包发布。为避免"依赖已升级但被依赖包尚未发布"的窗口期，应**先发被依赖包、后更新消费方**，或在 CI 中按上述顺序依次推送。

### CI 已知偏差（roadmap B7）

- `publish` job 用 `dotnet pack ... --no-build`，但缺少先行的 `dotnet build` 步
- Push Metapackage 的 glob `DevTrove.Crypto.*.nupkg` **会重复匹配** Core 包

修复建议：publish 加 `dotnet build`；Metapackage 推送 glob 改为 `DevTrove.Crypto.*[!Core]*.nupkg` 或按确切文件名推。

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

```
tests/data/
├── certs/        证书（真实站点链、自签链、SM2 证书）
├── crls/         CRL（含 SM2）
├── csrs/         CSR（含扩展组合、SM2）
├── keys/         密钥（RSA / EC / DSA / SM2，PEM + DER，含加密私钥）
├── pfx/          PKCS#12（口令保护，**不入库** —— 本地 / CI 现场生成）
├── ocsp/         OCSP 响应夹具（计划 —— 见 roadmap B5）
└── ntls/         NTLS 握手字节夹具（计划 —— 见 roadmap B5）
```

> roadmap B5：`ocsp/` 与 `ntls/` 目录尚未创建。

### 8.2 来源与策略

| 夹具 | 来源 | 入库 | 说明 |
|---|---|---|---|
| RSA / EC / DSA 密钥与证书 | 由 `openssl` 生成 | ✅ | 脚本可重复生成 |
| **SM2 密钥、证书、CSR、CRL** | 由 `openssl` 生成（B4 之后） | ✅ | **已生成的夹具直接入库**，避免 CI 依赖国密工具链 |
| PKCS#12 | 由 `openssl` 生成 | ❌ | `.gitignore` 忽略 `*.pfx`；**CI 现场生成** |
| 真实站点证书链 | 从公开站点抓取 | ✅ | 链解析与验证的回归 |
| **NTLS 握手字节** | 从公开国密站点抓取 | ✅ | 见 [tls-scanner.md §8.4](tls-scanner.md) |

**统一口令**：所有加密私钥与 PKCS#12 使用同一个测试口令（当前 `test1234`），在 `tests/data/<子目录>/README.md` 中说明。

### 8.3 夹具脚本

```
scripts/
├── generate-test-certs.sh       RSA / EC 自签 + CA+leaf 链
├── generate-test-crl.sh         CRL 含两个吊销条目 + 原因
├── generate-test-pfx.sh         私钥 + 证书 / 证书链 PFX（test1234）
├── generate-test-csrs.sh        含扩展组合的 CSR
├── generate-test-keys.sh        RSA / EC / DSA 密钥（PEM + DER，加密变体）
└── generate-test-sm-certs.sh    SM2 证书 / CSR（tongsuo，计划恢复 —— 见 B4）
```

> roadmap B4：`generate-test-sm-certs.sh` 不存在；`tests/data/*/README.md` 仍引用它。

### 8.4 NTLS 握手字节夹具（关键）

由于不使用原生国密协议栈，NTLS 检测的回归**无法通过 loopback 服务端完成**。替代方案：

| 手段 | 说明 |
|---|---|
| **静态字节夹具** | 从公开国密站点抓取真实的 `ServerHello` / `Certificate` / `ServerKeyExchange` 字节，存入 `tests/data/ntls/`，测试直接喂给解析器 |
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
# 1. 克隆（或在应用仓里用子模块）
git clone https://github.com/blue-cloud-net/DevTrove.Crypto.git
cd DevTrove.Crypto

# 2. 确认 PATH 上有 `openssl` 3.x（测试依赖）
openssl version

# 3. （可选）夹具漂移时重新生成
./scripts/generate-test-certs.sh
./scripts/generate-test-keys.sh
./scripts/generate-test-pfx.sh

# 4. 构建 + 测试
dotnet build DevTrove.Crypto.slnx -c Release
dotnet test  DevTrove.Crypto.slnx -c Release

# 5. 打包
dotnet pack src/DevTrove.Crypto.Core/DevTrove.Crypto.Core.csproj -c Release -o ./artifacts
dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj         -c Release -o ./artifacts
```

> 当前第 4 步的 `dotnet build DevTrove.Crypto.slnx -c Release` 因 NU1201 失败（roadmap B1、B2）。

---

## 10. 相关文档

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 项目布局、依赖方向、能力边界 |
| [standards.md](standards.md) | 编码规范 |
| [nuget.md](nuget.md) | 包边界、版本策略、发布流程 |
| [tls-scanner.md](tls-scanner.md) | TLS 探测引擎设计 |
| [roadmap.md](roadmap.md) | 阶段路线 + 待办 / 已知偏差 |
