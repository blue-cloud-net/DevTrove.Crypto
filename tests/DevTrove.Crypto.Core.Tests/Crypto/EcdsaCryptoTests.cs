using DevTrove.Crypto.Crypto;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// EcdsaCrypto 单元测试：ECDSA 签名验签（DER 编码）与 ECDH 共享密钥计算。
/// </summary>
public class EcdsaCryptoTests
{
    [Fact]
    public void SignVerify_ShouldSucceed()
    {
        // Arrange
        using var ecdsa = new EcdsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateEc("secp256r1");
        ecdsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "ECDSA sign test"u8.ToArray();

        // Act
        var signature = ecdsa.SignData(data);
        var verified = ecdsa.VerifyData(data, signature);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        verified.Should().BeTrue();
    }

    [Fact]
    public void SignVerify_WithDifferentHash_ShouldSucceed()
    {
        // Arrange
        using var ecdsa = new EcdsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateEc("secp256r1");
        ecdsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "SHA-384 sign"u8.ToArray();

        // Act
        var signature = ecdsa.SignData(data, "SHA-384");
        var verified = ecdsa.VerifyData(data, signature, "SHA-384");

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public void Verify_TamperedData_ShouldFail()
    {
        // Arrange
        using var ecdsa = new EcdsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateEc("secp256r1");
        ecdsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "Original"u8.ToArray();
        var signature = ecdsa.SignData(data);

        // Act
        var verified = ecdsa.VerifyData("Tampered"u8.ToArray(), signature);

        // Assert
        verified.Should().BeFalse();
    }

    [Fact]
    public void Sign_WithoutPrivateKey_ShouldThrow()
    {
        // Arrange
        using var ecdsa = new EcdsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateEc("secp256r1");
        ecdsa.ImportPublicKey(keyPair.PublicKey.ToDer());

        // Act & Assert
        var action = () => ecdsa.SignData("data"u8.ToArray());
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ComputeSharedSecret_SameCurve_ShouldBeEqualBothDirections()
    {
        // Arrange
        var alice = AsymmetricKeyPair.GenerateEc("secp256r1");
        var bob = AsymmetricKeyPair.GenerateEc("secp256r1");

        // Act
        var secretA = EcdsaCrypto.ComputeSharedSecret(alice.PrivateKey, bob.PublicKey);
        var secretB = EcdsaCrypto.ComputeSharedSecret(bob.PrivateKey, alice.PublicKey);

        // Assert
        secretA.Should().NotBeEmpty();
        secretA.Should().Equal(secretB);
        secretA.Length.Should().Be(32); // P-256 字段宽度
    }

    [Fact]
    public void ComputeSharedSecret_DifferentCurves_ShouldThrow()
    {
        // Arrange
        var alice = AsymmetricKeyPair.GenerateEc("secp256r1");
        var bob = AsymmetricKeyPair.GenerateEc("secp384r1");

        // Act & Assert
        var action = () => EcdsaCrypto.ComputeSharedSecret(alice.PrivateKey, bob.PublicKey);
        action.Should().Throw<CryptographicException>();
    }
}
