using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.X509.Enums;
using DevTrove.Crypto.X509.Models;

namespace DevTrove.Crypto.Core.Tests.X509;

/// <summary>
/// CertificateSigningRequest 单元测试：生成含扩展、解析与验签、素材解析对照。
/// </summary>
public class CertificateSigningRequestTests
{
    [Fact]
    public void Generate_ShouldCreateValidCsr()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);

        // Act
        var csr = CertificateSigningRequest.Generate(
            "/C=CN/O=Test Corp/CN=gen.example.com",
            keyPair,
            "SHA256WITHRSA");

        // Assert
        csr.Subject.Should().Contain("CN=gen.example.com");
        csr.SignatureAlgorithmName.Should().BeEquivalentTo("SHA256WITHRSA");
        csr.Verify().Should().BeTrue();
    }

    [Fact]
    public void Generate_WithExtensions_ShouldPreserveExtensions()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var options = new X509ExtensionOptions
        {
            KeyUsages = KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment,
            ExtendedKeyUsages = ExtendedKeyUsage.ServerAuthentication,
            SubjectAlternativeNames =
            [
                new GeneralName(GeneralNameType.DnsName, "gen.example.com"),
                new GeneralName(GeneralNameType.IPAddress, "10.1.2.3"),
            ],
        };

        // Act
        var csr = CertificateSigningRequest.Generate(
            "/CN=gen.example.com",
            keyPair,
            "SHA256WITHRSA",
            options);

        // Assert
        csr.Verify().Should().BeTrue();
        csr.KeyUsages.Should().NotBeNull();
        csr.KeyUsages!.Value.Should().HaveFlag(KeyUsage.DigitalSignature);
        csr.KeyUsages!.Value.Should().HaveFlag(KeyUsage.KeyEncipherment);
        csr.ExtendedKeyUsages.Should().NotBeNull();
        csr.ExtendedKeyUsages!.Value.Should().HaveFlag(ExtendedKeyUsage.ServerAuthentication);
        csr.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "gen.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.IPAddress && n.Value == "10.1.2.3");
    }

    [Fact]
    public void ToPem_FromPem_ShouldRoundTrip()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var csr = CertificateSigningRequest.Generate("/CN=roundtrip.example.com", keyPair);

        // Act
        var pem = csr.ToPem();
        var restored = CertificateSigningRequest.FromPem(pem);

        // Assert
        restored.Subject.Should().Be(csr.Subject);
        restored.Verify().Should().BeTrue();
    }

    #region 素材解析对照（tests/data/csrs）

    [Fact]
    public void ParseRsaBasicFixture_ShouldMatchGenerationParams()
    {
        // Arrange
        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(TestData.Csrs("rsa-2048-basic.csr")));

        // Assert
        csr.Subject.Should().Contain("CN=test.example.com");
        csr.SignatureAlgorithmName.Should().BeEquivalentTo("SHA256WITHRSA");
        csr.Verify().Should().BeTrue();
        csr.SubjectAlternativeNames.Should().BeNull();
    }

    [Fact]
    public void ParseRsaSanFixture_ShouldMatchGenerationParams()
    {
        // Arrange
        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(TestData.Csrs("rsa-2048-san.csr")));

        // Assert
        csr.Subject.Should().Contain("CN=multi.example.com");
        csr.Verify().Should().BeTrue();
        csr.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "multi.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "www.multi.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "api.multi.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.IPAddress && n.Value == "192.168.1.100");
    }

    [Fact]
    public void ParseEcP256Fixture_ShouldBeEcKey()
    {
        // Arrange
        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(TestData.Csrs("ec-p256-basic.csr")));

        // Assert
        csr.Subject.Should().Contain("CN=ec-test.example.com");
        csr.SignatureAlgorithmName.Should().BeEquivalentTo("SHA256WITHECDSA");
        csr.Verify().Should().BeTrue();
    }

    [Fact]
    public void ParseRsaDerFixture_ShouldSucceed()
    {
        // Arrange
        var der = File.ReadAllBytes(TestData.Csrs("rsa-2048-basic.der"));

        // Act
        var csr = CertificateSigningRequest.FromDer(der);

        // Assert
        csr.Subject.Should().Contain("CN=der-test.example.com");
        csr.Verify().Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPublicKey_ShouldFail()
    {
        // Arrange
        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(TestData.Csrs("rsa-2048-basic.csr")));
        var wrongKey = AsymmetricKeyPair.GenerateRsa(2048).PublicKey;

        // Act
        var verified = csr.Verify(wrongKey);

        // Assert
        verified.Should().BeFalse();
    }

    #endregion
}
