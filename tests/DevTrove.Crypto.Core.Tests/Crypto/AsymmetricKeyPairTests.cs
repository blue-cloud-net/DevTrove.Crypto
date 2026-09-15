using DevTrove.Crypto.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// AsymmetricKeyPair 单元测试：生成、PEM/DER 往返、参数提取与素材解析对照。
/// </summary>
public class AsymmetricKeyPairTests
{
    #region RSA 生成

    [Fact]
    public void GenerateRsa_Default_ShouldCreate2048BitKeyPair()
    {
        // Arrange & Act
        var keyPair = AsymmetricKeyPair.GenerateRsa();

        // Assert
        keyPair.KeySize.Should().Be(2048);
        keyPair.Algorithm.Should().Be("RSA");
        keyPair.PrivateKey.Should().NotBeNull();
        keyPair.PublicKey.Should().NotBeNull();
    }

    [Fact]
    public void GenerateRsa_SpecificSize_ShouldMatch()
    {
        // Act
        var keyPair = AsymmetricKeyPair.GenerateRsa(3072);

        // Assert
        keyPair.KeySize.Should().Be(3072);
    }

    [Fact]
    public void GenerateRsa_PemRoundTrip_ShouldPreserveKey()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var pem = keyPair.ExportPrivateKeyPem();

        // Act
        var restored = AsymmetricPrivateKeyParameter.FromPem(pem);

        // Assert
        restored.KeySize.Should().Be(2048);
        restored.AlgorithmName.Should().Be("RSA");
    }

    [Fact]
    public void GenerateRsa_DerRoundTrip_ShouldPreserveKey()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var der = keyPair.PrivateKey.ToDer();

        // Act
        var restored = AsymmetricPrivateKeyParameter.FromDer(der);

        // Assert
        restored.KeySize.Should().Be(2048);
    }

    [Fact]
    public void GenerateRsa_PrivateKeyToPublicKey_ShouldMatch()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);

        // Act
        var derived = keyPair.PrivateKey.GetPublicKey();

        // Assert
        derived.GetBouncyCastleKey().Should().BeOfType<RsaKeyParameters>();
        derived.AlgorithmName.Should().Be("RSA");
    }

    #endregion

    #region EC 生成

    [Fact]
    public void GenerateEc_DefaultCurve_ShouldCreateKeyPair()
    {
        // Act
        var keyPair = AsymmetricKeyPair.GenerateEc();

        // Assert
        keyPair.Algorithm.Should().Be("EC");
        keyPair.PrivateKey.GetBouncyCastleKey().Should().BeOfType<ECPrivateKeyParameters>();
    }

    [Fact]
    public void GenerateEc_Secp384r1_ShouldCreateKeyPair()
    {
        // Act
        var keyPair = AsymmetricKeyPair.GenerateEc("secp384r1");

        // Assert
        keyPair.PrivateKey.GetBouncyCastleKey().Should().BeOfType<ECPrivateKeyParameters>();
        var ecKey = (ECPrivateKeyParameters)keyPair.PrivateKey.GetBouncyCastleKey();
        ecKey.Parameters.Curve.FieldSize.Should().Be(384);
    }

    [Fact]
    public void GenerateEc_UnknownCurve_ShouldThrow()
    {
        // Act & Assert
        var action = () => AsymmetricKeyPair.GenerateEc("unknown-curve");
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region SM2 / DSA 生成

    [Fact]
    public void GenerateSm2_ShouldCreateKeyPair()
    {
        // Act
        var keyPair = AsymmetricKeyPair.GenerateSm2();

        // Assert
        keyPair.PrivateKey.GetBouncyCastleKey().Should().BeOfType<ECPrivateKeyParameters>();
        var ecKey = (ECPrivateKeyParameters)keyPair.PrivateKey.GetBouncyCastleKey();
        ecKey.Parameters.Curve.FieldSize.Should().Be(256);
    }

    [Fact]
    public void GenerateDsa_ShouldCreateKeyPair()
    {
        // Act
        var keyPair = AsymmetricKeyPair.GenerateDsa(2048);

        // Assert
        keyPair.Algorithm.Should().Be("DSA");
        keyPair.PrivateKey.GetBouncyCastleKey().Should().BeOfType<DsaPrivateKeyParameters>();
    }

    #endregion

    #region 素材解析对照（tests/data/keys）

    [Fact]
    public void ParseRsa2048Pkcs1Fixture_ShouldHaveKeySize2048()
    {
        // Arrange
        var pem = File.ReadAllText(TestData.Keys("rsa-2048-pkcs1.pem"));

        // Act
        var key = AsymmetricPrivateKeyParameter.FromPem(pem);

        // Assert
        key.AlgorithmName.Should().Be("RSA");
        key.KeySize.Should().Be(2048);
        key.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void ParseRsa3072Pkcs8Fixture_ShouldHaveKeySize3072()
    {
        // Arrange
        var pem = File.ReadAllText(TestData.Keys("rsa-3072-pkcs8.pem"));

        // Act
        var key = AsymmetricPrivateKeyParameter.FromPem(pem);

        // Assert
        key.KeySize.Should().Be(3072);
    }

    [Fact]
    public void ParseRsa2048PublicFixture_ShouldBePublic()
    {
        // Arrange
        var pem = File.ReadAllText(TestData.Keys("rsa-2048-public.pem"));

        // Act
        var key = AsymmetricPublicKeyParameter.FromPem(pem);

        // Assert
        key.AlgorithmName.Should().Be("RSA");
        key.IsPublic.Should().BeTrue();
        key.KeySize.Should().Be(2048);
    }

    [Fact]
    public void ParseRsa2048Pkcs8DerFixture_ShouldHaveKeySize2048()
    {
        // Arrange
        var der = File.ReadAllBytes(TestData.Keys("rsa-2048-pkcs8.der"));

        // Act
        var key = AsymmetricPrivateKeyParameter.FromDer(der);

        // Assert
        key.AlgorithmName.Should().Be("RSA");
        key.KeySize.Should().Be(2048);
    }

    [Fact]
    public void ParseEcP256Fixture_ShouldBeEcKey()
    {
        // Arrange
        var pem = File.ReadAllText(TestData.Keys("ec-p256-pkcs8.pem"));

        // Act
        var key = AsymmetricPrivateKeyParameter.FromPem(pem);

        // Assert
        key.AlgorithmName.Should().Be("EC");
        var ecKey = key.GetBouncyCastleKey() as ECPrivateKeyParameters;
        ecKey.Should().NotBeNull();
        ecKey!.Parameters.Curve.FieldSize.Should().Be(256);
    }

    [Fact]
    public void ParseDsa2048PrivateFixture_ShouldBeDsaKey()
    {
        // Arrange
        var pem = File.ReadAllText(TestData.Keys("dsa-2048-private.pem"));

        // Act
        var key = AsymmetricPrivateKeyParameter.FromPem(pem);

        // Assert
        key.AlgorithmName.Should().Be("DSA");
        key.GetBouncyCastleKey().Should().BeOfType<DsaPrivateKeyParameters>();
    }

    [Fact]
    public void ParseSm2Pkcs8Fixture_ShouldBeSm2Key()
    {
        // Arrange
        var pem = File.ReadAllText(TestData.Keys("sm2-pkcs8.pem"));

        // Act
        var key = AsymmetricPrivateKeyParameter.FromPem(pem);

        // Assert
        var ecKey = key.GetBouncyCastleKey() as ECPrivateKeyParameters;
        ecKey.Should().NotBeNull();
        ecKey!.Parameters.Curve.FieldSize.Should().Be(256);
    }

    #endregion
}
