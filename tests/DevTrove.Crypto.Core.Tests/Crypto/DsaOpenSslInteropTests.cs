using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.TestSupport;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// DSA 与 OpenSSL 签名互操作测试（使用 tests/data/keys 固定素材）。
/// A. 代码签名 → openssl 验签；B. openssl 签名 → 代码验签。
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "OpenSSL")]
public class DsaOpenSslInteropTests : IDisposable
{
    private readonly string _tempDir;

    public DsaOpenSslInteropTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dsa_interop_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // 忽略清理错误。
            }
        }
    }

    private const string DsaPrivPem = "dsa-2048-private.pem";
    private const string DsaPubPem = "dsa-2048-public.pem";

    [Fact]
    public async Task Dsa_CodeSign_OpenSslVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "DSA sign from code"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        using var dsa = new DsaCrypto();
        dsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(DsaPrivPem))).ToDer());

        // Act - 代码签名（DER 编码）
        var signature = dsa.SignData(data, "SHA-256");
        await File.WriteAllBytesAsync(sigPath, signature);

        // openssl 验证
        var verify = await OpenSslCli.ExecuteCommandAsync(
            $"dgst -sha256 -verify \"{TestData.Keys(DsaPubPem)}\" -signature \"{sigPath}\" \"{dataPath}\"");

        // Assert
        verify.IsSuccess.Should().BeTrue(verify.FullOutput);
    }

    [Fact]
    public async Task Dsa_OpenSslSign_CodeVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "DSA sign from openssl"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        // Act - openssl 签名
        var sign = await OpenSslCli.ExecuteCommandAsync(
            $"dgst -sha256 -sign \"{TestData.Keys(DsaPrivPem)}\" -out \"{sigPath}\" \"{dataPath}\"");
        sign.IsSuccess.Should().BeTrue(sign.FullOutput);
        var signature = await File.ReadAllBytesAsync(sigPath);

        // 代码验证
        using var dsa = new DsaCrypto();
        dsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(DsaPrivPem))).ToDer());
        var verified = dsa.VerifyData(data, signature, "SHA-256");

        // Assert
        verified.Should().BeTrue();
    }
}
