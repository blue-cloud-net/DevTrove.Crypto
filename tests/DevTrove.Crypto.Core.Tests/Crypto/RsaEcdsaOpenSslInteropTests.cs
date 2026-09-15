using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.TestSupport;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// RSA / ECDSA 与 OpenSSL 加解密、签名交叉验证（使用 tests/data/keys 固定素材）。
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "OpenSSL")]
public class RsaEcdsaOpenSslInteropTests : IDisposable
{
    private readonly string _tempDir;

    public RsaEcdsaOpenSslInteropTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"rsa_ec_interop_{Guid.NewGuid():N}");
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

    private const string RsaPrivPem = "rsa-2048-pkcs8.pem";
    private const string RsaPubPem = "rsa-2048-public.pem";
    private const string EcPrivPem = "ec-p256-pkcs8.pem";
    private const string EcPubPem = "ec-p256-public.pem";

    #region RSA 加解密

    [Fact]
    public async Task Rsa_Oaep_CodeEncrypt_OpenSslDecrypt_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var plaintext = "RSA OAEP interop from code"u8.ToArray();
        var plainPath = Path.Combine(_tempDir, "plain.txt");
        var cipherPath = Path.Combine(_tempDir, "cipher.bin");
        var outPath = Path.Combine(_tempDir, "out.txt");
        await File.WriteAllBytesAsync(plainPath, plaintext);

        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());

        // Act - 代码加密
        var ciphertext = rsa.Encrypt(plaintext, useOaep: true);
        await File.WriteAllBytesAsync(cipherPath, ciphertext);

        // openssl 解密
        var decrypt = await OpenSslCli.DecryptAsync(cipherPath, outPath, TestData.Keys(RsaPrivPem), useOaep: true);
        decrypt.IsSuccess.Should().BeTrue(decrypt.FullOutput);

        // Assert
        (await File.ReadAllBytesAsync(outPath)).Should().Equal(plaintext);
    }

    [Fact]
    public async Task Rsa_Oaep_OpenSslEncrypt_CodeDecrypt_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var plaintext = "RSA OAEP interop from openssl"u8.ToArray();
        var plainPath = Path.Combine(_tempDir, "plain.txt");
        var cipherPath = Path.Combine(_tempDir, "cipher.bin");
        await File.WriteAllBytesAsync(plainPath, plaintext);

        // Act - openssl 加密
        var encrypt = await OpenSslCli.EncryptAsync(plainPath, cipherPath, TestData.Keys(RsaPubPem), useOaep: true);
        encrypt.IsSuccess.Should().BeTrue(encrypt.FullOutput);
        var ciphertext = await File.ReadAllBytesAsync(cipherPath);

        // 代码解密
        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());
        var decrypted = rsa.Decrypt(ciphertext, useOaep: true);

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    #endregion

    #region RSA 签名验签

    [Fact]
    public async Task Rsa_Pss_CodeSign_OpenSslVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "RSA PSS sign from code"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());

        // Act - 代码签名
        var signature = rsa.SignData(data, "SHA-256", usePss: true);
        await File.WriteAllBytesAsync(sigPath, signature);

        // openssl 验证
        var verify = await OpenSslCli.VerifyDataAsync(dataPath, sigPath, TestData.Keys(RsaPubPem), usePss: true);

        // Assert
        verify.IsSuccess.Should().BeTrue(verify.FullOutput);
    }

    [Fact]
    public async Task Rsa_Pkcs1_OpenSslSign_CodeVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "RSA PKCS1 sign from openssl"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        // Act - openssl 签名（PKCS#1 v1.5）
        var sign = await OpenSslCli.SignDataAsync(dataPath, TestData.Keys(RsaPrivPem), sigPath, usePss: false);
        sign.IsSuccess.Should().BeTrue(sign.FullOutput);
        var signature = await File.ReadAllBytesAsync(sigPath);

        // 代码验证
        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());
        var verified = rsa.VerifyData(data, signature, "SHA-256", usePss: false);

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public async Task Rsa_Pkcs1_CodeSign_OpenSslVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "RSA PKCS1 sign from code"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());

        // Act - 代码签名（PKCS#1 v1.5）
        var signature = rsa.SignData(data, "SHA-256", usePss: false);
        await File.WriteAllBytesAsync(sigPath, signature);

        // openssl 验证
        var verify = await OpenSslCli.VerifyDataAsync(dataPath, sigPath, TestData.Keys(RsaPubPem), usePss: false);

        // Assert
        verify.IsSuccess.Should().BeTrue(verify.FullOutput);
    }

    [Fact]
    public async Task Rsa_Pss_OpenSslSign_CodeVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "RSA PSS sign from openssl"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        // Act - openssl 签名（PSS-SHA256）
        var sign = await OpenSslCli.SignDataAsync(dataPath, TestData.Keys(RsaPrivPem), sigPath, usePss: true);
        sign.IsSuccess.Should().BeTrue(sign.FullOutput);
        var signature = await File.ReadAllBytesAsync(sigPath);

        // 代码验证
        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());
        var verified = rsa.VerifyData(data, signature, "SHA-256", usePss: true);

        // Assert
        verified.Should().BeTrue();
    }

    [Theory]
    [InlineData("SHA-384")]
    [InlineData("SHA-512")]
    public async Task Rsa_Pss_CodeSign_OpenSslVerify_ShouldSucceed_ForHash(string hashAlgorithm)
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = System.Text.Encoding.UTF8.GetBytes($"RSA PSS sign from code ({hashAlgorithm})");
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());

        // Act - 代码签名（PSS + 指定哈希）
        var signature = rsa.SignData(data, hashAlgorithm, usePss: true);
        await File.WriteAllBytesAsync(sigPath, signature);

        // openssl 验证
        var verify = await OpenSslCli.VerifyDataAsync(dataPath, sigPath, TestData.Keys(RsaPubPem), hashAlgorithm, usePss: true);

        // Assert
        verify.IsSuccess.Should().BeTrue(verify.FullOutput);
    }

    [Theory]
    [InlineData("SHA-384")]
    [InlineData("SHA-512")]
    public async Task Rsa_Pss_OpenSslSign_CodeVerify_ShouldSucceed_ForHash(string hashAlgorithm)
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = System.Text.Encoding.UTF8.GetBytes($"RSA PSS sign from openssl ({hashAlgorithm})");
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        // Act - openssl 签名（PSS + 指定哈希）
        var sign = await OpenSslCli.SignDataAsync(dataPath, TestData.Keys(RsaPrivPem), sigPath, hashAlgorithm, usePss: true);
        sign.IsSuccess.Should().BeTrue(sign.FullOutput);
        var signature = await File.ReadAllBytesAsync(sigPath);

        // 代码验证
        using var rsa = new RsaCrypto();
        rsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(RsaPrivPem))).ToDer());
        var verified = rsa.VerifyData(data, signature, hashAlgorithm, usePss: true);

        // Assert
        verified.Should().BeTrue();
    }

    #endregion

    #region ECDSA 签名验签

    [Fact]
    public async Task Ecdsa_CodeSign_OpenSslVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "ECDSA sign from code"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        using var ecdsa = new EcdsaCrypto();
        ecdsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(EcPrivPem))).ToDer());

        // Act - 代码签名（DER 编码）
        var signature = ecdsa.SignData(data, "SHA-256");
        await File.WriteAllBytesAsync(sigPath, signature);

        // openssl 验证
        var verify = await OpenSslCli.ExecuteCommandAsync(
            $"dgst -sha256 -verify \"{TestData.Keys(EcPubPem)}\" -signature \"{sigPath}\" \"{dataPath}\"");

        // Assert
        verify.IsSuccess.Should().BeTrue(verify.FullOutput);
    }

    [Fact]
    public async Task Ecdsa_OpenSslSign_CodeVerify_ShouldSucceed()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        var data = "ECDSA sign from openssl"u8.ToArray();
        var dataPath = Path.Combine(_tempDir, "data.txt");
        var sigPath = Path.Combine(_tempDir, "sig.bin");
        await File.WriteAllBytesAsync(dataPath, data);

        // Act - openssl 签名
        var sign = await OpenSslCli.ExecuteCommandAsync(
            $"dgst -sha256 -sign \"{TestData.Keys(EcPrivPem)}\" -out \"{sigPath}\" \"{dataPath}\"");
        sign.IsSuccess.Should().BeTrue(sign.FullOutput);
        var signature = await File.ReadAllBytesAsync(sigPath);

        // 代码验证
        using var ecdsa = new EcdsaCrypto();
        ecdsa.ImportPrivateKey(AsymmetricPrivateKeyParameter.FromPem(File.ReadAllText(TestData.Keys(EcPrivPem))).ToDer());
        var verified = ecdsa.VerifyData(data, signature, "SHA-256");

        // Assert
        verified.Should().BeTrue();
    }

    #endregion
}
