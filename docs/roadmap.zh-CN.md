# 库路线图

> 英文默认入口：[roadmap.md](roadmap.md)

本文档负责 `DevTrove.Crypto` 的**版本线与状态追踪**。目标与工程规则不在本文件 —— 分别见 [architecture.md §1](architecture.md) 与 [standards.md](standards.md)。

---

## 1. 本文件的范围

| 属于本文件 | 属于其他文件 |
|---|---|
| 版本线：工作项 + 发布里程碑 | 能力目标 → [architecture.md §1](architecture.md) |
| 每个子项的状态与证据 | 编码 / 工程规则 → [standards.md](standards.md)、[AGENTS.md](../AGENTS.md) §4 |
| 排除项与非目标 | 构建 / 测试 / 打包命令 → [development-guide.md](development-guide.md) |
| 风险登记 | TLS 探测引擎设计 → [tls-scanner.md](tls-scanner.md) |

文档成对存在：英文默认（`x.md`）+ 中文（`x.zh-CN.md`），章节一一对应。

---

## 2. 状态图例与维护

| 标记 | 状态 | 判定标准 |
|---|---|---|
| ✅ | 已完成 | 该子项的**全部验收条件均已满足并经验证** —— 不是「代码写完」。验收依赖构建或测试时，必须附上证据。 |
| 🚧 | 处理中 | 正在实施，或已排入当前工作批次。 |
| 🟡 | 部分完成 | 已有部分落地，但尚未满足验收。 |
| ⬜ | 未开始 | 尚未动工。 |

**版本级状态汇总**（确定性，不留解释空间）：

- 全部子项 ✅ → ✅
- 存在 🚧 且未全部 ✅ → 🚧
- 无 🚧 但存在 🟡 → 🟡
- 全部子项 ⬜ → ⬜

**维护**：子项状态变化时，须与造成该变化的代码在同一次提交里更新。见 [AGENTS.md](../AGENTS.md) 与 [standards.md §9.3](standards.md) 的提交前检查。中英两份在同一次提交内更新。

---

## 3. 版本模型

遵循 SemVer。`0.x` 允许破坏性变更。

| 规则 | 说明 |
|---|---|
| `0.0.x` **仅为工作项编号** | 不打包、不打 tag、不发布。本仓库从未发布过任何包。 |
| 首次真实发布 | `0.1.0` |
| 包版本 | 每个里程碑内三个包共用同一版本号。`DevTrove.Crypto.Tls` 在 `0.4.0` 之前不存在，因此不参与更早的里程碑。 |
| 预发布 | 非最终构建使用 `-dev` / `-preview` 后缀。 |

库**独立版本号**；消费方声明最低可兼容版本（见 [nuget.md §3.2](nuget.md)）。

---

## 4. 版本总览

| 版本 | 主题 | 子项数 | 状态 |
|---|---|---|---|
| `RM-0.0.1` – `RM-0.0.13` | 基线修正（工作项） | 13 | 🟡 |
| `0.1.0` | 结构与一致性收敛 | 6 | ⬜ |
| `0.2.0` | 密码学原语补齐 | 8 | ⬜ |
| `0.3.0` | PKI 能力补齐 | 9 | ⬜ |
| `0.4.0` | TLS 探测 L1 + NTLS 指纹 | 11 | ⬜ |
| `0.5.0` | TLS 探测 L2 + 国密 | 9 | ⬜ |
| `1.0.0` | 稳定 API + PKIX | 5 | ⬜ |

---

## 5. 无版本基线条目

已完成（或进行中）但不承载版本号的条目。

| 条目 | 状态 | 说明 |
|---|---|---|
| 新建 `docs/standards.md` 与 `docs/library-api.md` | ✅ | README 与 AGENTS.md 已引用 |
| 删除 Web 时代的 `docs/v0.1/` | ✅ | — |
| 文档重构：独立库口径、版本线、状态追踪 | 🚧 | 有意不归属任何版本 |

---

## 6. 版本明细

每行一个子项。`证据` 列写明验收条件的证明方式。

### 6.1 `RM-0.0.1` —— TFM 集合对齐

`Directory.Build.props` 声明 5 个目标框架，Core 覆盖为 3 个，门包覆盖为 1 个。三处必须一致，且每个 TFM 都要单独验证。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.1 | 统一 props 与两个 csproj 的 `<TargetFrameworks>`；逐个 TFM 验证 | 5 个 TFM 各自 `dotnet build -f <tfm>` 成功 | ✅ | Core 与门包对 `netstandard2.0` / `netstandard2.1` / `net8.0` / `net9.0` / `net10.0` 逐个 `dotnet build -f <tfm>` 均成功（0 警告 0 错误） |

### 6.2 `RM-0.0.2` —— 门包无源码

门包项目仍含 `src/DevTrove.Crypto/Program.cs`，而设计要求它完全没有源码。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.2 | 删除 `Program.cs`，只保留 `ProjectReference` → Core | 项目内无任何 `.cs` 文件；包仍可构建与打包 | ✅ | `find src/DevTrove.Crypto -name '*.cs'` 无输出；`dotnet build src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -f net10.0` 成功（0 警告 0 错误）；`dotnet pack src/DevTrove.Crypto/DevTrove.Crypto.csproj -c Release -p:TargetFramework=net10.0` 产出 `DevTrove.Crypto.1.0.0.nupkg` 与 `.snupkg` |

### 6.3 `RM-0.0.3` —— 构建基线属性

`standards.md §2.3` 与 `development-guide.md §3` 写明了 `TreatWarningsAsErrors` 与 `EnforceCodeStyleInBuild`，但 `Directory.Build.props` 中两者都不存在。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.3 | 补上两个属性（或修正文档） | 配置与文档一致；构建行为与文档描述相符 | ✅ | Directory.Build.props 补齐 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` 与 `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`；`dotnet build DevTrove.Crypto.slnx -c Release` 全 5 TFM 成功（0 警告 0 错误） |

### 6.4 `RM-0.0.4` —— 包元数据

`<Version>`、`<PackageId>`、SourceLink 均缺失，而 `nuget.md §4` 称其为必需项。无 `<Version>` 打包会默认产出 `1.0.0`。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.4 | 正确声明 `<Version>`、`<PackageId>`、SourceLink 与 `RepositoryUrl` | `dotnet pack` 产出预期版本号；`.nupkg` 含 README 与 XML 文档 | ⬜ | 检查 `.nupkg` 内容 |

### 6.5 `RM-0.0.5` —— 解决方案文件

`DevTrove.Crypto.slnx` 引用了已删除的 `docs\v0.1\core-roadmap.md`，且只列出少数文档。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.5 | 删除失效引用；列出当前文档集 | 解决方案文件内每个路径均可解析 | ⬜ | 解析解决方案，无缺失文件 |

### 6.6 `RM-0.0.6` —— CI 工作流

`publish` 任务执行 `dotnet pack --no-build` 却没有前置构建；门包推送 glob `DevTrove.Crypto.*.nupkg` 会同时匹配 Core 包；工作流只在 `main` 触发；`actions/checkout` 仍请求 `submodules: recursive`，而本仓库没有子模块。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.6 | publish 增加构建步骤；收窄推送 glob；在集成分支触发；去掉 `submodules` | tag 触发的运行能正确构建、打包并发布两个包 | ⬜ | CI 运行（或试运行） |

### 6.7 `RM-0.0.7` —— TestSupport 目标 `net9.0`

`DevTrove.Crypto.TestSupport.csproj` 声明 `net8.0;net10.0`，而测试项目声明 `net8.0;net9.0;net10.0`。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.7 | 为 TestSupport 补上 `net9.0` | 三个 TFM 均可还原与构建 | ✅ | `dotnet build tests/DevTrove.Crypto.TestSupport/DevTrove.Crypto.TestSupport.csproj -c Release -f net8.0` / `net9.0` / `net10.0` 均成功（0 警告 0 错误） |

### 6.8 `RM-0.0.8` —— 默认签名算法

5 处公开签名的 `signatureAlgorithm` 默认值被硬编码为 `SHA256WITHRSA`：`Certificate.GenerateSelfSigned`、`Certificate.SignCsr`、`Certificate.SignPublicKey`、`CertificateRevocationList.Generate`、`CertificateSigningRequest.Generate`（两个重载）。这迫使 EC / DSA / SM2 调用方必须显式传入算法，不传则默认值错误。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.8 | 默认值按私钥算法推导 | 不再残留 `SHA256WITHRSA` 字面量；EC / DSA / SM2 无需显式指定算法即可签名 | ⬜ | 每种密钥类型的单元测试 |

本项必须**先于** `RM-0.1.0-01` 落地，使新的算法抽象直接吸收该推导，避免改两遍。

### 6.9 `RM-0.0.9` —— 外部工具统一为 tongsuo

夹具脚本与互操作测试当前同时依赖 `openssl` 与 `tongsuo`。此后 tongsuo 是唯一外部工具，脚本也必须改用 `TONGSUO_PATH`。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.9a | `generate-test-{keys,certs,csrs,crl,pfx}.sh` 改用 `TONGSUO_PATH` 而非 `openssl` | `scripts/` 内不再出现 `openssl` 调用 | ⬜ | `grep -rn openssl scripts/` 无输出 |
| RM-0.0.9b | `generate-test-certs.sh` 补 SM2 自签名证书段（当前仅有一行注释） | SM2 证书夹具可由脚本复现 | ⬜ | 在干净的 `tests/data/` 上重跑脚本 |
| RM-0.0.9c | `generate-test-crl.sh` 补 SM2 CRL 段（当前完全没有） | SM2 CRL 夹具可复现 | ⬜ | 重跑脚本 |
| RM-0.0.9d | 取消「独立 SM 脚本」概念；`TestDataGenerator` 按正常顺序生成 SM2 | `SmCertScript` 常量与其专属 `try/catch` 已删除 | ⬜ | `grep -rn generate-test-sm-certs` 无输出 |
| RM-0.0.9e | tongsuo 缺失时构建失败，而非跳过并警告 | 工具不可用时脚本以非零码退出 | ⬜ | 将 `TONGSUO_PATH` 指向不存在路径后运行 |
| RM-0.0.9f | CI 从源码编译 tongsuo，pin 版本并缓存产物 | 干净 runner 上集成阶段通过 | ⬜ | CI 运行 |

### 6.10 `RM-0.0.10` —— 夹具目录与说明

整个 `tests/data/` 由脚本生成并被 **Git 忽略** —— 没有任何夹具入库，连各目录的 `README.md` 也不入库。`TestData` 只为 5 个生成目录提供访问方法。脚本产不出的数据必须另找一个**可入库**的位置。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.10 | 新增**可入库**的 `tests/fixtures/ntls/` 放抓取的握手字节；把 `ocsp/` 加进生成集；补对应 `TestData` 方法；夹具清单写进 [development-guide.md §8](development-guide.md) 而非被忽略的文件里 | 抓取类夹具位于被忽略目录之外且受版本控制；生成集可由脚本重建 | ⬜ | 对新路径跑 `git check-ignore` + 在干净工作区重跑脚本 |

### 6.11 `RM-0.0.11` —— 5 个 TFM 真正可构建

`netstandard2.0/2.1` 从未产出程序集。polyfill 文件存在，但守卫写的是 `#if NETSTANDARD2_0`，把 `netstandard2.1` 排除在外；且 `Convert.FromHexString`、`RandomNumberGenerator.GetBytes(int)`、`AsSpan` 在 netstandard 目标下均无保护地使用。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.11a | 修正 polyfill 守卫（`NETSTANDARD2_0_OR_GREATER`） | `netstandard2.1` 可编译 | ⬜ | `dotnet build -f netstandard2.1` |
| RM-0.0.11b | 为两个 netstandard 目标补齐缺失的 polyfill / 包引用 | `dotnet build -f netstandard2.0` 与 `-f netstandard2.1` 均成功 | ⬜ | 构建输出 |
| RM-0.0.11c | 修正把 polyfill 写成位于 `Compat/`（实际不存在）的文档 | 文档与实际文件布局一致 | ⬜ | `grep -rn 'Compat/' docs/` 无输出 |

在 `RM-0.1.0-03` 移除 SM4 实现中 .NET 8 专属的 `TryEncryptEcbCore` / `TryEncryptCbcCore` 重写之前，先用 `#if NET8_0_OR_GREATER` 作为临时桥接。

### 6.12 `RM-0.0.12` —— 裁剪与 AOT 兼容（仅 net8.0 及以后）

枚举显示名经由 `[Display(ResourceType = typeof(RS))]` 与生成的 `ResourceManager` 解析，二者都走反射。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.12 | 为 net8.0+ 目标标注裁剪 / AOT 兼容，并为资源解析路径加注 | 发布 AOT 测试应用成功，且枚举显示名仍可解析 | ⬜ | AOT 发布 + 冒烟测试 |

两个 netstandard 目标不承载 AOT 元数据：它们承担兼容面，`net8.0` 及以后承担 AOT 面。

### 6.13 `RM-0.0.13` —— 编码规则对齐

`standards.md §2.4` 写明 LF 行尾、文件末尾换行与各扩展名缩进，但 `.editorconfig` 设的是 `end_of_line = crlf`、`insert_final_newline = false`，且没有为 Markdown、XML、JSON、YAML 定义任何段落。`.gitattributes` 也不存在，尽管 `standards.md §2.1` 列出了它。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.0.13a | 按 `standards.md §2.4` 改写 `.editorconfig`：LF、末尾换行、UTF-8，并为每种已记录的文件类型补齐段落 | §2.4 的每条规则都能在 `.editorconfig` 中找到对应项 | ⬜ | 两文件逐条比对 |
| RM-0.0.13b | 新增 `.gitattributes` | 所有贡献者检出时行尾被规范化 | ⬜ | `git check-attr` 抽查 |
| RM-0.0.13c | `.gitignore` 补 `artifacts/` | `artifacts/` 保持未跟踪，符合 `standards.md §10` | ⬜ | `git status --ignored` |
| RM-0.0.13d | 存量文件重规范化，**单独一个提交** | 重规范化后工作区干净 | ⬜ | 提交后 `git add --renormalize .` 不再产生差异 |

本项必须在 `standards.md` 定稿之后开始 —— 以文档为准，而非以配置文件为准。

---

### 6.14 `0.1.0` —— 结构与一致性收敛

不新增能力。公开类型面在此改变形态；库从未发布，因此改动零外部成本。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.1.0-01 | Core 结构重整：目录与命名空间（`Algorithms`、`Formats`、`Asn1`、`Interop`、`Compat`），BouncyCastle 类型撤出公开面，每类型一个 `Interop` 扩展，统一 `*Crypto` 命名 | 不再残留 `DevTrove.Crypto.Crypto.*` 或 `DevTrove.Crypto.BouncyCastle.*` 命名空间；无公开的 `GetBouncyCastle*` 成员 | ⬜ | 对 `src/` 检索旧命名空间 |
| RM-0.1.0-02 | 自建密码学抽象：`CipherModeKind` / `PaddingKind` 枚举、对称与摘要基类、BCL 适配器 `AsSymmetricAlgorithm()` / `AsHashAlgorithm()` | 无需 BCL 枚举即可表达 CTR 与 AEAD 模式 | ⬜ | 覆盖全部模式的单元测试 |
| RM-0.1.0-03 | 将 AES 与 SM4 迁到新抽象；对齐两者模式集合；ECB 均受支持、默认 CBC，并在 XML 注释中显式告警；移除 .NET 8 专属重写 | 两种算法暴露相同模式集合；`CryptoStream` 互操作仍可经适配器工作 | ⬜ | 模式矩阵测试 + 互操作测试 |
| RM-0.1.0-04 | 命名与布局配套：`GlobalUsings`、测试目录镜像、`library-api.md` 重写、`architecture.md` §4 / §5 重绘 | 文档与代码一致 | ⬜ | 文档与代码比对 |
| RM-0.1.0-05 | 可发布验收：5 个 TFM 全绿、打包产物完整、干净消费方项目可还原并调用 API | 可在空的 `netstandard2.0` 与 `net8.0` 项目中使用 | ⬜ | `dotnet pack` + 消费方冒烟测试 |
| RM-0.1.0-06 | 删除 `OpenSslCli`，成员并入 `TongsuoCli`；更新全部调用点、`CliToolGuard` 措辞，并把 `*OpenSslInteropTests` 文件改名为中性的 `*InteropTests` | 不再有任何 `OpenSslCli` 引用 | ⬜ | `grep -rn OpenSslCli tests/` 无输出 |

---

### 6.15 `0.2.0` —— 密码学原语补齐

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.2.0-01 | 密钥对能力接口（`ISigner`、`IKeyEncipherment`、`IKeyAgreement`）+ 新增 Ed25519、Ed448、X25519（X448 可选）。X25519 只做密钥协商、Ed25519 只做签名，因此用能力接口而非单一基类 | 每个算法只实现其真实具备的能力 | ⬜ | 接口清单测试 |
| RM-0.2.0-02 | 对称密钥对象：算法 + 密钥 + IV/Nonce + 内存清理 | 公开 API 中密钥不再是裸 `byte[]`；释放时清零密钥材料 | ⬜ | 释放行为测试 |
| RM-0.2.0-03 | HMAC：HMAC-SM3 与 HMAC-SHA256/384/512，不使用 BCL 的 `HashName` 反射工厂 | 与参考向量签名验签一致 | ⬜ | 已知答案测试 |
| RM-0.2.0-04 | CMAC 与 GMAC | 已知答案测试通过 | ⬜ | 已知答案测试 |
| RM-0.2.0-05 | KDF：HKDF-SHA256/384/512、PBKDF2（scrypt 可选） | RFC 5869 测试向量通过 | ⬜ | 已知答案测试 |
| RM-0.2.0-06 | 哈希家族：SHA-256/384/512 包装，接入自建摘要抽象 | 每个哈希与 SM3 形态一致 | ⬜ | API 清单 |
| RM-0.2.0-07 | 对称模式补齐：AES-CTR、AES key wrap（RFC 3394）、SM4-CTR、SM4-GCM | RFC 3394 与 GCM 测试向量通过 | ⬜ | 已知答案测试 |
| RM-0.2.0-08 | 安全随机数统一入口 | 不再有散落各处的随机数辅助方法 | ⬜ | API 清单 |

---

### 6.16 `0.3.0` —— PKI 能力补齐

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.3.0-01 | 证书链构建与验证，含环路与深度保护 | 真实站点链与自签 CA→leaf 链验证正确；互相签发的输入能终止 | ⬜ | 基于 `tests/data/certs/` 的回归 |
| RM-0.3.0-02 | 可分辨名称构建（结构化字段 → DN）与随机序列号 | 解析 → 构建 → 再解析稳定往返 | ⬜ | 往返测试 |
| RM-0.3.0-03 | 格式层：PEM/DER 自动识别、统一转换、多类型 PEM 束解析（私钥 + 证书） | 任意受支持输入都能被识别并转换，无需调用方指明格式 | ⬜ | 转换矩阵测试 |
| RM-0.3.0-04 | X.509 扩展写入：AIA、CertificatePolicies、NameConstraints、PolicyConstraints、SCT | 生成的证书携带这些扩展，且 `openssl`/`tongsuo` 能读回 | ⬜ | 互操作测试 |
| RM-0.3.0-05 | PKCS#12 生成可选参数：可选 KDF 与加密算法，默认 PBES2 + AES-256 | 生成文件能以所选参数打开 | ⬜ | 互操作测试 |
| RM-0.3.0-06 | CRL 能力加强：delta CRL、CRL 签名验证、CRL Number、issuing distribution point | 生成与解析的 CRL 携带并验证这些字段 | ⬜ | 互操作测试 |
| RM-0.3.0-07 | PKCS#7 / CMS SignedData 解析（生成可选） | 能解析其他工具产出的 CMS SignedData | ⬜ | 夹具测试 |
| RM-0.3.0-08 | OCSP 响应解析 | 能解析抓取的 OCSP 响应 | ⬜ | 夹具测试 |
| RM-0.3.0-09 | ASN.1 工具：DER 往返与 OID 映射 | 供 TLS 引擎的扩展解析使用 | ⬜ | 单元测试 |

---

### 6.17 `0.4.0` —— TLS 探测 L1 与 NTLS 指纹

引擎位于 `src/DevTrove.Crypto.Tls/`，该目录目前尚不存在。

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.4.0-01 | 创建 `DevTrove.Crypto.Tls` 项目：源码、包元数据、解决方案条目、对 `DevTrove.Crypto` 的最低版本依赖 | 项目可构建并作为独立包打包 | ⬜ | `dotnet pack` |
| RM-0.4.0-02 | 协议版本矩阵 | 对公开测试站点集的判定与 `openssl s_client` 一致 | ⬜ | 交叉验证 |
| RM-0.4.0-03 | 密码套件矩阵与弱套件识别 | 能枚举套件并标记弱套件 | ⬜ | 交叉验证 |
| RM-0.4.0-04 | 扩展指纹：SCT、OCSP staple、EMS、ALPN、session ticket、secure renegotiation | 在声明相应扩展的服务端上均能识别 | ⬜ | 交叉验证 |
| RM-0.4.0-05 | 服务端实发证书链 | 顺序与 `openssl s_client -showcerts` 一致 | ⬜ | 交叉验证 |
| RM-0.4.0-06 | 协商群与签名算法采集 | 取值与服务端实际选择一致 | ⬜ | 交叉验证 |
| RM-0.4.0-07 | `TlsRaw`：手工构造 ClientHello + 手写 ServerHello / Certificate / ServerKeyExchange 解析 | 能解析抓取的字节夹具 | ⬜ | 夹具测试 |
| RM-0.4.0-08 | NTLS 指纹：版本、套件、双证书是否存在、签名算法、曲线 | 对抓取的公开国密站点字节判定正确 | ⬜ | 夹具测试 |
| RM-0.4.0-09 | ROBOT oracle 探测 | 能区分存在漏洞与已加固的服务端 | ⬜ | 交叉验证 |
| RM-0.4.0-10 | SSLv2 ClientHello 探测 | 能识别对 SSLv2 记录有响应的服务端 | ⬜ | 交叉验证 |
| RM-0.4.0-11 | 优雅降级：服务端异常扩展绝不导致整场扫描失败 | 扫描完成并记录异常 | ⬜ | 故障注入测试 |

---

### 6.18 `0.5.0` —— TLS 探测 L2 与国密

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-0.5.0-01 | A~F 评级，每个扣分项都给出理由 | 每个测试站点的结果可解释 | ⬜ | 报告复核 |
| RM-0.5.0-02 | 客户端模拟矩阵：Chrome、Firefox、Safari、Edge、Java、Android | 判定与各客户端真实握手能力一致 | ⬜ | 交叉验证 |
| RM-0.5.0-03 | ALPN / HTTP2 探测 | 协商结果与服务端实际选择一致 | ⬜ | 交叉验证 |
| RM-0.5.0-04 | 证书透明度日志查询 | 能检测内嵌 SCT 与日志可用性 | ⬜ | 交叉验证 |
| RM-0.5.0-05 | DNS CAA 校验 | 报告 CAA 记录及其与签发 CA 的不匹配 | ⬜ | 交叉验证 |
| RM-0.5.0-06 | 国密套件矩阵 | 能枚举 SM2/SM3/SM4 套件 | ⬜ | 夹具与实网测试 |
| RM-0.5.0-07 | 国密站点专项报告：协商套件、双证书、签名算法、曲线 | 对抓取站点报告字段完整 | ⬜ | 报告复核 |
| RM-0.5.0-08 | RFC 8998（SM2-TLS 1.3）完整握手 | 能对支持的服务端完成握手 | ⬜ | 实网测试 |
| RM-0.5.0-09 | OCSP 响应签名验证 | 能验证签名与目标证书是否匹配 | ⬜ | 夹具测试 |

---

### 6.19 `1.0.0` —— 稳定 API 与 PKIX

| ID | 子项 | 验收 | 状态 | 证据 |
|---|---|---|---|---|
| RM-1.0.0-01 | API 稳定化：冻结公开类型面并记录兼容性基线 | 无主版本号提升即不得变更公开 API | ⬜ | API 差异报告 |
| RM-1.0.0-02 | PKIX 路径校验：AuthorityKeyIdentifier、KeyUsage、BasicConstraints、路径长度、策略与名称约束 | PKIX 会拒绝的链同样被拒绝 | ⬜ | PKIX 一致性用例 |
| RM-1.0.0-03 | 链构建的环路与深度保护 | 病态输入能终止 | ⬜ | 模糊式输入 |
| RM-1.0.0-04 | 评级算法及其依据文档化 | 每个扣分项都能追溯到成文规则 | ⬜ | 文档复核 |
| RM-1.0.0-05 | 完整 XML 文档与正式 NuGet 发布 | 包内附带文档；[nuget.md §8](nuget.md) 的发布清单通过 | ⬜ | 发布清单 |

---

## 7. 排除项与非目标

| 条目 | 原因 |
|---|---|
| 消费方侧的 `ProjectReference` / `PackageReference` 切换 | 属消费方集成模式，不是库自身行为 |
| 子模块 / 双仓工作流 | 本仓库独立存在；如何被消费是消费方的事 |
| L3 漏洞探测（Heartbleed、CCS Injection、Ticketbleed） | 需要自研 record 层与密钥派生；本库只做 ROBOT |
| NTLS 完整握手 | 需从零实现 TLS 1.2 子集；推迟到 `0.5.0` 之后 |
| 打包 `testssl.sh` | GPLv2；仅作可选的外部交叉验证 |
| 原生依赖，含 tongsuo P/Invoke | 与「纯托管、支持 AOT」的目标冲突 |

---

## 8. 风险登记

| # | 风险 | 影响 | 缓解 |
|---|---|---|---|
| R1 | CI 从源码编译 tongsuo 显著拉长流水线 | 反馈变慢、任务不稳定 | 缓存编译产物并 pin 版本 |
| R2 | 统一到 tongsuo 后不再验证与上游 OpenSSL 的互操作 | 上游 OpenSSL 特有的回归会被漏掉 | 文档如实说明；保留一个可选、不阻塞的交叉校验 |
| R3 | 全仓行尾重规范化产生极大 diff | 历史更难读 | 单独提交并在提交信息中注明 |
| R4 | 替换 BCL 抽象会触及测试与文档中的大量调用点 | 重构面大、易出错 | 先落地 `RM-0.0.8`；按算法逐个迁移到新抽象 |
| R5 | GB/T 38636（NTLS）细节无法从公开资料确认 | 检测判定可能不准 | 以抓取的真实站点字节为夹具；不确定项显式标注 |
| R6 | 经裁剪或 AOT 编译的消费方丢失枚举显示名 | 静默降级 | 为资源解析路径加注；用 AOT 发布冒烟测试验证 |
| R7 | 状态表与实际脱节 | 路线图沦为又一份误导性文档 | 状态更新纳入提交前检查（见 §2） |

---

## 9. 相关文档

| 文档 | 内容 |
|---|---|
| [architecture.md](architecture.md) | 定位与目标、分层、依赖方向、能力边界、技术决策 |
| [standards.md](standards.md) | 语言策略、工程文件、C# 规范、测试、Git、提交前检查 |
| [nuget.md](nuget.md) | 包边界、元数据、版本策略、发布流程 |
| [tls-scanner.md](tls-scanner.md) | TLS 探测引擎设计 |
| [development-guide.md](development-guide.md) | 构建 / 测试 / 打包命令、外部工具、夹具、CI |
| [library-api.md](library-api.md) | 公开 API 索引 |

