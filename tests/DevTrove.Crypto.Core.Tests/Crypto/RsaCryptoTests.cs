using DevTrove.Crypto.Crypto;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// RsaCrypto 单元测试：OAEP / PKCS#1 v1.5 加解密，PSS / PKCS#1 v1.5 签名验签。
/// </summary>
public class RsaCryptoTests
{
    [Fact]
    public void EncryptDecrypt_Oaep_ShouldRoundTrip()
    {
        // Arrange
        using var rsa = new RsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        rsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var plaintext = "Hello RSA OAEP"u8.ToArray();

        // Act
        var ciphertext = rsa.Encrypt(plaintext, useOaep: true);
        var decrypted = rsa.Decrypt(ciphertext, useOaep: true);

        // Assert
        decrypted.Should().Equal(plaintext);
        ciphertext.Should().NotEqual(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_Pkcs1_ShouldRoundTrip()
    {
        // Arrange
        using var rsa = new RsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        rsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var plaintext = "Hello RSA PKCS1"u8.ToArray();

        // Act
        var ciphertext = rsa.Encrypt(plaintext, useOaep: false);
        var decrypted = rsa.Decrypt(ciphertext, useOaep: false);

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void Encrypt_WithPublicKeyOnly_ShouldSucceed()
    {
        // Arrange
        using var rsa = new RsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        rsa.ImportPublicKey(keyPair.PublicKey.ToDer());
        var plaintext = "Public only"u8.ToArray();

        // Act
        var ciphertext = rsa.Encrypt(plaintext);

        // Assert
        ciphertext.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Decrypt_WithWrongKey_ShouldThrow()
    {
        // Arrange
        using var encryptor = new RsaCrypto();
        using var other = new RsaCrypto();
        var keyPairA = AsymmetricKeyPair.GenerateRsa(2048);
        var keyPairB = AsymmetricKeyPair.GenerateRsa(2048);
        encryptor.ImportPublicKey(keyPairA.PublicKey.ToDer());
        other.ImportPrivateKey(keyPairB.PrivateKey.ToDer());
        var ciphertext = encryptor.Encrypt("secret"u8.ToArray());

        // Act & Assert
        var action = () => other.Decrypt(ciphertext);
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Encrypt_WithoutPublicKey_ShouldThrow()
    {
        // Arrange
        using var rsa = new RsaCrypto();

        // Act & Assert
        var action = () => rsa.Encrypt("data"u8.ToArray());
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WithoutPrivateKey_ShouldThrow()
    {
        // Arrange
        using var rsa = new RsaCrypto();

        // Act & Assert
        var action = () => rsa.Decrypt("data"u8.ToArray());
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void SignVerify_Pss_ShouldSucceed()
    {
        // Arrange
        using var rsa = new RsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        rsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "Sign me with PSS"u8.ToArray();

        // Act
        var signature = rsa.SignData(data, "SHA-256", usePss: true);
        var verified = rsa.VerifyData(data, signature, "SHA-256", usePss: true);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        verified.Should().BeTrue();
    }

    [Fact]
    public void SignVerify_Pkcs1_ShouldSucceed()
    {
        // Arrange
        using var rsa = new RsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        rsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "Sign me with PKCS1"u8.ToArray();

        // Act
        var signature = rsa.SignData(data, "SHA-256", usePss: false);
        var verified = rsa.VerifyData(data, signature, "SHA-256", usePss: false);

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public void Verify_TamperedData_ShouldFail()
    {
        // Arrange
        using var rsa = new RsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        rsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "Original data"u8.ToArray();
        var signature = rsa.SignData(data);

        // Act
        var tampered = "Tampered data"u8.ToArray();
        var verified = rsa.VerifyData(tampered, signature);

        // Assert
        verified.Should().BeFalse();
    }

    [Fact]
    public void Sign_WithUnsupportedHash_ShouldThrow()
    {
        // Arrange
        using var rsa = new RsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        rsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());

        // Act & Assert
        var action = () => rsa.SignData("data"u8.ToArray(), "SHA-1");
        action.Should().Throw<ArgumentException>();
    }
}
