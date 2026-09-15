using DevTrove.Crypto.Crypto.Sm;
using DevTrove.Crypto.TestSupport;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace DevTrove.Crypto.Core.Tests.Crypto.Sm;

/// <summary>
/// SM2 与 OpenSSL 互操作性测试
/// 需要系统安装 OpenSSL 且支持 SM2
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Tongsuo")]
public class SM2OpenSslInteropTests : IDisposable
{
    private string _tempDir = null!;

    public SM2OpenSslInteropTests()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm2_test_{Guid.NewGuid():N}");
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
                // 忽略清理错误
            }
        }
    }

    #region OpenSSL 公钥识别测试

    [Fact]
    public async Task InteroperabilityWithOpenSSL_PublicKey_ShouldBeRecognized()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var publicKeyDer = sm2.ExportPublicKey();

        var publicKeyPath = Path.Combine(_tempDir, "public_key.der");
        await File.WriteAllBytesAsync(publicKeyPath, publicKeyDer);

        // Act - 使用 OpenSSL 查看密钥信息
        var result = await TongsuoCli.ViewKeyInfoAsync(publicKeyPath, isPublicKey: true);

        // Assert
        result.IsSuccess.Should().BeTrue($"OpenSSL should recognize the public key. Error: {result.StandardError}");
        result.StandardOutput.Should().Contain("SM2", "Key should be identified as SM2");
    }

    [Fact]
    public async Task InteroperabilityWithOpenSSL_PrivateKey_ShouldBeRecognized()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var privateKeyDer = sm2.ExportPrivateKey();

        var privateKeyPath = Path.Combine(_tempDir, "private_key.der");
        await File.WriteAllBytesAsync(privateKeyPath, privateKeyDer);

        // Act - 使用 OpenSSL 查看密钥信息
        var result = await TongsuoCli.ViewKeyInfoAsync(privateKeyPath, isPublicKey: false);

        // Assert
        result.IsSuccess.Should().BeTrue($"OpenSSL should recognize the private key. Error: {result.StandardError}");
        result.StandardOutput.Should().Contain("SM2", "Key should be identified as SM2");
    }

    #endregion

    #region 从 OpenSSL 导入密钥测试

    [Fact]
    public async Task ImportFromOpenSSL_PublicKey_ShouldSucceed()
    {
        // Arrange - 使用 OpenSSL 生成密钥对
        var privateKeyPem = Path.Combine(_tempDir, "private_key.pem");
        var publicKeyPem = Path.Combine(_tempDir, "public_key.pem");
        var publicKeyDer = Path.Combine(_tempDir, "public_key.der");

        var genResult = await TongsuoCli.GenerateSm2KeyPairAsync(privateKeyPem, publicKeyPem);
        genResult.IsSuccess.Should().BeTrue($"Failed to generate SM2 key pair. Error: {genResult.StandardError}");

        var convertResult = await TongsuoCli.ConvertPublicKeyPemToDerAsync(publicKeyPem, publicKeyDer);
        convertResult.IsSuccess.Should().BeTrue($"Failed to convert public key to DER. Error: {convertResult.StandardError}");

        var publicKeyBytes = await File.ReadAllBytesAsync(publicKeyDer);

        // Act - 导入到 SM2 类
        using var sm2 = new SM2();
        var importAction = () => sm2.ImportPublicKey(publicKeyBytes);

        // Assert
        importAction.Should().NotThrow();

        // 验证能够使用导入的公钥
        var exportedKey = sm2.ExportPublicKey();
        exportedKey.Should().Equal(publicKeyBytes);
    }

    [Fact]
    public async Task ImportFromOpenSSL_PrivateKey_ShouldSucceed()
    {
        // Arrange - 使用 OpenSSL 生成密钥对
        var privateKeyPem = Path.Combine(_tempDir, "private_key.pem");
        var privateKeyDer = Path.Combine(_tempDir, "private_key.der");

        var genResult = await TongsuoCli.GenerateSm2KeyPairAsync(privateKeyPem);
        genResult.IsSuccess.Should().BeTrue($"Failed to generate SM2 key pair. Error: {genResult.StandardError}");

        var convertResult = await TongsuoCli.ConvertPrivateKeyPemToDerAsync(privateKeyPem, privateKeyDer);
        convertResult.IsSuccess.Should().BeTrue($"Failed to convert private key to DER. Error: {convertResult.StandardError}");

        var privateKeyBytes = await File.ReadAllBytesAsync(privateKeyDer);

        // Act - 导入到 SM2 类
        using var sm2 = new SM2();
        var importAction = () => sm2.ImportPrivateKey(privateKeyBytes);

        // Assert
        importAction.Should().NotThrow();

        // 验证能够签名和加密
        var testData = Encoding.UTF8.GetBytes("Test data");
        var signAction = () => sm2.SignData(testData);
        signAction.Should().NotThrow();
    }

    #endregion

    #region 签名互操作性测试

    [Fact]
    public async Task InteroperabilityWithOpenSSL_SignWithSM2_VerifyWithOpenSSL()
    {
        // Arrange - 生成密钥对
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        var publicKeyDer = sm2.ExportPublicKey();
        var privateKeyDer = sm2.ExportPrivateKey();

        var publicKeyPath = Path.Combine(_tempDir, "public_key.der");
        var privateKeyPath = Path.Combine(_tempDir, "private_key.der");
        await File.WriteAllBytesAsync(publicKeyPath, publicKeyDer);
        await File.WriteAllBytesAsync(privateKeyPath, privateKeyDer);

        // 准备测试数据
        var testData = Encoding.UTF8.GetBytes("Hello SM2!");
        var dataPath = Path.Combine(_tempDir, "data.txt");
        await File.WriteAllBytesAsync(dataPath, testData);

        // Act - 使用 SM2 类签名
        var signature = sm2.SignData(testData);
        var signaturePath = Path.Combine(_tempDir, "signature.der");
        await File.WriteAllBytesAsync(signaturePath, signature);

        // 使用 OpenSSL 验证签名
        var verifyResult = await TongsuoCli.VerifyWithSm2Async(dataPath, signaturePath, publicKeyPath);

        // Assert
        verifyResult.IsSuccess.Should().BeTrue($"OpenSSL verification failed. Output: {verifyResult.FullOutput}");
    }

    [Fact]
    public async Task InteroperabilityWithOpenSSL_SignWithOpenSSL_VerifyWithSM2()
    {
        // Arrange - 使用 OpenSSL 生成密钥对
        var privateKeyPem = Path.Combine(_tempDir, "private_key.pem");
        var publicKeyPem = Path.Combine(_tempDir, "public_key.pem");
        var privateKeyDer = Path.Combine(_tempDir, "private_key.der");
        var publicKeyDer = Path.Combine(_tempDir, "public_key.der");

        var genResult = await TongsuoCli.GenerateSm2KeyPairAsync(privateKeyPem, publicKeyPem);
        genResult.IsSuccess.Should().BeTrue($"Failed to generate keys. Error: {genResult.StandardError}");

        await TongsuoCli.ConvertPrivateKeyPemToDerAsync(privateKeyPem, privateKeyDer);
        await TongsuoCli.ConvertPublicKeyPemToDerAsync(publicKeyPem, publicKeyDer);

        // 准备测试数据
        var testData = Encoding.UTF8.GetBytes("Hello SM2!");
        var dataPath = Path.Combine(_tempDir, "data.txt");
        await File.WriteAllBytesAsync(dataPath, testData);

        // Act - 使用 OpenSSL 签名
        var signaturePath = Path.Combine(_tempDir, "signature.der");
        var signResult = await TongsuoCli.SignWithSm2Async(dataPath, privateKeyPem, signaturePath);
        signResult.IsSuccess.Should().BeTrue($"OpenSSL signing failed. Error: {signResult.StandardError}");

        // 使用 SM2 类验证
        var publicKeyBytes = await File.ReadAllBytesAsync(publicKeyDer);
        var signatureBytes = await File.ReadAllBytesAsync(signaturePath);

        using var sm2 = new SM2();
        sm2.ImportPublicKey(publicKeyBytes);
        var verifyResult = sm2.VerifyData(testData, signatureBytes);

        // Assert
        verifyResult.Should().BeTrue("SM2 should verify OpenSSL signature");
    }

    [Fact]
    public async Task InteroperabilityWithOpenSSL_SignVerifyWithUserId()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        var publicKeyDer = sm2.ExportPublicKey();
        var privateKeyDer = sm2.ExportPrivateKey();

        var publicKeyPath = Path.Combine(_tempDir, "public_key.der");
        var privateKeyPath = Path.Combine(_tempDir, "private_key.der");
        await File.WriteAllBytesAsync(publicKeyPath, publicKeyDer);
        await File.WriteAllBytesAsync(privateKeyPath, privateKeyDer);

        var testData = Encoding.UTF8.GetBytes("Hello SM2 with UserID!");
        var userId = "testuser@example.com";
        var dataPath = Path.Combine(_tempDir, "data.txt");
        await File.WriteAllBytesAsync(dataPath, testData);

        // Act - 使用 SM2 类带 userId 签名
        var signature = sm2.SignData(testData, Encoding.UTF8.GetBytes(userId));
        var signaturePath = Path.Combine(_tempDir, "signature.der");
        await File.WriteAllBytesAsync(signaturePath, signature);

        // 使用 OpenSSL 带 userId 验证
        var verifyResult = await TongsuoCli.VerifyWithSm2Async(dataPath, signaturePath, publicKeyPath, userId);

        // Assert
        verifyResult.IsSuccess.Should().BeTrue($"OpenSSL verification with userId failed. Output: {verifyResult.FullOutput}");
    }

    #endregion

    #region 加密解密互操作性测试

    [Fact]
    public async Task InteroperabilityWithOpenSSL_EncryptWithSM2_DecryptWithOpenSSL()
    {
        // Arrange - 生成密钥对
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        var publicKeyDer = sm2.ExportPublicKey();
        var privateKeyDer = sm2.ExportPrivateKey();

        var publicKeyPath = Path.Combine(_tempDir, "public_key.der");
        var privateKeyPath = Path.Combine(_tempDir, "private_key.der");
        var publicKeyPem = Path.Combine(_tempDir, "public_key.pem");
        var privateKeyPem = Path.Combine(_tempDir, "private_key.pem");

        await File.WriteAllBytesAsync(publicKeyPath, publicKeyDer);
        await File.WriteAllBytesAsync(privateKeyPath, privateKeyDer);

        // 转换为 PEM 格式供 OpenSSL 使用
        await TongsuoCli.ConvertPublicKeyDerToPemAsync(publicKeyPath, publicKeyPem);
        await TongsuoCli.ConvertPrivateKeyDerToPemAsync(privateKeyPath, privateKeyPem);

        // 准备测试数据
        var plaintext = Encoding.UTF8.GetBytes("Secret message");
        var plaintextPath = Path.Combine(_tempDir, "plaintext.txt");
        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - 使用 SM2 类加密
        var ciphertext = sm2.Encrypt(plaintext);
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");
        await File.WriteAllBytesAsync(ciphertextPath, ciphertext);

        // 使用 OpenSSL 解密
        var decryptedPath = Path.Combine(_tempDir, "decrypted.txt");
        var decryptResult = await TongsuoCli.DecryptWithSm2Async(ciphertextPath, decryptedPath, privateKeyPem);

        // Assert
        decryptResult.IsSuccess.Should().BeTrue($"OpenSSL decryption failed. Error: {decryptResult.StandardError}");

        var decryptedData = await File.ReadAllBytesAsync(decryptedPath);
        decryptedData.Should().Equal(plaintext, "Decrypted data should match original plaintext");
    }

    [Fact]
    public async Task InteroperabilityWithOpenSSL_EncryptWithOpenSSL_DecryptWithSM2()
    {
        // Arrange - 使用 OpenSSL 生成密钥对
        var privateKeyPem = Path.Combine(_tempDir, "private_key.pem");
        var publicKeyPem = Path.Combine(_tempDir, "public_key.pem");
        var privateKeyDer = Path.Combine(_tempDir, "private_key.der");
        var publicKeyDer = Path.Combine(_tempDir, "public_key.der");

        var genResult = await TongsuoCli.GenerateSm2KeyPairAsync(privateKeyPem, publicKeyPem);
        genResult.IsSuccess.Should().BeTrue($"Failed to generate keys. Error: {genResult.StandardError}");

        await TongsuoCli.ConvertPrivateKeyPemToDerAsync(privateKeyPem, privateKeyDer);
        await TongsuoCli.ConvertPublicKeyPemToDerAsync(publicKeyPem, publicKeyDer);

        // 准备测试数据
        var plaintext = Encoding.UTF8.GetBytes("Secret message from OpenSSL");
        var plaintextPath = Path.Combine(_tempDir, "plaintext.txt");
        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - 使用 OpenSSL 加密
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");
        var encryptResult = await TongsuoCli.EncryptWithSm2Async(plaintextPath, ciphertextPath, publicKeyPem);
        encryptResult.IsSuccess.Should().BeTrue($"OpenSSL encryption failed. Error: {encryptResult.StandardError}");

        // 使用 SM2 类解密
        var privateKeyBytes = await File.ReadAllBytesAsync(privateKeyDer);
        var ciphertextBytes = await File.ReadAllBytesAsync(ciphertextPath);

        using var sm2 = new SM2();
        sm2.ImportPrivateKey(privateKeyBytes);
        var decryptedData = sm2.Decrypt(ciphertextBytes);

        // Assert
        decryptedData.Should().Equal(plaintext, "Decrypted data should match original plaintext");
    }

    #endregion
}
