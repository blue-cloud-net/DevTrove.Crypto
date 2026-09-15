using DevTrove.Crypto.Crypto;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// DsaCrypto 单元测试：DSA 签名验签（DER 编码）。
/// </summary>
public class DsaCryptoTests
{
    [Fact]
    public void SignVerify_ShouldSucceed()
    {
        // Arrange
        using var dsa = new DsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateDsa(2048);
        dsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "DSA sign test"u8.ToArray();

        // Act
        var signature = dsa.SignData(data);
        var verified = dsa.VerifyData(data, signature);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        verified.Should().BeTrue();
    }

    [Fact]
    public void Verify_TamperedData_ShouldFail()
    {
        // Arrange
        using var dsa = new DsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateDsa(2048);
        dsa.ImportPrivateKey(keyPair.PrivateKey.ToDer());
        var data = "Original"u8.ToArray();
        var signature = dsa.SignData(data);

        // Act
        var verified = dsa.VerifyData("Tampered"u8.ToArray(), signature);

        // Assert
        verified.Should().BeFalse();
    }

    [Fact]
    public void Sign_WithoutPrivateKey_ShouldThrow()
    {
        // Arrange
        using var dsa = new DsaCrypto();
        var keyPair = AsymmetricKeyPair.GenerateDsa(2048);
        dsa.ImportPublicKey(keyPair.PublicKey.ToDer());

        // Act & Assert
        var action = () => dsa.SignData("data"u8.ToArray());
        action.Should().Throw<CryptographicException>();
    }
}
