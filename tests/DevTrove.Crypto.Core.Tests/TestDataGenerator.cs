using System.Runtime.CompilerServices;
using CliWrap;
using CliWrap.Buffered;

namespace DevTrove.Crypto.Core.Tests;

/// <summary>
/// 测试素材生成器。
/// 测试程序集加载时（module initializer）检查仓库 tests/data 是否已生成，
/// 缺失则依次调用 scripts/generate-test-*.sh 生成。测试素材不纳入版本控制。
/// </summary>
internal static class TestDataGenerator
{
    /// <summary>按依赖顺序执行的生成脚本（SM2 证书脚本单独处理）。</summary>
    private static readonly string[] GenerationScripts =
    [
        "generate-test-keys.sh",
        "generate-test-certs.sh",
        "generate-test-csrs.sh",
        "generate-test-crl.sh",
        "generate-test-pfx.sh",
    ];

    /// <summary>SM2 证书/CSR 生成脚本（依赖 tongsuo）。</summary>
    private const string SmCertScript = "generate-test-sm-certs.sh";

    /// <summary>判断素材是否已生成的哨兵文件（相对 tests/data）。</summary>
    private static readonly string[] SentinelFiles =
    [
        Path.Combine("keys", "rsa-2048-pkcs1.pem"),
        Path.Combine("keys", "ec-p256-pkcs8.pem"),
        Path.Combine("certs", "rsa-2048-selfsigned-ext.pem"),
        Path.Combine("certs", "dsa-2048-selfsigned.pem"),
        Path.Combine("csrs", "rsa-2048-basic.csr"),
        Path.Combine("csrs", "dsa-2048-basic.csr"),
        Path.Combine("crls", "test.crl"),
        Path.Combine("crls", "ec.crl"),
        Path.Combine("crls", "dsa.crl"),
        Path.Combine("pfx", "key-and-cert.pfx"),
    ];

    /// <summary>
    /// 测试程序集加载时调用：素材缺失则生成。
    /// </summary>
    [ModuleInitializer]
    internal static void EnsureTestDataGenerated()
    {
        var repoRoot = FindRepoRoot();
        var dataDir = Path.Combine(repoRoot, "tests", "data");
        if (IsGenerated(dataDir))
        {
            return;
        }

        var scriptsDir = Path.Combine(repoRoot, "scripts");

        foreach (var script in GenerationScripts)
        {
            RunScript(Path.Combine(scriptsDir, script));
        }

        // SM2 素材依赖 tongsuo；工具缺失时由 SM 相关测试自身（CliToolGuard）负责失败，此处忽略。
        try
        {
            RunScript(Path.Combine(scriptsDir, SmCertScript));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARN: 跳过 SM2 测试素材生成（{SmCertScript}）：{ex.Message}");
        }
    }

    private static bool IsGenerated(string dataDir)
    {
        return SentinelFiles.All(file => File.Exists(Path.Combine(dataDir, file)));
    }

    private static void RunScript(string scriptPath)
    {
        var result = Cli.Wrap("bash")
            .WithArguments(scriptPath)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync()
            .GetAwaiter()
            .GetResult();

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to generate test data via '{Path.GetFileName(scriptPath)}' (exit {result.ExitCode}): {result.StandardError}");
        }
    }

    /// <summary>
    /// 从测试输出目录向上回溯，定位包含 scripts/generate-test-keys.sh 的仓库根目录。
    /// </summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "scripts", "generate-test-keys.sh")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate the repository root (scripts/generate-test-keys.sh not found). Tests must run from a repository checkout.");
    }
}
