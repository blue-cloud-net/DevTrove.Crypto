namespace DevTrove.Crypto.Core.Tests;

/// <summary>
/// 测试数据（tests/data）路径助手。
/// 素材由 <see cref="TestDataGenerator"/> 在测试程序集加载时调用脚本生成（不纳入版本控制），
/// 位于仓库根目录下的 tests/data/。
/// </summary>
public static class TestData
{
    private static readonly string Root = ResolveRoot();

    /// <summary>密钥素材目录。</summary>
    public static string Keys(string fileName) => Path.Combine(Root, "keys", fileName);

    /// <summary>证书素材目录。</summary>
    public static string Certs(string fileName) => Path.Combine(Root, "certs", fileName);

    /// <summary>CSR 素材目录。</summary>
    public static string Csrs(string fileName) => Path.Combine(Root, "csrs", fileName);

    /// <summary>CRL 素材目录。</summary>
    public static string Crls(string fileName) => Path.Combine(Root, "crls", fileName);

    /// <summary>PFX 素材目录。</summary>
    public static string Pfx(string fileName) => Path.Combine(Root, "pfx", fileName);

    /// <summary>
    /// 从测试输出目录向上回溯，定位仓库根目录下的 tests/data。
    /// </summary>
    private static string ResolveRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate the repository tests/data directory. Ensure tests run from a repository checkout and test data has been generated.");
    }
}
