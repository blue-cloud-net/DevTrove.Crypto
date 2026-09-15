using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.X509.Enums;
using DevTrove.Crypto.X509.Models;
using DevTrove.Crypto.X509.Utils;

namespace DevTrove.Crypto.Core.Tests.X509;

/// <summary>
/// Certificate 单元测试：素材解析断言、含扩展自签名、SignCsr、SignPublicKey、PFX 往返。
/// </summary>
public class CertificateTests
{
    #region 素材解析断言（tests/data/certs）

    [Fact]
    public void ParseRsaSelfSignedExt_ShouldMatchGenerationParams()
    {
        // Arrange
        var cert = Certificate.FromPem(File.ReadAllText(TestData.Certs("rsa-2048-selfsigned-ext.pem")));

        // Assert - 主体/颁发者/序列号
        cert.Subject.Should().Contain("CN=test.example.com");
        cert.Issuer.Should().Be(cert.Subject, "自签名证书颁发者应等于主体");
        cert.SerialNumber.Should().Be("1001");
        cert.SignatureAlgorithmName.Should().BeEquivalentTo("SHA256WITHRSA");

        // Assert - 有效期跨度（365 天）
        cert.ValidityPeriod.Should().BeCloseTo(TimeSpan.FromDays(365), TimeSpan.FromHours(1));

        // Assert - KeyUsage / EKU
        cert.KeyUsages.Should().HaveFlag(KeyUsage.DigitalSignature);
        cert.KeyUsages.Should().HaveFlag(KeyUsage.KeyEncipherment);
        cert.ExtendedKeyUsages.Should().HaveFlag(ExtendedKeyUsage.ServerAuthentication);
        cert.ExtendedKeyUsages.Should().HaveFlag(ExtendedKeyUsage.ClientAuthentication);

        // Assert - SAN
        cert.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "test.example.com");
        cert.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "www.test.example.com");
        cert.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.IPAddress && n.Value == "192.168.1.100");

        // Assert - SKI（自签名：openssl req -x509 不生成 AKI keyid）
        cert.SubjectKeyIdentifier.Should().NotBeNullOrEmpty();

        // Assert - CRL 分发点
        cert.CrlDistributionPointUrls.Should().Contain("http://crl.example.com/test.crl");
    }

    [Fact]
    public void ParseEcSelfSignedExt_ShouldMatchGenerationParams()
    {
        // Arrange
        var cert = Certificate.FromPem(File.ReadAllText(TestData.Certs("ec-p256-selfsigned-ext.pem")));

        // Assert
        cert.Subject.Should().Contain("CN=ec-test.example.com");
        cert.SerialNumber.Should().Be("2001");
        cert.KeyUsages.Should().HaveFlag(KeyUsage.DigitalSignature);
        cert.ExtendedKeyUsages.Should().HaveFlag(ExtendedKeyUsage.ServerAuthentication);
        cert.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "ec-test.example.com");
        cert.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "api.ec-test.example.com");
        cert.SubjectKeyIdentifier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ParseRsaMinimal_ShouldHaveNoExtensions()
    {
        // Arrange
        var cert = Certificate.FromPem(File.ReadAllText(TestData.Certs("rsa-2048-minimal.pem")));

        // Assert
        cert.Subject.Should().Contain("CN=minimal.example.com");
        cert.SerialNumber.Should().Be("3001");
        cert.KeyUsages.Should().Be(KeyUsage.None);
        cert.ExtendedKeyUsages.Should().Be(ExtendedKeyUsage.None);
        cert.SubjectAlternativeNames.Should().BeNull();
        cert.SubjectKeyIdentifier.Should().BeNull();
        cert.AuthorityKeyIdentifier.Should().BeNull();
    }

    [Fact]
    public void ParseCaAndLeaf_ShouldBuildChain()
    {
        // Arrange
        var ca = Certificate.FromPem(File.ReadAllText(TestData.Certs("ca.crt")));
        var leaf = Certificate.FromPem(File.ReadAllText(TestData.Certs("leaf.crt")));

        // Assert - CA
        ca.Subject.Should().Contain("CN=Test Root CA");
        ca.SerialNumber.Should().Be("A001");
        ca.IsCertificateAuthority.Should().BeTrue();
        ca.PathLengthConstraint.Should().Be(1);
        ca.KeyUsages.Should().HaveFlag(KeyUsage.KeyCertSign);
        ca.KeyUsages.Should().HaveFlag(KeyUsage.CrlSign);

        // Assert - 叶子
        leaf.Subject.Should().Contain("CN=leaf.example.com");
        leaf.SerialNumber.Should().Be("B001");
        leaf.Issuer.Should().Be(ca.Subject);
        leaf.IsCertificateAuthority.Should().BeFalse();
        leaf.KeyUsages.Should().HaveFlag(KeyUsage.DigitalSignature);
        leaf.ExtendedKeyUsages.Should().HaveFlag(ExtendedKeyUsage.ServerAuthentication);
        leaf.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "leaf.example.com");

        // Assert - 签名链
        leaf.IsSignatureVerify(ca).Should().BeTrue();
        ca.IsSignatureVerify(ca).Should().BeTrue("CA 自签名应能自验");
    }

    [Fact]
    public void ParseSm2SelfSignedExt_ShouldMatchGenerationParams()
    {
        // Arrange
        var cert = Certificate.FromPem(File.ReadAllText(TestData.Certs("sm2-selfsigned-ext.pem")));

        // Assert
        cert.Subject.Should().Contain("CN=sm2-test.example.cn");
        cert.SerialNumber.Should().Be("4001");
        cert.KeyUsages.Should().HaveFlag(KeyUsage.DigitalSignature);
        cert.ExtendedKeyUsages.Should().HaveFlag(ExtendedKeyUsage.ServerAuthentication);
        cert.SubjectAlternativeNames.Should().Contain(n => n.Type == GeneralNameType.DnsName && n.Value == "sm2-test.example.cn");
    }

    [Fact]
    public void ComputeFingerprint_ShouldReturnSha256Hex()
    {
        // Arrange
        var cert = Certificate.FromPem(File.ReadAllText(TestData.Certs("rsa-2048-selfsigned-ext.pem")));

        // Act
        var sha256 = cert.ComputeFingerprint("SHA-256");

        // Assert
        sha256.Should().MatchRegex("^[0-9A-F]{64}$");
    }

    #endregion

    #region 含扩展自签名生成

    [Fact]
    public void GenerateSelfSigned_WithExtensions_ShouldIncludeAllExtensions()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var options = new X509ExtensionOptions
        {
            BasicConstraints = new BasicConstraintsOptions { IsCa = true, PathLengthConstraint = 1 },
            KeyUsages = KeyUsage.DigitalSignature | KeyUsage.KeyCertSign | KeyUsage.CrlSign,
            ExtendedKeyUsages = ExtendedKeyUsage.ServerAuthentication,
            SubjectAlternativeNames =
            [
                new GeneralName(GeneralNameType.DnsName, "gen.example.com"),
                new GeneralName(GeneralNameType.IPAddress, "10.0.0.1"),
            ],
            IncludeSubjectKeyIdentifier = true,
            IncludeAuthorityKeyIdentifier = true,
            CrlDistributionPointUrls = ["http://crl.example.com/gen.crl"],
        };

        // Act
        var cert = Certificate.GenerateSelfSigned(
            "/C=CN/O=Gen Corp/CN=gen.example.com",
            keyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(364),
            "5A5A",
            "SHA256WITHRSA",
            options);

        // Assert
        cert.SerialNumber.Should().Be("5A5A");
        cert.IsCertificateAuthority.Should().BeTrue();
        cert.PathLengthConstraint.Should().Be(1);
        cert.KeyUsages.Should().HaveFlag(KeyUsage.KeyCertSign);
        cert.ExtendedKeyUsages.Should().HaveFlag(ExtendedKeyUsage.ServerAuthentication);
        cert.SubjectAlternativeNames.Should().Contain(n => n.Value == "gen.example.com");
        cert.SubjectKeyIdentifier.Should().NotBeNullOrEmpty();
        cert.AuthorityKeyIdentifier.Should().Be(cert.SubjectKeyIdentifier);
        cert.CrlDistributionPointUrls.Should().Contain("http://crl.example.com/gen.crl");
        cert.IsSignatureVerify(cert).Should().BeTrue();
    }

    [Fact]
    public void GenerateSelfSigned_WithoutExtensions_ShouldAddDefaultCaBasicConstraints()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);

        // Act（不传 extensions，保持旧行为）
        var cert = Certificate.GenerateSelfSigned(
            "/CN=legacy-ca",
            keyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(364),
            "0001");

        // Assert
        cert.IsCertificateAuthority.Should().BeTrue();
    }

    #endregion

    #region SignCsr / SignPublicKey

    [Fact]
    public void SignCsr_ShouldIssueCertificateWithExtensions()
    {
        // Arrange
        var caKeyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var caCert = Certificate.GenerateSelfSigned(
            "/C=CN/O=Test CA/CN=Test CA",
            caKeyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(3650),
            "1001",
            "SHA256WITHRSA",
            new X509ExtensionOptions
            {
                BasicConstraints = new BasicConstraintsOptions { IsCa = true },
                IncludeSubjectKeyIdentifier = true,
            });

        var csrKeyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var csr = CertificateSigningRequest.Generate(
            "/C=CN/O=Client Corp/CN=client.example.com",
            csrKeyPair,
            "SHA256WITHRSA",
            new X509ExtensionOptions
            {
                KeyUsages = KeyUsage.DigitalSignature,
                ExtendedKeyUsages = ExtendedKeyUsage.ClientAuthentication,
                SubjectAlternativeNames = [new GeneralName(GeneralNameType.DnsName, "client.example.com")],
            });

        var issuedExtensions = new X509ExtensionOptions
        {
            KeyUsages = KeyUsage.DigitalSignature,
            ExtendedKeyUsages = ExtendedKeyUsage.ClientAuthentication,
            SubjectAlternativeNames = [new GeneralName(GeneralNameType.DnsName, "client.example.com")],
            IncludeSubjectKeyIdentifier = true,
            IncludeAuthorityKeyIdentifier = true,
        };

        // Act
        var issued = Certificate.SignCsr(
            csr,
            caCert,
            caKeyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(364),
            "2001",
            "SHA256WITHRSA",
            issuedExtensions);

        // Assert
        issued.Subject.Should().Contain("CN=client.example.com");
        issued.Issuer.Should().Be(caCert.Subject);
        issued.SerialNumber.Should().Be("2001");
        issued.KeyUsages.Should().HaveFlag(KeyUsage.DigitalSignature);
        issued.ExtendedKeyUsages.Should().HaveFlag(ExtendedKeyUsage.ClientAuthentication);
        issued.SubjectAlternativeNames.Should().Contain(n => n.Value == "client.example.com");
        issued.SubjectKeyIdentifier.Should().NotBeNullOrEmpty();
        issued.AuthorityKeyIdentifier.Should().NotBeNullOrEmpty();
        issued.IsSignatureVerify(caCert).Should().BeTrue();
    }

    [Fact]
    public void SignPublicKey_ShouldIssueCertificate()
    {
        // Arrange
        var caKeyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var caCert = Certificate.GenerateSelfSigned(
            "/CN=Test CA",
            caKeyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(3650),
            "1001");

        var subjectKeyPair = AsymmetricKeyPair.GenerateRsa(2048);

        // Act
        var issued = Certificate.SignPublicKey(
            subjectKeyPair.PublicKey,
            "/CN=direct.example.com",
            caCert,
            caKeyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(364),
            "3001");

        // Assert
        issued.Subject.Should().Contain("CN=direct.example.com");
        issued.Issuer.Should().Be(caCert.Subject);
        issued.SerialNumber.Should().Be("3001");
        issued.IsSignatureVerify(caCert).Should().BeTrue();
    }

    #endregion

    #region PFX 往返与素材解析

    [Fact]
    public void PfxUtils_ToPfxFromPfx_ShouldRoundTrip()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var cert = Certificate.GenerateSelfSigned(
            "/CN=pfx-test.example.com",
            keyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(364),
            "ABCD");

        // Act
        var pfx = PfxUtils.ToPfx(cert, "test1234", keyPair.PrivateKey);
        var result = PfxUtils.FromPfx(pfx, "test1234");

        // Assert
        result.Certificate.Subject.Should().Contain("CN=pfx-test.example.com");
        result.PrivateKey.Should().NotBeNull();
        result.PrivateKey!.GetBouncyCastleKey().IsPrivate.Should().BeTrue();
        result.FriendlyName.Should().Be("pfx-test.example.com");
    }

    [Fact]
    public void PfxUtils_FromPfx_WrongPassword_ShouldThrow()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var cert = Certificate.GenerateSelfSigned(
            "/CN=pfx-test",
            keyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(364),
            "ABCD");
        var pfx = PfxUtils.ToPfx(cert, "test1234", keyPair.PrivateKey);

        // Act & Assert
        var action = () => PfxUtils.FromPfx(pfx, "wrong-password");
        action.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void PfxUtils_ToPfx_WithEmptyPassword_ShouldThrow()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var cert = Certificate.GenerateSelfSigned(
            "/CN=pfx-test",
            keyPair.PrivateKey,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(364),
            "ABCD");

        // Act & Assert
        var action = () => PfxUtils.ToPfx(cert, string.Empty, keyPair.PrivateKey);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseKeyAndCertPfxFixture_ShouldContainLeafAndPrivateKey()
    {
        // Arrange
        var pfx = File.ReadAllBytes(TestData.Pfx("key-and-cert.pfx"));

        // Act
        var result = PfxUtils.FromPfx(pfx, "test1234");

        // Assert
        result.Certificate.Subject.Should().Contain("CN=test.example.com");
        result.PrivateKey.Should().NotBeNull();
        result.PrivateKey!.KeySize.Should().Be(2048);
    }

    [Fact]
    public void ParseKeyCertChainPfxFixture_ShouldContainChain()
    {
        // Arrange
        var pfx = File.ReadAllBytes(TestData.Pfx("key-cert-chain.pfx"));

        // Act
        var result = PfxUtils.FromPfx(pfx, "test1234");

        // Assert
        result.Certificate.Subject.Should().Contain("CN=leaf.example.com");
        result.PrivateKey.Should().NotBeNull();
        result.PrivateKey!.KeySize.Should().Be(3072);
        result.Chain.Should().HaveCount(1);
        result.Chain[0].Subject.Should().Contain("CN=Test Root CA");
    }

    #endregion
}
