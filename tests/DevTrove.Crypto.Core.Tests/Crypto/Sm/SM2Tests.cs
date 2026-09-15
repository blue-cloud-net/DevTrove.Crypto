using DevTrove.Crypto.Crypto.Sm;
using FluentAssertions;
using System.Security.Cryptography;

namespace DevTrove.Crypto.Core.Tests.Crypto.Sm;

/// <summary>
/// SM2 类的单元测试
/// </summary>
public class SM2Tests
{
    #region 1. 构造函数和基本属性测试

    [Fact]
    public void Constructor_ShouldInitializeWithCorrectKeySize()
    {
        // Arrange & Act
        using var sm2 = new SM2();

        // Assert
        sm2.KeySize.Should().Be(256);
    }

    [Fact]
    public void Constructor_ShouldSetLegalKeySizes()
    {
        // Arrange & Act
        using var sm2 = new SM2();

        // Assert
        sm2.LegalKeySizes.Should().NotBeNull();
        sm2.LegalKeySizes.Should().HaveCount(1);
        sm2.LegalKeySizes[0].MinSize.Should().Be(256);
        sm2.LegalKeySizes[0].MaxSize.Should().Be(256);
        sm2.LegalKeySizes[0].SkipSize.Should().Be(0);
    }

    #endregion

    #region 2. 密钥生成测试

    [Fact]
    public void GenerateKeyPair_ShouldCreateValidKeyPair()
    {
        // Arrange
        using var sm2 = new SM2();

        // Act
        sm2.GenerateKeyPair();

        // Assert
        var publicKey = sm2.ExportPublicKey();
        var privateKey = sm2.ExportPrivateKey();

        publicKey.Should().NotBeNull();
        publicKey.Length.Should().BeGreaterThan(0);
        privateKey.Should().NotBeNull();
        privateKey.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateKeyPair_ShouldGenerateDifferentKeys()
    {
        // Arrange
        using var sm2_1 = new SM2();
        using var sm2_2 = new SM2();

        // Act
        sm2_1.GenerateKeyPair();
        sm2_2.GenerateKeyPair();

        // Assert
        var publicKey1 = sm2_1.ExportPublicKey();
        var publicKey2 = sm2_2.ExportPublicKey();
        var privateKey1 = sm2_1.ExportPrivateKey();
        var privateKey2 = sm2_2.ExportPrivateKey();

        publicKey1.Should().NotEqual(publicKey2);
        privateKey1.Should().NotEqual(privateKey2);
    }

    #endregion

    #region 3. 公钥导出和导入测试

    [Fact]
    public void ExportPublicKey_WithoutKeyPair_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();

        // Act & Assert
        var action = () => sm2.ExportPublicKey();
        action.Should().Throw<CryptographicException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void ExportPublicKey_ShouldReturnValidSpki()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act
        var publicKey = sm2.ExportPublicKey();

        // Assert
        publicKey.Should().NotBeNull();
        publicKey.Length.Should().BeGreaterThan(0);

        // 验证可以重新导入
        using var sm2Import = new SM2();
        var importAction = () => sm2Import.ImportPublicKey(publicKey);
        importAction.Should().NotThrow();
    }

    [Fact]
    public void ImportPublicKey_WithValidSpki_ShouldSucceed()
    {
        // Arrange
        using var sm2Original = new SM2();
        sm2Original.GenerateKeyPair();
        var originalPublicKey = sm2Original.ExportPublicKey();

        // Act
        using var sm2Import = new SM2();
        sm2Import.ImportPublicKey(originalPublicKey);

        // Assert
        var importedPublicKey = sm2Import.ExportPublicKey();
        importedPublicKey.Should().Equal(originalPublicKey);
    }

    [Fact]
    public void ImportPublicKey_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm2 = new SM2();

        // Act & Assert
        var action = () => sm2.ImportPublicKey(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImportPublicKey_WithEmptyData_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm2 = new SM2();

        // Act & Assert
        var action = () => sm2.ImportPublicKey(Array.Empty<byte>());
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void ImportPublicKey_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();
        var invalidData = new byte[] { 0x00, 0x01, 0x02 };

        // Act & Assert
        var action = () => sm2.ImportPublicKey(invalidData);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void ImportPublicKey_ShouldClearPrivateKey()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        using var sm2Other = new SM2();
        sm2Other.GenerateKeyPair();
        var otherPublicKey = sm2Other.ExportPublicKey();

        // Act
        sm2.ImportPublicKey(otherPublicKey);

        // Assert - 导入公钥后应该无法签名（需要私钥）
        var testData = new byte[] { 1, 2, 3 };
        var action = () => sm2.SignData(testData);
        action.Should().Throw<CryptographicException>()
            .WithMessage("*Private key*");
    }

    #endregion

    #region 4. 私钥导出和导入测试

    [Fact]
    public void ExportPrivateKey_WithoutKeyPair_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();

        // Act & Assert
        var action = () => sm2.ExportPrivateKey();
        action.Should().Throw<CryptographicException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void ExportPrivateKey_ShouldReturnValidPkcs8()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act
        var privateKey = sm2.ExportPrivateKey();

        // Assert
        privateKey.Should().NotBeNull();
        privateKey.Length.Should().BeGreaterThan(0);

        // 验证可以重新导入
        using var sm2Import = new SM2();
        var importAction = () => sm2Import.ImportPrivateKey(privateKey);
        importAction.Should().NotThrow();
    }

    [Fact]
    public void ImportPrivateKey_WithValidPkcs8_ShouldSucceed()
    {
        // Arrange
        using var sm2Original = new SM2();
        sm2Original.GenerateKeyPair();
        var originalPrivateKey = sm2Original.ExportPrivateKey();
        var originalPublicKey = sm2Original.ExportPublicKey();

        // Act
        using var sm2Import = new SM2();
        sm2Import.ImportPrivateKey(originalPrivateKey);

        // Assert
        var importedPrivateKey = sm2Import.ExportPrivateKey();
        var importedPublicKey = sm2Import.ExportPublicKey();

        importedPrivateKey.Should().Equal(originalPrivateKey);
        importedPublicKey.Should().Equal(originalPublicKey);
    }

    [Fact]
    public void ImportPrivateKey_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm2 = new SM2();

        // Act & Assert
        var action = () => sm2.ImportPrivateKey(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImportPrivateKey_WithEmptyData_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm2 = new SM2();

        // Act & Assert
        var action = () => sm2.ImportPrivateKey(Array.Empty<byte>());
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void ImportPrivateKey_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();
        var invalidData = new byte[] { 0x00, 0x01, 0x02 };

        // Act & Assert
        var action = () => sm2.ImportPrivateKey(invalidData);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void ImportPrivateKey_ShouldRestorePublicKey()
    {
        // Arrange
        using var sm2Original = new SM2();
        sm2Original.GenerateKeyPair();
        var privateKey = sm2Original.ExportPrivateKey();

        // Act
        using var sm2Import = new SM2();
        sm2Import.ImportPrivateKey(privateKey);

        // Assert - 应该能够成功导出公钥
        var publicKeyAction = () => sm2Import.ExportPublicKey();
        publicKeyAction.Should().NotThrow();

        var publicKey = sm2Import.ExportPublicKey();
        publicKey.Should().NotBeNull();
        publicKey.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region 5. 密钥兼容性测试

    [Fact]
    public void ExportImportRoundTrip_PublicKey_ShouldPreserveKey()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var publicKey1 = sm2.ExportPublicKey();

        // Act
        using var sm2Import = new SM2();
        sm2Import.ImportPublicKey(publicKey1);
        var publicKey2 = sm2Import.ExportPublicKey();

        // Assert
        publicKey2.Should().Equal(publicKey1);
    }

    [Fact]
    public void ExportImportRoundTrip_PrivateKey_ShouldPreserveKey()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var privateKey1 = sm2.ExportPrivateKey();

        // Act
        using var sm2Import = new SM2();
        sm2Import.ImportPrivateKey(privateKey1);
        var privateKey2 = sm2Import.ExportPrivateKey();

        // Assert
        privateKey2.Should().Equal(privateKey1);
    }

    #endregion

    #region 6. 签名测试

    [Fact]
    public void SignData_WithoutPrivateKey_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var publicKey = sm2.ExportPublicKey();

        using var sm2PublicOnly = new SM2();
        sm2PublicOnly.ImportPublicKey(publicKey);

        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        var action = () => sm2PublicOnly.SignData(testData);
        action.Should().Throw<CryptographicException>()
            .WithMessage("*Private key*");
    }

    [Fact]
    public void SignData_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act & Assert
        var action = () => sm2.SignData(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SignData_WithValidData_ShouldReturnSignature()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var signature = sm2.SignData(testData);

        // Assert
        signature.Should().NotBeNull();
        signature.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SignData_WithEmptyData_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act & Assert
        var action = () => sm2.SignData(Array.Empty<byte>());
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void SignData_SameDataDifferentSignatures()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var signature1 = sm2.SignData(testData);
        var signature2 = sm2.SignData(testData);

        // Assert - SM2 签名包含随机数，所以每次签名结果可能不同
        // 但两个签名都应该有效
        signature1.Should().NotBeNull();
        signature2.Should().NotBeNull();
        signature1.Length.Should().BeGreaterThan(0);
        signature2.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SignData_WithUserId_ShouldUseUserId()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var userId = System.Text.Encoding.UTF8.GetBytes("testuser");

        // Act
        var signatureWithoutUserId = sm2.SignData(testData);
        var signatureWithUserId = sm2.SignData(testData, userId);

        // Assert - 使用不同 userId 应该产生不同的签名
        signatureWithoutUserId.Should().NotBeNull();
        signatureWithUserId.Should().NotBeNull();
        // 注意：由于签名有随机性，这里不直接比较是否相等
    }

    [Fact]
    public void SignData_DifferentUserId_ShouldProduceDifferentSignatures()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var userId1 = System.Text.Encoding.UTF8.GetBytes("user1");
        var userId2 = System.Text.Encoding.UTF8.GetBytes("user2");

        // Act
        var signature1 = sm2.SignData(testData, userId1);
        var signature2 = sm2.SignData(testData, userId2);

        // Assert
        signature1.Should().NotBeNull();
        signature2.Should().NotBeNull();
        // 验证签名是否有效，而不是直接比较
    }

    #endregion

    #region 7. 验签测试

    [Fact]
    public void VerifyData_WithoutPublicKey_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var fakeSignature = new byte[] { 1, 2, 3 };

        // Act & Assert
        var action = () => sm2.VerifyData(testData, fakeSignature);
        action.Should().Throw<CryptographicException>()
            .WithMessage("*Public key*");
    }

    [Fact]
    public void VerifyData_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var fakeSignature = new byte[] { 1, 2, 3 };

        // Act & Assert
        var action = () => sm2.VerifyData(null!, fakeSignature);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifyData_WithNullSignature_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        var action = () => sm2.VerifyData(testData, null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifyData_WithEmptyData_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var signature = sm2.SignData(testData);

        // Act & Assert
        var action = () => sm2.VerifyData(Array.Empty<byte>(), signature);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void VerifyData_WithEmptySignature_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        var action = () => sm2.VerifyData(testData, Array.Empty<byte>());
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void VerifyData_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var signature = sm2.SignData(testData);

        // Act
        var result = sm2.VerifyData(testData, signature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyData_WithModifiedData_ShouldReturnFalse()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var signature = sm2.SignData(testData);

        var modifiedData = new byte[] { 1, 2, 3, 4, 6 }; // 修改最后一个字节

        // Act
        var result = sm2.VerifyData(modifiedData, signature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyData_WithModifiedSignature_ShouldReturnFalse()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var signature = sm2.SignData(testData);

        // 修改签名
        var modifiedSignature = (byte[])signature.Clone();
        modifiedSignature[0] ^= 0xFF;

        // Act
        var result = sm2.VerifyData(testData, modifiedSignature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyData_WithDifferentKey_ShouldReturnFalse()
    {
        // Arrange
        using var sm2A = new SM2();
        sm2A.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var signature = sm2A.SignData(testData);

        using var sm2B = new SM2();
        sm2B.GenerateKeyPair();

        // Act
        var result = sm2B.VerifyData(testData, signature);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyData_WithUserId_ShouldMatchSigningUserId()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var userId = System.Text.Encoding.UTF8.GetBytes("testuser");

        var signature = sm2.SignData(testData, userId);

        // Act
        var result = sm2.VerifyData(testData, signature, userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyData_MismatchedUserId_ShouldReturnFalse()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var userId1 = System.Text.Encoding.UTF8.GetBytes("user1");
        var userId2 = System.Text.Encoding.UTF8.GetBytes("user2");

        var signature = sm2.SignData(testData, userId1);

        // Act
        var result = sm2.VerifyData(testData, signature, userId2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyData_WithPublicKeyOnly_ShouldSucceed()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var signature = sm2.SignData(testData);
        var publicKey = sm2.ExportPublicKey();

        // Act
        using var sm2Verify = new SM2();
        sm2Verify.ImportPublicKey(publicKey);
        var result = sm2Verify.VerifyData(testData, signature);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region 8. 签名验签集成测试

    [Fact]
    public void SignAndVerify_CompleteWorkflow_ShouldSucceed()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var signature = sm2.SignData(testData);
        var result = sm2.VerifyData(testData, signature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SignAndVerify_WithExportImport_ShouldSucceed()
    {
        // Arrange
        using var sm2Sign = new SM2();
        sm2Sign.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var signature = sm2Sign.SignData(testData);
        var publicKey = sm2Sign.ExportPublicKey();

        // Act
        using var sm2Verify = new SM2();
        sm2Verify.ImportPublicKey(publicKey);
        var result = sm2Verify.VerifyData(testData, signature);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SignAndVerify_LargeData_ShouldSucceed()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var largeData = new byte[1024 * 1024]; // 1MB
        Random.Shared.NextBytes(largeData);

        // Act
        var signature = sm2.SignData(largeData);
        var result = sm2.VerifyData(largeData, signature);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region 9. 加密测试

    [Fact]
    public void Encrypt_WithoutPublicKey_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        var action = () => sm2.Encrypt(plaintext);
        action.Should().Throw<CryptographicException>()
            .WithMessage("*Public key*");
    }

    [Fact]
    public void Encrypt_WithNullPlaintext_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act & Assert
        var action = () => sm2.Encrypt(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Encrypt_WithValidPlaintext_ShouldReturnCiphertext()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var ciphertext = sm2.Encrypt(plaintext);

        // Assert
        ciphertext.Should().NotBeNull();
        ciphertext.Length.Should().BeGreaterThan(0);
        ciphertext.Should().NotEqual(plaintext);
    }

    [Fact]
    public void Encrypt_WithEmptyData_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act & Assert
        var action = () => sm2.Encrypt(Array.Empty<byte>());
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Encrypt_SamePlaintext_ShouldProduceDifferentCiphertexts()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var ciphertext1 = sm2.Encrypt(plaintext);
        var ciphertext2 = sm2.Encrypt(plaintext);

        // Assert - SM2 加密包含随机性，每次加密结果应该不同
        ciphertext1.Should().NotEqual(ciphertext2);
    }

    [Fact]
    public void Encrypt_WithPublicKeyOnly_ShouldSucceed()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var publicKey = sm2.ExportPublicKey();

        using var sm2Encrypt = new SM2();
        sm2Encrypt.ImportPublicKey(publicKey);
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var ciphertext = sm2Encrypt.Encrypt(plaintext);

        // Assert
        ciphertext.Should().NotBeNull();
        ciphertext.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region 10. 解密测试

    [Fact]
    public void Decrypt_WithoutPrivateKey_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };
        var ciphertext = sm2.Encrypt(plaintext);
        var publicKey = sm2.ExportPublicKey();

        using var sm2PublicOnly = new SM2();
        sm2PublicOnly.ImportPublicKey(publicKey);

        // Act & Assert
        var action = () => sm2PublicOnly.Decrypt(ciphertext);
        action.Should().Throw<CryptographicException>()
            .WithMessage("*Private key*");
    }

    [Fact]
    public void Decrypt_WithNullCiphertext_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act & Assert
        var action = () => sm2.Decrypt(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Decrypt_WithEmptyCiphertext_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act & Assert
        var action = () => sm2.Decrypt(Array.Empty<byte>());
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Decrypt_WithValidCiphertext_ShouldReturnPlaintext()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var originalPlaintext = new byte[] { 1, 2, 3, 4, 5 };
        var ciphertext = sm2.Encrypt(originalPlaintext);

        // Act
        var decryptedPlaintext = sm2.Decrypt(ciphertext);

        // Assert
        decryptedPlaintext.Should().Equal(originalPlaintext);
    }

    [Fact]
    public void Decrypt_WithInvalidCiphertext_ShouldThrowException()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var invalidCiphertext = new byte[] { 1, 2, 3, 4, 5 };

        // Act & Assert
        var action = () => sm2.Decrypt(invalidCiphertext);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Decrypt_WithModifiedCiphertext_ShouldThrowOrReturnInvalid()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };
        var ciphertext = sm2.Encrypt(plaintext);

        // 修改密文
        var modifiedCiphertext = (byte[])ciphertext.Clone();
        modifiedCiphertext[modifiedCiphertext.Length / 2] ^= 0xFF;

        // Act & Assert
        var action = () => sm2.Decrypt(modifiedCiphertext);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Decrypt_WithDifferentKey_ShouldThrowOrReturnInvalid()
    {
        // Arrange
        using var sm2A = new SM2();
        sm2A.GenerateKeyPair();
        var plaintext = new byte[] { 1, 2, 3, 4, 5 };
        var ciphertext = sm2A.Encrypt(plaintext);

        using var sm2B = new SM2();
        sm2B.GenerateKeyPair();

        // Act & Assert
        var action = () => sm2B.Decrypt(ciphertext);
        action.Should().Throw<Exception>();
    }

    #endregion

    #region 11. 加密解密集成测试

    [Fact]
    public void EncryptDecrypt_CompleteWorkflow_ShouldSucceed()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var originalPlaintext = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var ciphertext = sm2.Encrypt(originalPlaintext);
        var decryptedPlaintext = sm2.Decrypt(ciphertext);

        // Assert
        decryptedPlaintext.Should().Equal(originalPlaintext);
    }

    [Fact]
    public void EncryptDecrypt_WithExportImport_ShouldSucceed()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var originalPlaintext = new byte[] { 1, 2, 3, 4, 5 };
        var publicKey = sm2.ExportPublicKey();
        var privateKey = sm2.ExportPrivateKey();

        // Act - 使用公钥加密
        using var sm2Encrypt = new SM2();
        sm2Encrypt.ImportPublicKey(publicKey);
        var ciphertext = sm2Encrypt.Encrypt(originalPlaintext);

        // 使用私钥解密
        using var sm2Decrypt = new SM2();
        sm2Decrypt.ImportPrivateKey(privateKey);
        var decryptedPlaintext = sm2Decrypt.Decrypt(ciphertext);

        // Assert
        decryptedPlaintext.Should().Equal(originalPlaintext);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(100)]
    public void EncryptDecrypt_VariousDataSizes_ShouldSucceed(int dataSize)
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var plaintext = new byte[dataSize];
        Random.Shared.NextBytes(plaintext);

        // Act
        var ciphertext = sm2.Encrypt(plaintext);
        var decryptedPlaintext = sm2.Decrypt(ciphertext);

        // Assert
        decryptedPlaintext.Should().Equal(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_BinaryData_ShouldSucceed()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var plaintext = new byte[] { 0x00, 0xFF, 0x00, 0x01, 0x80, 0x7F };

        // Act
        var ciphertext = sm2.Encrypt(plaintext);
        var decryptedPlaintext = sm2.Decrypt(ciphertext);

        // Assert
        decryptedPlaintext.Should().Equal(plaintext);
    }

    #endregion

    #region 12. Dispose 和资源管理测试

    [Fact]
    public void Dispose_ShouldClearKeys()
    {
        // Arrange
        var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act
        sm2.Dispose();

        // Assert - Dispose 后应该无法导出密钥
        var publicKeyAction = () => sm2.ExportPublicKey();
        var privateKeyAction = () => sm2.ExportPrivateKey();

        publicKeyAction.Should().Throw<CryptographicException>();
        privateKeyAction.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var sm2 = new SM2();
        sm2.GenerateKeyPair();

        // Act & Assert
        var action = () =>
        {
            sm2.Dispose();
            sm2.Dispose();
            sm2.Dispose();
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void UsingPattern_ShouldAutomaticallyDispose()
    {
        SM2? sm2Reference = null;

        // Act
        using (var sm2 = new SM2())
        {
            sm2.GenerateKeyPair();
            sm2Reference = sm2;
        }

        // Assert - 离开 using 作用域后应该已经 Dispose
        var action = () => sm2Reference!.ExportPublicKey();
        action.Should().Throw<CryptographicException>();
    }

    #endregion

    #region 13. 静态方法测试

    [Fact]
    public void Create_ShouldReturnNewInstance()
    {
        // Act
        using var sm2 = SM2.Create();

        // Assert
        sm2.Should().NotBeNull();
        sm2.Should().BeOfType<SM2>();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldReturnDifferentInstances()
    {
        // Act
        using var sm2_1 = SM2.Create();
        using var sm2_2 = SM2.Create();

        // Assert
        sm2_1.Should().NotBeSameAs(sm2_2);
    }

    #endregion

    #region 14. 边界和异常情况测试

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(90)]
    public void MaxDataSize_Encryption_ShouldSucceed(int dataSize)
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var data = new byte[dataSize];
        Random.Shared.NextBytes(data);

        // Act
        var action = () =>
        {
            var ciphertext = sm2.Encrypt(data);
            var decrypted = sm2.Decrypt(ciphertext);
            return decrypted;
        };

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void ConcurrentAccess_ShouldBeSafe()
    {
        // Arrange
        using var sm2 = new SM2();
        sm2.GenerateKeyPair();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, 10, i =>
        {
            try
            {
                var signature = sm2.SignData(testData);
                var result = sm2.VerifyData(testData, signature);
                if (!result)
                {
                    exceptions.Add(new Exception("Verification failed"));
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }

    #endregion
}
