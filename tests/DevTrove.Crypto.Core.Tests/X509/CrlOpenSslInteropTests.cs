using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.TestSupport;
using DevTrove.Crypto.X509;
using CertificateRevocationReason = DevTrove.Crypto.X509.Enums.CertificateRevocationReason;

namespace DevTrove.Crypto.Core.Tests.X509;

/// <summary>
/// CRL 对照测试：代码生成 → openssl 解析、素材双方解析对照。
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "OpenSSL")]
public class CrlOpenSslInteropTests : IDisposable
{
    private readonly string _tempDir;

    public CrlOpenSslInteropTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"crl_interop_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // 忽略清理错误。
            }
        }
    }

    [Fact]
    public async Task CodeGeneratedCrl_ShouldBeParsedByOpenSsl()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - 代码生成 CRL
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var now = DateTime.UtcNow;
        var crl = CertificateRevocationList.Generate(
            "/C=CN/O=Interop Org/CN=Interop CA",
            keyPair.PrivateKey,
            [("AAAA", now, CertificateRevocationReason.KeyCompromise)],
            now,
            now.AddDays(30),
            "SHA256WITHRSA");

        var crlPath = Path.Combine(_tempDir, "interop.crl");
        await File.WriteAllTextAsync(crlPath, crl.ToPem());

        // Act - openssl 解析
        var result = await OpenSslCli.CrlInfoAsync(crlPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("CN = Interop CA");
        result.StandardOutput.Should().Contain("Serial Number: AAAA");
        result.StandardOutput.Should().Contain("Key Compromise");
    }

    [Fact]
    public async Task Fixture_CodeAndOpenSsl_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        var fixturePath = TestData.Crls("test.crl");

        var crl = CertificateRevocationList.FromPem(File.ReadAllText(fixturePath));
        var result = await OpenSslCli.CrlInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        result.StandardOutput.Should().Contain("CN = Test CRL CA");
        result.StandardOutput.Should().Contain("Serial Number: 1111");
        result.StandardOutput.Should().Contain("Serial Number: 2222");

        crl.Issuer.Should().Contain("CN=Test CRL CA");
        crl.RevokedCertificatesCount.Should().Be(2);
        crl.IsRevoked("1111").Should().BeTrue();
    }

    #region RSA A：openssl 生成 → 代码解析

    [Fact]
    public async Task OpenSslGeneratedCrl_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - openssl 运行时生成 CA 证书与含吊销条目的 CRL
        var caKeyPath = Path.Combine(_tempDir, "ca-key.pem");
        var caCertPath = Path.Combine(_tempDir, "ca-cert.pem");
        var caDir = Path.Combine(_tempDir, "cadb");
        var configPath = Path.Combine(_tempDir, "ca.cnf");
        var crlPath = Path.Combine(_tempDir, "generated.crl");

        var genKey = await OpenSslCli.GenerateRsaKeyAsync(caKeyPath);
        genKey.IsSuccess.Should().BeTrue(genKey.FullOutput);

        var genCert = await OpenSslCli.GenerateSelfSignedCertificateAsync(
            caKeyPath, caCertPath, "/C=CN/O=Interop Org/CN=RSA Interop CRL CA", days: 3650, isCa: true, includeSki: true);
        genCert.IsSuccess.Should().BeTrue(genCert.FullOutput);

        await OpenSslCli.SetupCaDatabaseAsync(caDir, caKeyPath, caCertPath, configPath, revokedSerialHex: "0F0F");

        var genCrl = await OpenSslCli.GenerateCrlAsync(configPath, crlPath);
        genCrl.IsSuccess.Should().BeTrue(genCrl.FullOutput);

        // Act - 代码解析
        var crl = CertificateRevocationList.FromPem(await File.ReadAllTextAsync(crlPath));

        // Assert
        crl.Issuer.Should().Contain("CN=RSA Interop CRL CA");
        crl.SignatureAlgorithmName.Should().Contain("RSA");
        crl.IsRevoked("0F0F").Should().BeTrue();
    }

    #endregion

    #region EC（openssl）

    [Fact]
    public async Task OpenSslGeneratedEcCrl_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - openssl 运行时生成 EC CA 与 CRL
        var caKeyPath = Path.Combine(_tempDir, "ec-ca-key.pem");
        var caCertPath = Path.Combine(_tempDir, "ec-ca-cert.pem");
        var caDir = Path.Combine(_tempDir, "ec-cadb");
        var configPath = Path.Combine(_tempDir, "ec-ca.cnf");
        var crlPath = Path.Combine(_tempDir, "ec-generated.crl");

        var genKey = await OpenSslCli.GenerateEcKeyAsync(caKeyPath);
        genKey.IsSuccess.Should().BeTrue(genKey.FullOutput);

        var genCert = await OpenSslCli.GenerateSelfSignedCertificateAsync(
            caKeyPath, caCertPath, "/C=CN/O=EC Org/CN=EC Interop CRL CA", days: 3650, isCa: true, includeSki: true);
        genCert.IsSuccess.Should().BeTrue(genCert.FullOutput);

        await OpenSslCli.SetupCaDatabaseAsync(caDir, caKeyPath, caCertPath, configPath, revokedSerialHex: "ECEF");

        var genCrl = await OpenSslCli.GenerateCrlAsync(configPath, crlPath);
        genCrl.IsSuccess.Should().BeTrue(genCrl.FullOutput);

        // Act - 代码解析
        var crl = CertificateRevocationList.FromPem(await File.ReadAllTextAsync(crlPath));

        // Assert
        crl.Issuer.Should().Contain("CN=EC Interop CRL CA");
        crl.SignatureAlgorithmName.Should().Contain("ECDSA");
        crl.IsRevoked("ECEF").Should().BeTrue();
    }

    [Fact]
    public async Task CodeGeneratedEcCrl_ShouldBeParsedByOpenSsl()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - 代码生成 EC CRL
        var keyPair = AsymmetricKeyPair.GenerateEc("secp256r1");
        var now = DateTime.UtcNow;
        var crl = CertificateRevocationList.Generate(
            "/C=CN/O=EC Org/CN=EC Interop CRL CA",
            keyPair.PrivateKey,
            [("0C0C", now, CertificateRevocationReason.KeyCompromise)],
            now,
            now.AddDays(30),
            "SHA256WITHECDSA");

        var crlPath = Path.Combine(_tempDir, "ec-interop.crl");
        await File.WriteAllTextAsync(crlPath, crl.ToPem());

        // Act - openssl 解析
        var result = await OpenSslCli.CrlInfoAsync(crlPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("CN = EC Interop CRL CA");
        result.StandardOutput.Should().Contain("Serial Number: 0C0C");
        result.StandardOutput.Should().Contain("Key Compromise");
    }

    [Fact]
    public async Task Fixture_EcCrl_CodeAndOpenSsl_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        var fixturePath = TestData.Crls("ec.crl");

        var crl = CertificateRevocationList.FromPem(File.ReadAllText(fixturePath));
        var result = await OpenSslCli.CrlInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        result.StandardOutput.Should().Contain("CN = EC Test CRL CA");
        result.StandardOutput.Should().Contain("Serial Number: EC01");

        crl.Issuer.Should().Contain("CN=EC Test CRL CA");
        crl.IsRevoked("EC01").Should().BeTrue();
    }

    #endregion

    #region SM2（tongsuo）

    [Fact]
    public async Task TongsuoGeneratedSm2Crl_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);

        // Arrange - tongsuo 运行时生成 SM2 CA 与 CRL
        var caKeyPath = Path.Combine(_tempDir, "sm2-ca-key.pem");
        var caCertPath = Path.Combine(_tempDir, "sm2-ca-cert.pem");
        var caDir = Path.Combine(_tempDir, "sm2-cadb");
        var configPath = Path.Combine(_tempDir, "sm2-ca.cnf");
        var crlPath = Path.Combine(_tempDir, "sm2-generated.crl");

        var genKey = await TongsuoCli.GenerateSm2KeyPairAsync(caKeyPath);
        genKey.IsSuccess.Should().BeTrue(genKey.FullOutput);

        var genCert = await TongsuoCli.GenerateSm2SelfSignedCertificateAsync(
            caKeyPath, caCertPath, "/C=CN/O=SM2 Org/CN=SM2 Interop CRL CA", days: 3650);
        genCert.IsSuccess.Should().BeTrue(genCert.FullOutput);

        await TongsuoCli.SetupSm2CaDatabaseAsync(caDir, caKeyPath, caCertPath, configPath, revokedSerialHex: "0A0A");

        var genCrl = await TongsuoCli.GenerateSm2CrlAsync(configPath, crlPath);
        genCrl.IsSuccess.Should().BeTrue(genCrl.FullOutput);

        // Act - 代码解析
        var crl = CertificateRevocationList.FromPem(await File.ReadAllTextAsync(crlPath));

        // Assert
        crl.Issuer.Should().Contain("CN=SM2 Interop CRL CA");
        crl.SignatureAlgorithmName.Should().Contain("SM2");
        crl.IsRevoked("0A0A").Should().BeTrue();
    }

    [Fact]
    public async Task CodeGeneratedSm2Crl_ShouldBeParsedByTongsuo()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);

        // Arrange - 代码生成 SM2 CRL
        var keyPair = AsymmetricKeyPair.GenerateSm2();
        var now = DateTime.UtcNow;
        var crl = CertificateRevocationList.Generate(
            "/C=CN/O=SM2 Org/CN=SM2 Interop CRL CA",
            keyPair.PrivateKey,
            [("5A5A", now, CertificateRevocationReason.KeyCompromise)],
            now,
            now.AddDays(30),
            "SM3WITHSM2");

        var crlPath = Path.Combine(_tempDir, "sm2-interop.crl");
        await File.WriteAllTextAsync(crlPath, crl.ToPem());

        // Act - tongsuo 解析
        var result = await TongsuoCli.CrlInfoAsync(crlPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("SM2 Interop CRL CA");
        result.StandardOutput.Should().Contain("Serial Number: 5A5A");
        result.StandardOutput.Should().Contain("Key Compromise");
    }

    [Fact]
    public async Task Fixture_Sm2Crl_CodeAndTongsuo_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);

        var fixturePath = TestData.Crls("sm2.crl");

        var crl = CertificateRevocationList.FromPem(File.ReadAllText(fixturePath));
        var result = await TongsuoCli.CrlInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        result.StandardOutput.Should().Contain("SM2 Test CRL CA");
        result.StandardOutput.Should().Contain("Serial Number: 5A01");

        crl.Issuer.Should().Contain("CN=SM2 Test CRL CA");
        crl.SignatureAlgorithmName.Should().Contain("SM2");
        crl.IsRevoked("5A01").Should().BeTrue();
    }

    #endregion

    #region DSA（openssl）

    [Fact]
    public async Task OpenSslGeneratedDsaCrl_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - openssl 运行时生成 DSA CA 与 CRL
        var caKeyPath = Path.Combine(_tempDir, "dsa-ca-key.pem");
        var caCertPath = Path.Combine(_tempDir, "dsa-ca-cert.pem");
        var caDir = Path.Combine(_tempDir, "dsa-cadb");
        var configPath = Path.Combine(_tempDir, "dsa-ca.cnf");
        var crlPath = Path.Combine(_tempDir, "dsa-generated.crl");

        var genKey = await OpenSslCli.GenerateDsaKeyAsync(caKeyPath);
        genKey.IsSuccess.Should().BeTrue(genKey.FullOutput);

        var genCert = await OpenSslCli.GenerateSelfSignedCertificateAsync(
            caKeyPath, caCertPath, "/C=CN/O=DSA Org/CN=DSA Interop CRL CA", days: 3650, isCa: true, includeSki: true);
        genCert.IsSuccess.Should().BeTrue(genCert.FullOutput);

        await OpenSslCli.SetupCaDatabaseAsync(caDir, caKeyPath, caCertPath, configPath, revokedSerialHex: "0D0D");

        var genCrl = await OpenSslCli.GenerateCrlAsync(configPath, crlPath);
        genCrl.IsSuccess.Should().BeTrue(genCrl.FullOutput);

        // Act - 代码解析
        var crl = CertificateRevocationList.FromPem(await File.ReadAllTextAsync(crlPath));

        // Assert
        crl.Issuer.Should().Contain("CN=DSA Interop CRL CA");
        crl.SignatureAlgorithmName.Should().Contain("DSA");
        crl.IsRevoked("0D0D").Should().BeTrue();
    }

    [Fact]
    public async Task CodeGeneratedDsaCrl_ShouldBeParsedByOpenSsl()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - 代码生成 DSA CRL
        var keyPair = AsymmetricKeyPair.GenerateDsa(2048);
        var now = DateTime.UtcNow;
        var crl = CertificateRevocationList.Generate(
            "/C=CN/O=DSA Org/CN=DSA Interop CRL CA",
            keyPair.PrivateKey,
            [("0D0D", now, CertificateRevocationReason.KeyCompromise)],
            now,
            now.AddDays(30),
            "SHA256WITHDSA");

        var crlPath = Path.Combine(_tempDir, "dsa-interop.crl");
        await File.WriteAllTextAsync(crlPath, crl.ToPem());

        // Act - openssl 解析
        var result = await OpenSslCli.CrlInfoAsync(crlPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("CN = DSA Interop CRL CA");
        result.StandardOutput.Should().Contain("Serial Number: 0D0D");
        result.StandardOutput.Should().Contain("Key Compromise");
    }

    [Fact]
    public async Task Fixture_DsaCrl_CodeAndOpenSsl_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        var fixturePath = TestData.Crls("dsa.crl");

        var crl = CertificateRevocationList.FromPem(File.ReadAllText(fixturePath));
        var result = await OpenSslCli.CrlInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        result.StandardOutput.Should().Contain("CN = DSA Test CRL CA");
        result.StandardOutput.Should().Contain("Serial Number: D500");

        crl.Issuer.Should().Contain("CN=DSA Test CRL CA");
        crl.SignatureAlgorithmName.Should().Contain("DSA");
        crl.IsRevoked("D500").Should().BeTrue();
    }

    #endregion
}
