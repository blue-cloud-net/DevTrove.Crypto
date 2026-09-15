# Standards

> 中文对照：[standards.zh-CN.md](standards.zh-CN.md)

This document defines the coding standards, engineering conventions and collaboration process for `DevTrove.Crypto`. All contributors (including AI assistants) must follow it before committing code.

---

## 1. Language policy

See [AGENTS.md](../AGENTS.md) §3 for the canonical language policy. Quick reference:

| Object | Language |
|---|---|
| `docs/*.md` | **English (default entry)** |
| `docs/*.zh-CN.md` | **Chinese**, one-to-one with English version |
| `README.md` / `CHANGELOG.md` | **English (default entry)** |
| `README.zh-CN.md` / `CHANGELOG.zh-CN.md` | **Chinese** |
| `AGENTS.md` (this repo) | **Chinese** |
| Code comments, XML doc comments | **Chinese** (internationalisation is not currently scheduled) |
| Log messages | **English** |
| Exception messages | **English, ending with a period** |
| Identifiers | English |
| Git commit messages | **English type prefix + Chinese description** |

---

## 2. Engineering files

### 2.1 Repository root files

| File | Purpose |
|---|---|
| `DevTrove.Crypto.slnx` | Solution (XML, not legacy `.sln`) |
| `global.json` | Lock .NET SDK version (`rollForward: latestMajor`) |
| `Directory.Build.props` | Global build properties (TFM, nullability, language version, NuGet metadata) |
| `Directory.Packages.props` | Central package version management (CPM) |
| `.editorconfig` | Code style |
| `.gitignore` | Ignore rules |
| `.gitattributes` | Line-ending normalisation — **not present yet**, see `RM-0.0.13` |
| `AGENTS.md` | Entry point for agents / contributors: authoritative docs, hard constraints, pre-submission checks |

### 2.2 Central package management (CPM)

All package versions are declared **only** in `Directory.Packages.props`:

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
</PropertyGroup>
<ItemGroup>
  <PackageVersion Include="BouncyCastle.Cryptography" Version="2.6.2" />
</ItemGroup>
```

Each project writes `<PackageReference Include="..." />` only — **never** with a `Version` attribute.

### 2.3 Global build properties

```xml
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

For library projects (`IsPackable=true`) additionally:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<IsPackable>true</IsPackable>
```

### 2.4 Line endings & encoding

| Type | Setting |
|---|---|
| All files | UTF-8, LF, trailing newline, no trailing whitespace |
| `*.cs` | 4-space indent, Allman braces (`csharp_new_line_before_open_brace = all`) |
| `*.{xml,csproj,props,targets,slnx}` | 2-space indent |
| `*.{json,yml,yaml}` | 2-space indent |
| `*.md` | Preserve trailing whitespace (Markdown line break semantics) |

> `.editorconfig` and `.gitattributes` have been rewritten to match this table (`RM-0.0.13` complete).

---

## 3. C# coding standards

### 3.1 File layout

- **One top-level type per file** (an enum and its extension methods may share a file).
- **File-scoped namespaces** are required:

  ```csharp
  namespace DevTrove.Crypto.X509;
  ```

- The namespace must match the directory structure.
- `using` directives go at the top of the file (no need to repeat with `ImplicitUsings`); `System.*` first.

### 3.2 Type design

| Rule | Note |
|---|---|
| Default `sealed` | Unless explicitly designed for inheritance |
| Immutability first | Use `record` or read-only properties for data models |
| `record` for value semantics | DTOs, result models, metadata |
| `class` for objects with behavior & lifecycle | Services, executors, engines |
| Prefer `IReadOnlyList<T>` / `IReadOnlyDictionary<TKey, TValue>` | Public APIs do not expose mutable collections |
| Don't expose `List<T>` / `Dictionary<TKey, TValue>` as return types | Prevents callers from mutating internal state |

### 3.3 Nullability

- `Nullable=enable`; no `#nullable disable`.
- Public API parameters use `ArgumentNullException.ThrowIfNull(x)` for explicit validation.
- Don't use `!` (null-forgiving) to silence warnings; if genuinely needed, add a comment explaining why.

### 3.4 Async programming

- Async methods end with `Async`; return `Task` / `Task<T>` / `ValueTask`.
- **All public async methods accept `CancellationToken ct = default`** and propagate it.
- `await` calls in library code add `.ConfigureAwait(false)`.
- Synchronously-completing hot paths use `ValueTask<T>`.
- **Forbidden**: `.Result` / `.Wait()` / `GetAwaiter().GetResult()` (deadlock risk).
- **Forbidden**: wrapping synchronous code in `Task.Run` to "fake" async.

### 3.5 Resource management

- Types holding unmanaged resources or `IDisposable` fields implement `IDisposable`; use `using` / `using var`.
- `Dispose` is idempotent.
- No finalizers unless directly holding an unmanaged handle.

### 3.6 Exceptions

| Scenario | Practice |
|---|---|
| Parameter is null | `ArgumentNullException.ThrowIfNull` |
| Argument out of range | `ArgumentOutOfRangeException` |
| Illegal state | `InvalidOperationException` |
| Known business failure | **Return a `ToolResult`-style failure result; do not throw** |
| Unexpected exception | Allowed to bubble up; host handles uniformly |

- Exception messages **must be English with a period at the end**.
- Do not swallow exceptions (no empty `catch`); if genuinely swallowed, log + comment the reason.
- Custom exceptions live in an `Exceptions/` directory and inherit from the semantically closest system exception.

### 3.7 Performance & memory

- Hot paths use `Span<T>` / `ReadOnlySpan<T>` / `Memory<T>`.
- Avoid unnecessary copies when handling bytes.
- Avoid allocations inside loops; use `ArrayPool<T>` when needed.
- String concatenation inside loops uses `StringBuilder`.

### 3.8 Cryptography-specific constraints

| Rule | Reason |
|---|---|
| Do **not** derive from `SymmetricAlgorithm`, `HashAlgorithm`, `AsymmetricAlgorithm` or `HMAC` | `CipherMode` is a closed enum with no CTR and no AEAD concept, and the `netstandard` targets lack the `net8.0` virtuals — inheriting them ties the library's capability set to a target framework. Use the built-in abstractions and expose BCL interop through adapters (see [architecture.md §8](architecture.md), decision D20). |
| BouncyCastle types must not appear in a public signature | The implementation has to stay replaceable; interop goes through the explicit extensions in `Interop/` (decision D21). |
| Algorithm proxies are named `<Algorithm>Crypto` | One rule for every algorithm (decision D23). |
| Key material is cleared on disposal | It must not outlive the object that owns it; see `SymmetricKey` in [roadmap.md](roadmap.md) §6.15. |
| Prefer `Span<T>` on hot paths, but keep the `netstandard` targets buildable | `System.Memory` supplies spans there; guard anything that only exists on `net8.0` and later with `#if`. |
| Code reachable from a trimmed or AOT-compiled consumer must avoid unannotated reflection | See `RM-0.0.12`. |

---

## 4. Naming

| Element | Convention | Example |
|---|---|---|
| Namespace / assembly | PascalCase, dot-separated | `DevTrove.Crypto` |
| Class / struct / record | PascalCase | `Certificate` |
| Interface | `I` + PascalCase | `IDisposable` |
| Method | PascalCase | `GenerateSelfSigned` |
| Property | PascalCase | `NotAfter` |
| Public field | **Not used** (use a property instead) | — |
| Private field | `_camelCase` | `_registry` |
| Local variable / parameter | camelCase | `privateKey` |
| Constant | PascalCase | `DefaultSerialNumberBytes` |
| Generic parameter | `T` + descriptor | `TResult` |
| Test project | `<Subject>.Tests` | `DevTrove.Crypto.Core.Tests` |
| Test method | `Method_Should_Behavior_When_Condition` | `GenerateSelfSigned_Should_Fail_When_KeyIsNull` |

---

## 5. Comments & XML documentation

- **All public members must have XML documentation** (libraries enable `GenerateDocumentationFile`; missing comments trigger CS1591).
- Comments are Chinese.
- Prefer `<inheritdoc/>` to inherit base / interface documentation.
- `<summary>` describes **what**, in one sentence — do not repeat the method name.
- Non-obvious choices, complex algorithms, performance considerations use `//` inline comments to explain **why**, not **what**.
- Do not keep commented-out code; Git preserves history.
- `TODO` / `HACK` / `FIXME` must carry context:

  ```csharp
  // TODO(tls): support GOST suites (RFC 9189); current scope is standard suites + ShangMi
  ```

---

## 6. Logging

- **Libraries don't log.** No `Microsoft.Extensions.Logging.Abstractions`, no built-in logger; the caller decides what to record.
- If library code genuinely needs to surface information, return it through the result object or a callback parameter.
- Exceptions already carry enough context.

---

## 7. Testing

- Framework: **xUnit** + **FluentAssertions** + coverlet for coverage.
- Method naming: `Method_Should_Behavior_When_Condition`.
- Structure: Arrange–Act–Assert, separated by blank lines.
- One test verifies one behavior.
- Test projects **mirror** the directory structure of the project under test.
- Test fixtures live under `tests/data/` and are accessed by relative path. That directory is **generated, never committed** (see [architecture.md §8](architecture.md), D18). Captured data that no script can produce goes to `tests/fixtures/`, which is version-controlled.
- **Do not write** tests that only assert "does not throw".
- Tests depending on external executables (tongsuo) have explicit failure semantics (see [development-guide.md §5.2](development-guide.md)).

> **Known deviation** (`RM-0.0.7`): the project uses xUnit 2.9.2 + FluentAssertions 6.12.1, and `DevTrove.Crypto.TestSupport` does not declare `net9.0` yet. An xUnit v3 upgrade is under consideration but is **not** currently scheduled.

---

## 8. Repository working mode

This repository is **standalone**: it is developed, tested and released on its own, and nothing in it depends on how or where it is consumed.

| Mode | How it works |
|---|---|
| Day-to-day | `dotnet build DevTrove.Crypto.slnx -c Release` and `dotnet test` run entirely inside this repository. |
| Release | Once CI is green, push a `v*` tag to trigger the publish job (see [development-guide.md §6](development-guide.md)). |

How a consumer references the package — project reference, package reference or a local feed — is a consumer-side decision and is deliberately not covered here.

---

## 9. Git

### 9.1 Branches

| Branch | Purpose |
|---|---|
| `main` | Stable branch, always buildable |
| `dev` | Integration branch; pre-release work lands here before it reaches `main` |
| `feature/<slug>` | Feature work |
| `fix/<slug>` | Bug fixes |
| `docs/<slug>` | Documentation-only changes |

### 9.2 Commits

Conventional Commits, **English type prefix + Chinese description**:

```
<type>(<scope>): <中文描述>
```

| type | Meaning |
|---|---|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation |
| `refactor` | Refactor (no behavior change) |
| `test` | Tests |
| `build` | Build / dependencies |
| `chore` | Misc |
| `perf` | Performance |

One commit, one thing. No unrelated formatting changes mixed in.

### 9.3 Pre-submission checks

- `dotnet build DevTrove.Crypto.slnx -c Release` — expect failures until `RM-0.0.1` and `RM-0.0.11` land; warnings must be zero for new code.
- `dotnet test DevTrove.Crypto.slnx -c Release` — green (or documented pre-existing failures).
- New / modified public members have Chinese XML doc comments.
- Touched `README.md` / `CHANGELOG.md` ⇒ sync the `.zh-CN.md`.
- Touched `docs/*.md` ⇒ sync the matching `*.zh-CN.md` (sections, tables, Mermaid, code samples all 1:1).
- **Changed the status of any item in [roadmap.md](roadmap.md)** ⇒ update it in the same commit, in both language versions.
- Architecture / package / naming / TFM / test-strategy changes ⇒ sync this repository's `docs/*` and `.zh-CN.md`.
- Logs and exceptions contain **no** key material, passphrases or input plaintext.

---

## 10. Prohibited

- ❌ Committing `bin/`, `obj/`, `artifacts/`, `.vs/`, `.vshistory/`, `*.pfx`
- ❌ Forcing `tests/data/` into version control (it is generated; see D18)
- ❌ Using `git add -A` / `git add .` (fixtures + residue easily miscommitted)
- ❌ Writing English in `docs/` and skipping chapters in `*.zh-CN.md` (or vice versa)
- ❌ Claiming capabilities that aren't implemented (docs must match code)
- ❌ Logging or throwing Chinese exception messages inside the library
- ❌ Placing source code or public API types inside the `DevTrove.Crypto` metapackage project

---

## 11. Related

| Document | Contents |
|---|---|
| [architecture.md](architecture.md) | Internal layering, dependency direction, capability boundaries |
| [nuget.md](nuget.md) | Package boundaries, versioning, release process |
| [tls-scanner.md](tls-scanner.md) | TLS probe engine design |
| [roadmap.md](roadmap.md) | Library phase plan + pending / known deviations |
| [development-guide.md](development-guide.md) | Build / test / pack / CI / fixture conventions |
| [library-api.md](library-api.md) | Public API index |
| [../AGENTS.md](../AGENTS.md) | Entry point: authoritative docs, hard constraints, pre-submission checks |
