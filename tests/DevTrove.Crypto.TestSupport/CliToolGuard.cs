using CliWrap;
using CliWrap.Buffered;

namespace DevTrove.Crypto.TestSupport;

/// <summary>
/// CLI 工具可用性守卫。
/// 当 openssl / tongsuo 在当前环境不可用时，直接使相关测试失败（而非跳过）。
/// </summary>
public static class CliToolGuard
{
    /// <summary>
    /// 断言指定的命令行工具可执行，否则抛出 <see cref="Xunit.Sdk.XunitException"/>。
    /// </summary>
    /// <param name="binaryPath">可执行文件路径。</param>
    public static void EnsureToolAvailable(string binaryPath)
    {
        try
        {
            var result = Cli.Wrap(binaryPath)
                .WithArguments("version")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync()
                .GetAwaiter()
                .GetResult();

            if (result.ExitCode != 0)
            {
                Xunit.Assert.Fail(
                    $"Tool '{binaryPath}' is not usable (exit code {result.ExitCode}): {result.StandardError}.");
            }
        }
        catch (Exception ex)
        {
            Xunit.Assert.Fail($"Tool '{binaryPath}' is not available. {ex.Message}");
        }
    }
}
