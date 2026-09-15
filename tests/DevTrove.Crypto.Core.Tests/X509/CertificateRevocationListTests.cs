using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.X509.Enums;

namespace DevTrove.Crypto.Core.Tests.X509;

/// <summary>
/// CertificateRevocationList 单元测试：素材解析、IsRevoked、生成往返。
/// </summary>
public class CertificateRevocationListTests
{
    #region 素材解析断言（tests/data/crls）

    [Fact]
    public void ParseCrlFixture_ShouldMatchGenerationParams()
    {
        // Arrange
        var crl = CertificateRevocationList.FromPem(File.ReadAllText(TestData.Crls("test.crl")));

        // Assert
        crl.Issuer.Should().Contain("CN=Test CRL CA");
        crl.SignatureAlgorithmName.Should().BeEquivalentTo("SHA256WITHRSA");
        crl.RevokedCertificatesCount.Should().Be(2);

        var revoked = crl.RevokedCertificates!.ToList();
        revoked.Should().HaveCount(2);

        var serial1111 = revoked.First(r => r.SerialNumber == "1111");
        serial1111.RevocationReason.Should().Be(CertificateRevocationReason.KeyCompromise);

        var serial2222 = revoked.First(r => r.SerialNumber == "2222");
        serial2222.RevocationReason.Should().Be(CertificateRevocationReason.Superseded);
    }

    [Fact]
    public void IsRevoked_ShouldReturnExpected()
    {
        // Arrange
        var crl = CertificateRevocationList.FromPem(File.ReadAllText(TestData.Crls("test.crl")));

        // Act & Assert
        crl.IsRevoked("1111").Should().BeTrue();
        crl.IsRevoked("2222").Should().BeTrue();
        crl.IsRevoked("9999").Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WithLeadingZeroSerial_ShouldMatch()
    {
        // Arrange - 生成包含前导零序列号（0x0F0F）的 CRL
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var thisUpdate = DateTime.UtcNow;
        var crl = CertificateRevocationList.Generate(
            "/C=CN/O=Test Org/CN=Test CA",
            keyPair.PrivateKey,
            [("0F0F", thisUpdate, CertificateRevocationReason.KeyCompromise)],
            thisUpdate,
            thisUpdate.AddDays(30),
            "SHA256WITHRSA");

        // Act & Assert - 大小写与前导零均不影响匹配
        crl.IsRevoked("0F0F").Should().BeTrue();
        crl.IsRevoked("0f0f").Should().BeTrue();
        crl.IsRevoked("F0F").Should().BeTrue();
        crl.IsRevoked("1F0F").Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldBeFalseForFreshCrl()
    {
        // Arrange
        var crl = CertificateRevocationList.FromPem(File.ReadAllText(TestData.Crls("test.crl")));

        // Assert
        crl.IsExpired().Should().BeFalse();
        crl.NextUpdate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void ToPem_FromPem_ShouldRoundTrip()
    {
        // Arrange
        var crl = CertificateRevocationList.FromPem(File.ReadAllText(TestData.Crls("test.crl")));

        // Act
        var pem = crl.ToPem();
        var restored = CertificateRevocationList.FromPem(pem);

        // Assert
        restored.Issuer.Should().Be(crl.Issuer);
        restored.RevokedCertificatesCount.Should().Be(crl.RevokedCertificatesCount);
    }

    #endregion

    #region 生成往返

    [Fact]
    public void Generate_ShouldCreateCrlWithEntries()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var thisUpdate = DateTime.UtcNow;
        var nextUpdate = thisUpdate.AddDays(30);
        var revoked = new List<(string SerialNumber, DateTime RevocationDate, CertificateRevocationReason? Reason)>
        {
            ("AAAA", thisUpdate, CertificateRevocationReason.KeyCompromise),
            ("BBBB", thisUpdate, CertificateRevocationReason.Superseded),
        };

        // Act
        var crl = CertificateRevocationList.Generate(
            "/C=CN/O=Test Org/CN=Test CA",
            keyPair.PrivateKey,
            revoked,
            thisUpdate,
            nextUpdate,
            "SHA256WITHRSA");

        // Assert
        crl.Issuer.Should().Contain("CN=Test CA");
        crl.RevokedCertificatesCount.Should().Be(2);
        crl.IsRevoked("AAAA").Should().BeTrue();
        crl.IsRevoked("BBBB").Should().BeTrue();
        crl.IsRevoked("CCCC").Should().BeFalse();
        crl.ThisUpdate.Should().BeCloseTo(thisUpdate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Generate_WithoutReasons_ShouldHaveNullReasons()
    {
        // Arrange
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var now = DateTime.UtcNow;

        // Act
        var crl = CertificateRevocationList.Generate(
            "/CN=Test CA",
            keyPair.PrivateKey,
            [("1001", now, null)],
            now,
            now.AddDays(30));

        // Assert
        var entry = crl.RevokedCertificates!.Single();
        entry.SerialNumber.Should().Be("1001");
        entry.RevocationReason.Should().BeNull("未指定原因时不添加 Reason 扩展");
    }

    #endregion
}
