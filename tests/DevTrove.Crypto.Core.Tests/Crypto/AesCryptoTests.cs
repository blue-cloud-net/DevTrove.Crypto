using DevTrove.Crypto.Crypto;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// AesCrypto 单元测试：CBC 加解密、GCM 认证加密与 ECB 禁用约束。
/// </summary>
public class AesCryptoTests
{
    private static byte[] CbcEncrypt(byte[] key, byte[] iv, byte[] plaintext)
    {
        using var aes = new AesCrypto
        {
            Key = key,
            IV = iv,
            Mode = CipherMode.CBC,
            Padding = PaddingMode.PKCS7,
        };
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }

    private static byte[] CbcDecrypt(byte[] key, byte[] iv, byte[] ciphertext)
    {
        using var aes = new AesCrypto
        {
            Key = key,
            IV = iv,
            Mode = CipherMode.CBC,
            Padding = PaddingMode.PKCS7,
        };
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    [Fact]
    public void Cbc_EncryptDecrypt_ShouldRoundTrip()
    {
        // Arrange
        var key = RandomNumberGenerator.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(AesCrypto.BlockSizeInBytes);
        var plaintext = "AES-CBC 加解密测试"u8.ToArray();

        // Act
        var ciphertext = CbcEncrypt(key, iv, plaintext);
        var decrypted = CbcDecrypt(key, iv, ciphertext);

        // Assert
        ciphertext.Should().NotEqual(plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Theory]
    [InlineData(CipherMode.CFB)]
    [InlineData(CipherMode.OFB)]
    public void CfbOfb_EncryptDecrypt_ShouldRoundTrip(CipherMode mode)
    {
        // Arrange - CFB/OFB 以 128 位全块处理，明文填充到块大小整数倍。
        var key = RandomNumberGenerator.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(AesCrypto.BlockSizeInBytes);
        var raw = "AES-CFB/OFB 加解密测试"u8.ToArray();
        var plaintext = new byte[((raw.Length + 15) / 16) * 16];
        raw.CopyTo(plaintext, 0);

        // Act
        byte[] ciphertext;
        using (var aes = new AesCrypto { Key = key, IV = iv, Mode = mode, Padding = PaddingMode.None })
        using (var encryptor = aes.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        byte[] decrypted;
        using (var aes = new AesCrypto { Key = key, IV = iv, Mode = mode, Padding = PaddingMode.None })
        using (var decryptor = aes.CreateDecryptor())
        using (var ms = new MemoryStream(ciphertext))
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        using (var result = new MemoryStream())
        {
            cs.CopyTo(result);
            decrypted = result.ToArray();
        }

        // Assert
        ciphertext.Should().NotEqual(plaintext);
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Gcm_EncryptDecrypt_ShouldRoundTrip()
    {
        // Arrange
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = "AES-GCM 认证加密测试"u8.ToArray();

        // Act
        var ciphertext = AesCrypto.EncryptGcm(plaintext, key);
        var decrypted = AesCrypto.DecryptGcm(ciphertext, key);

        // Assert
        decrypted.Should().Equal(plaintext);
        ciphertext.Length.Should().Be(plaintext.Length + AesCrypto.GcmNonceSizeInBytes + AesCrypto.GcmTagSizeInBytes);
    }

    [Fact]
    public void Gcm_WithAad_ShouldRoundTrip()
    {
        // Arrange
        var key = RandomNumberGenerator.GetBytes(16);
        var plaintext = "With AAD"u8.ToArray();
        var aad = "header"u8.ToArray();

        // Act
        var ciphertext = AesCrypto.EncryptGcm(plaintext, key, associatedData: aad);
        var decrypted = AesCrypto.DecryptGcm(ciphertext, key, associatedData: aad);

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Gcm_TamperedCiphertext_ShouldThrow()
    {
        // Arrange
        var key = RandomNumberGenerator.GetBytes(32);
        var ciphertext = AesCrypto.EncryptGcm("secret"u8.ToArray(), key);
        ciphertext[^1] ^= 0x01; // 篡改最后一个字节（tag 部分）

        // Act & Assert
        var action = () => AesCrypto.DecryptGcm(ciphertext, key);
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Gcm_WrongNonceLength_ShouldThrow()
    {
        // Arrange
        var key = RandomNumberGenerator.GetBytes(32);
        var badNonce = new byte[8];

        // Act & Assert
        var action = () => AesCrypto.EncryptGcm("data"u8.ToArray(), key, badNonce);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Mode_SetEcb_ShouldThrow()
    {
        // Arrange
        using var aes = new AesCrypto();

        // Act & Assert
        var action = () => aes.Mode = CipherMode.ECB;
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void GenerateGcmNonce_ShouldReturn12Bytes()
    {
        // Act
        var nonce1 = AesCrypto.GenerateGcmNonce();
        var nonce2 = AesCrypto.GenerateGcmNonce();

        // Assert
        nonce1.Should().HaveCount(AesCrypto.GcmNonceSizeInBytes);
        nonce1.Should().NotEqual(nonce2);
    }
}
