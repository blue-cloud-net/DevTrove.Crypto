using DevTrove.Crypto.Crypto.Sm;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace DevTrove.Crypto.Core.Tests.Crypto.Sm;

/// <summary>
/// SM4 类的单元测试
/// </summary>
public class SM4Tests
{
    #region 1. 构造函数和基本属性测试

    [Fact]
    public void Constructor_ShouldInitializeWithCorrectKeySize()
    {
        // Arrange & Act
        using var sm4 = new SM4();

        // Assert
        sm4.KeySize.Should().Be(128, "SM4 uses 128-bit key");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithCorrectBlockSize()
    {
        // Arrange & Act
        using var sm4 = new SM4();

        // Assert
        sm4.BlockSize.Should().Be(128, "SM4 uses 128-bit block");
    }

    [Fact]
    public void Constructor_ShouldSetLegalKeySizes()
    {
        // Arrange & Act
        using var sm4 = new SM4();

        // Assert
        sm4.LegalKeySizes.Should().NotBeNull();
        sm4.LegalKeySizes.Should().HaveCount(1);
        sm4.LegalKeySizes[0].MinSize.Should().Be(128);
        sm4.LegalKeySizes[0].MaxSize.Should().Be(128);
        sm4.LegalKeySizes[0].SkipSize.Should().Be(0);
    }

    [Fact]
    public void Constructor_ShouldSetLegalBlockSizes()
    {
        // Arrange & Act
        using var sm4 = new SM4();

        // Assert
        sm4.LegalBlockSizes.Should().NotBeNull();
        sm4.LegalBlockSizes.Should().HaveCount(1);
        sm4.LegalBlockSizes[0].MinSize.Should().Be(128);
        sm4.LegalBlockSizes[0].MaxSize.Should().Be(128);
        sm4.LegalBlockSizes[0].SkipSize.Should().Be(0);
    }

    [Fact]
    public void Constructor_ShouldGenerateRandomKeyAndIV()
    {
        // Arrange & Act
        using var sm4 = new SM4();

        // Assert
        sm4.Key.Should().NotBeNull();
        sm4.Key.Length.Should().Be(16, "SM4 key should be 16 bytes");
        sm4.IV.Should().NotBeNull();
        sm4.IV.Length.Should().Be(16, "SM4 IV should be 16 bytes");
    }

    [Fact]
    public void Constructor_ShouldSetDefaultMode()
    {
        // Arrange & Act
        using var sm4 = new SM4();

        // Assert
        sm4.Mode.Should().Be(CipherMode.CBC);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultPadding()
    {
        // Arrange & Act
        using var sm4 = new SM4();

        // Assert
        sm4.Padding.Should().Be(PaddingMode.PKCS7);
    }

    #endregion

    #region 2. 静态工厂方法测试

    [Fact]
    public void Create_ShouldReturnNewInstance()
    {
        // Act
        using var sm4 = SM4.Create();

        // Assert
        sm4.Should().NotBeNull();
        sm4.Should().BeOfType<SM4>();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldReturnDifferentInstances()
    {
        // Act
        using var sm4_1 = SM4.Create();
        using var sm4_2 = SM4.Create();

        // Assert
        sm4_1.Should().NotBeSameAs(sm4_2);
    }

    #endregion

    #region 3. 密钥和 IV 生成测试

    [Fact]
    public void GenerateKey_ShouldCreateValidKey()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act
        sm4.GenerateKey();

        // Assert
        sm4.Key.Should().NotBeNull();
        sm4.Key.Length.Should().Be(16);
    }

    [Fact]
    public void GenerateKey_MultipleCalls_ShouldCreateDifferentKeys()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act
        sm4.GenerateKey();
        var key1 = sm4.Key.ToArray();
        sm4.GenerateKey();
        var key2 = sm4.Key.ToArray();

        // Assert
        key1.Should().NotEqual(key2);
    }

    [Fact]
    public void GenerateIV_ShouldCreateValidIV()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act
        sm4.GenerateIV();

        // Assert
        sm4.IV.Should().NotBeNull();
        sm4.IV.Length.Should().Be(16);
    }

    [Fact]
    public void GenerateIV_MultipleCalls_ShouldCreateDifferentIVs()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act
        sm4.GenerateIV();
        var iv1 = sm4.IV.ToArray();
        sm4.GenerateIV();
        var iv2 = sm4.IV.ToArray();

        // Assert
        iv1.Should().NotEqual(iv2);
    }

    #endregion

    #region 4. 密钥和 IV 属性测试

    [Fact]
    public void Key_SetValidKey_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4();
        var customKey = new byte[16];
        Random.Shared.NextBytes(customKey);

        // Act
        sm4.Key = customKey;

        // Assert
        sm4.Key.Should().Equal(customKey);
    }

    [Fact]
    public void Key_SetNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act & Assert
        var action = () => sm4.Key = null!;
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Key_SetInvalidLengthKey_ShouldThrowCryptographicException()
    {
        // Arrange
        using var sm4 = new SM4();
        var invalidKey = new byte[8]; // 错误的长度

        // Act & Assert
        var action = () => sm4.Key = invalidKey;
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void IV_SetValidIV_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4();
        var customIV = new byte[16];
        Random.Shared.NextBytes(customIV);

        // Act
        sm4.IV = customIV;

        // Assert
        sm4.IV.Should().Equal(customIV);
    }

    [Fact]
    public void IV_SetNullIV_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act & Assert
        var action = () => sm4.IV = null!;
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IV_SetInvalidLengthIV_ShouldThrowCryptographicException()
    {
        // Arrange
        using var sm4 = new SM4();
        var invalidIV = new byte[8]; // 错误的长度

        // Act & Assert
        var action = () => sm4.IV = invalidIV;
        action.Should().Throw<CryptographicException>();
    }

    #endregion

    #region 5. 加密模式测试

    [Theory]
    [InlineData(CipherMode.ECB)]
    [InlineData(CipherMode.CBC)]
    [InlineData(CipherMode.CFB)]
    [InlineData(CipherMode.OFB)]
    [InlineData(CipherMode.CTS)]
    public void Mode_SetSupportedMode_ShouldSucceed(CipherMode mode)
    {
        // Arrange
        using var sm4 = new SM4();

        // Act
        sm4.Mode = mode;

        // Assert
        sm4.Mode.Should().Be(mode);
    }

    [Fact]
    public void Mode_SetUnsupportedMode_ShouldThrowCryptographicException()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act & Assert
        var action = () => sm4.Mode = (CipherMode)999;
        action.Should().Throw<CryptographicException>();
    }

    #endregion

    #region 6. 填充模式测试

    [Theory]
    [InlineData(PaddingMode.None)]
    [InlineData(PaddingMode.PKCS7)]
    [InlineData(PaddingMode.Zeros)]
    [InlineData(PaddingMode.ANSIX923)]
    [InlineData(PaddingMode.ISO10126)]
    public void Padding_SetSupportedPadding_ShouldSucceed(PaddingMode padding)
    {
        // Arrange
        using var sm4 = new SM4();

        // Act
        sm4.Padding = padding;

        // Assert
        sm4.Padding.Should().Be(padding);
    }

    [Fact]
    public void Padding_SetUnsupportedPadding_ShouldThrowCryptographicException()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act & Assert
        var action = () => sm4.Padding = (PaddingMode)999;
        action.Should().Throw<CryptographicException>();
    }

    #endregion

    #region 7. 基本加密解密测试 (CBC)

    [Fact]
    public void EncryptDecrypt_CBC_WithValidData_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = Encoding.UTF8.GetBytes("Hello SM4!");

        // Act - 加密
        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // Act - 解密
        byte[] decrypted;
        using (var decryptor = sm4.CreateDecryptor())
        using (var ms = new MemoryStream(ciphertext))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var result = new MemoryStream())
        {
            cs.CopyTo(result);
            decrypted = result.ToArray();
        }

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Encrypt_CBC_WithEmptyData_ShouldReturnPaddingOnly()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };
        var plaintext = Array.Empty<byte>();

        // Act
        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // Assert
        ciphertext.Should().NotBeEmpty("PKCS7 padding adds at least one block");
        ciphertext.Length.Should().Be(16, "Should be one block with padding");
    }

    [Fact]
    public void EncryptDecrypt_CBC_WithLargeData_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = new byte[10000];
        Random.Shared.NextBytes(plaintext);

        // Act - 加密
        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // Act - 解密
        byte[] decrypted;
        using (var decryptor = sm4.CreateDecryptor())
        using (var ms = new MemoryStream(ciphertext))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var result = new MemoryStream())
        {
            cs.CopyTo(result);
            decrypted = result.ToArray();
        }

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    #endregion

    #region 8. ECB 模式测试

    [Fact]
    public void EncryptDecrypt_ECB_WithValidData_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.ECB };
        var plaintext = Encoding.UTF8.GetBytes("Hello SM4!");

        // Act - 加密
        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // Act - 解密
        byte[] decrypted;
        using (var decryptor = sm4.CreateDecryptor())
        using (var ms = new MemoryStream(ciphertext))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var result = new MemoryStream())
        {
            cs.CopyTo(result);
            decrypted = result.ToArray();
        }

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void ECB_SamePlaintext_ShouldProduceSameCiphertext()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.ECB, Padding = PaddingMode.None };
        var plaintext = new byte[16]; // 正好一个块
        Array.Fill<byte>(plaintext, 0x42);

        // Act - 加密两次
        byte[] ciphertext1, ciphertext2;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext1 = ms.ToArray();
        }

        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext2 = ms.ToArray();
        }

        // Assert
        ciphertext1.Should().Equal(ciphertext2, "ECB mode should produce same ciphertext for same plaintext and key");
    }

    #endregion

    #region 9. 不同填充模式测试

    [Fact]
    public void EncryptDecrypt_PKCS7Padding_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };
        var plaintext = Encoding.UTF8.GetBytes("Test");

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_ZerosPadding_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
        var plaintext = Encoding.UTF8.GetBytes("Test");

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);

        // Zeros 填充可能会在末尾包含额外的零
        decrypted.Take(plaintext.Length).Should().Equal(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_ANSIX923Padding_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC, Padding = PaddingMode.ANSIX923 };
        var plaintext = Encoding.UTF8.GetBytes("Test");

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_ISO10126Padding_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC, Padding = PaddingMode.ISO10126 };
        var plaintext = Encoding.UTF8.GetBytes("Test");

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Encrypt_NoPadding_WithNonBlockAlignedData_ShouldThrow()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC, Padding = PaddingMode.None };
        var plaintext = Encoding.UTF8.GetBytes("Test"); // 不是块的整数倍

        // Act & Assert
        var action = () =>
        {
            using var encryptor = sm4.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
        };

        action.Should().Throw<CryptographicException>();
    }

    #endregion

    #region 10. 密钥和 IV 验证测试

    [Fact]
    public void CreateEncryptor_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm4 = new SM4();

        // Act & Assert
        var action = () => sm4.CreateEncryptor(null!, sm4.IV);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateEncryptor_WithInvalidKeyLength_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm4 = new SM4();
        var invalidKey = new byte[8];

        // Act & Assert
        var action = () => sm4.CreateEncryptor(invalidKey, sm4.IV);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid size*");
    }

    [Fact]
    public void CreateEncryptor_CBC_WithNullIV_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };

        // Act & Assert
        var action = () => sm4.CreateEncryptor(sm4.Key, null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateEncryptor_ECB_WithNullIV_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.ECB };

        // Act
        var encryptor = sm4.CreateEncryptor(sm4.Key, null);

        // Assert
        encryptor.Should().NotBeNull();
    }

    [Fact]
    public void CreateEncryptor_WithInvalidIVLength_ShouldThrowArgumentException()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var invalidIV = new byte[8];

        // Act & Assert
        var action = () => sm4.CreateEncryptor(sm4.Key, invalidIV);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*does not match the block size*");
    }

    #endregion

    #region 11. 密钥重用测试

    [Fact]
    public void EncryptDecrypt_ReuseKey_ShouldProduceDifferentCiphertexts()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = Encoding.UTF8.GetBytes("Hello SM4!");
        var key = sm4.Key.ToArray();

        // Act - 第一次加密
        sm4.GenerateIV();
        var (ciphertext1, _) = EncryptDecryptHelper(sm4, plaintext);

        // Act - 第二次加密（不同的 IV）
        sm4.GenerateIV();
        var (ciphertext2, _) = EncryptDecryptHelper(sm4, plaintext);

        // Assert
        ciphertext1.Should().NotEqual(ciphertext2, "Different IV should produce different ciphertext");
    }

    [Fact]
    public void EncryptDecrypt_WithSameKeyAndIV_ShouldBeReversible()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = Encoding.UTF8.GetBytes("Test data");
        var key = sm4.Key.ToArray();
        var iv = sm4.IV.ToArray();

        // Act - 加密
        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor(key, iv))
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // Act - 解密
        byte[] decrypted;
        using (var decryptor = sm4.CreateDecryptor(key, iv))
        using (var ms = new MemoryStream(ciphertext))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var result = new MemoryStream())
        {
            cs.CopyTo(result);
            decrypted = result.ToArray();
        }

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    #endregion

    #region 12. 标准测试向量验证

    [Fact]
    public void Encrypt_StandardTestVector_ShouldMatchExpected()
    {
        // Arrange - GB/T 32907-2016 标准测试向量
        using var sm4 = new SM4 { Mode = CipherMode.ECB, Padding = PaddingMode.None };

        var key = Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210");
        var plaintext = Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210");
        var expectedCiphertext = Convert.FromHexString("681EDF34D206965E86B3E94F536E4246");

        sm4.Key = key;

        // Act
        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // Assert
        ciphertext.Should().Equal(expectedCiphertext);
    }

    [Fact]
    public void Decrypt_StandardTestVector_ShouldMatchExpected()
    {
        // Arrange - GB/T 32907-2016 标准测试向量
        using var sm4 = new SM4 { Mode = CipherMode.ECB, Padding = PaddingMode.None };

        var key = Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210");
        var ciphertext = Convert.FromHexString("681EDF34D206965E86B3E94F536E4246");
        var expectedPlaintext = Convert.FromHexString("0123456789ABCDEFFEDCBA9876543210");

        sm4.Key = key;

        // Act
        byte[] plaintext;
        using (var decryptor = sm4.CreateDecryptor())
        using (var ms = new MemoryStream(ciphertext))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var result = new MemoryStream())
        {
            cs.CopyTo(result);
            plaintext = result.ToArray();
        }

        // Assert
        plaintext.Should().Equal(expectedPlaintext);
    }

    #endregion

    #region 13. Dispose 和资源管理测试

    [Fact]
    public void Dispose_ShouldClearSensitiveData()
    {
        // Arrange
        var sm4 = new SM4();
        var originalKey = sm4.Key.ToArray();

        // Act
        sm4.Dispose();

        // Assert - 密钥应该被清除
        originalKey.Should().NotEqual(sm4.Key);
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var sm4 = new SM4();

        // Act & Assert
        var action = () =>
        {
            sm4.Dispose();
            sm4.Dispose();
            sm4.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void UsingPattern_ShouldAutomaticallyDispose()
    {
        byte[]? ciphertext = null;

        using (var sm4 = new SM4())
        {
            var plaintext = Encoding.UTF8.GetBytes("test");
            using var encryptor = sm4.CreateEncryptor();
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        ciphertext.Should().NotBeNull();
        ciphertext!.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region 14. 边界和异常情况测试

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(100)]
    public void EncryptDecrypt_VariousDataSizes_ShouldSucceed(int dataSize)
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = new byte[dataSize];
        Random.Shared.NextBytes(plaintext);

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_BinaryData_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = new byte[100];
        for (var i = 0; i < plaintext.Length; i++)
        {
            plaintext[i] = (byte)i;
        }

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_AllZeros_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = new byte[32];

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_AllOnes_ShouldSucceed()
    {
        // Arrange
        using var sm4 = new SM4 { Mode = CipherMode.CBC };
        var plaintext = Enumerable.Repeat((byte)0xFF, 32).ToArray();

        // Act & Assert
        var (ciphertext, decrypted) = EncryptDecryptHelper(sm4, plaintext);
        decrypted.Should().Equal(plaintext);
    }

    #endregion

    #region 15. 并发测试

    [Fact]
    public async Task ConcurrentEncryption_WithDifferentInstances_ShouldBeSafe()
    {
        // Arrange
        var plaintext = Encoding.UTF8.GetBytes("concurrent test");
        var key = new byte[16];
        var iv = new byte[16];
        Random.Shared.NextBytes(key);
        Random.Shared.NextBytes(iv);

        var tasks = new List<Task<byte[]>>();

        // Act
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                using var sm4 = new SM4 { Mode = CipherMode.CBC, Key = key, IV = iv };
                using var encryptor = sm4.CreateEncryptor();
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                cs.Write(plaintext, 0, plaintext.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(ciphertext =>
        {
            ciphertext.Should().NotBeNull();
            ciphertext.Length.Should().BeGreaterThan(0);
        });

        // 使用相同的密钥和 IV，所有结果应该相同
        for (var i = 1; i < results.Length; i++)
        {
            results[i].Should().Equal(results[0]);
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 加密解密辅助方法
    /// </summary>
    private static (byte[] ciphertext, byte[] decrypted) EncryptDecryptHelper(SM4 sm4, byte[] plaintext)
    {
        // 加密
        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // 解密
        byte[] decrypted;
        using (var decryptor = sm4.CreateDecryptor())
        using (var ms = new MemoryStream(ciphertext))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var result = new MemoryStream())
        {
            cs.CopyTo(result);
            decrypted = result.ToArray();
        }

        return (ciphertext, decrypted);
    }

    #endregion
}
