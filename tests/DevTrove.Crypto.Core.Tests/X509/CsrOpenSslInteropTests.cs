using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.TestSupport;
using DevTrove.Crypto.X509;
using DevTrove.Crypto.X509.Enums;
using DevTrove.Crypto.X509.Models;

namespace DevTrove.Crypto.Core.Tests.X509;

/// <summary>
/// CSR 对照测试：openssl 生成 → 代码解析、代码生成 → openssl 解析、素材双方解析对照。
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "OpenSSL")]
public class CsrOpenSslInteropTests : IDisposable
{
    private readonly string _tempDir;

    public CsrOpenSslInteropTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"csr_interop_{Guid.NewGuid():N}");
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
    public async Task OpenSslGeneratedCsr_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - openssl 运行时生成密钥与 CSR
        var keyPath = Path.Combine(_tempDir, "key.pem");
        var csrPath = Path.Combine(_tempDir, "req.csr");
        await OpenSslCli.GenerateRsaKeyAsync(keyPath);

        var genCsr = await OpenSslCli.GenerateCsrAsync(
            keyPath,
            csrPath,
            "/CN=csr-interop.example.com",
            subjectAlternativeNames: "DNS:csr-interop.example.com,DNS:www.csr-interop.example.com",
            keyUsage: "critical,digitalSignature,keyEncipherment",
            extendedKeyUsage: "serverAuth");
        genCsr.IsSuccess.Should().BeTrue(genCsr.FullOutput);

        // Act - 代码解析
        var csr = CertificateSigningRequest.FromPem(await File.ReadAllTextAsync(csrPath));

        // Assert
        csr.Subject.Should().Contain("CN=csr-interop.example.com");
        csr.SignatureAlgorithmName.Should().BeEquivalentTo("SHA256WITHRSA");
        csr.Verify().Should().BeTrue();
        csr.KeyUsages.Should().NotBeNull();
        csr.KeyUsages!.Value.Should().HaveFlag(KeyUsage.DigitalSignature);
        csr.ExtendedKeyUsages.Should().NotBeNull();
        csr.ExtendedKeyUsages!.Value.Should().HaveFlag(ExtendedKeyUsage.ServerAuthentication);
        csr.SubjectAlternativeNames.Should().Contain(n => n.Value == "csr-interop.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Value == "www.csr-interop.example.com");
    }

    [Fact]
    public async Task CodeGeneratedCsr_ShouldBeParsedByOpenSsl()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - 代码生成含扩展 CSR
        var keyPair = AsymmetricKeyPair.GenerateRsa(2048);
        var csr = CertificateSigningRequest.Generate(
            "/C=CN/O=Interop Corp/CN=csr-b2o.example.com",
            keyPair,
            "SHA256WITHRSA",
            new X509ExtensionOptions
            {
                KeyUsages = KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment,
                ExtendedKeyUsages = ExtendedKeyUsage.ServerAuthentication,
                SubjectAlternativeNames = [new GeneralName(GeneralNameType.DnsName, "csr-b2o.example.com")],
            });

        var csrPath = Path.Combine(_tempDir, "csr-b2o.csr");
        await File.WriteAllTextAsync(csrPath, csr.ToPem());

        // Act - openssl 解析并验证
        var result = await OpenSslCli.ReqInfoAsync(csrPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("CN = csr-b2o.example.com");
        result.StandardOutput.Should().Contain("DNS:csr-b2o.example.com");
        result.StandardOutput.Should().Contain("Digital Signature");
        result.StandardOutput.Should().Contain("TLS Web Server Authentication");
        result.FullOutput.Should().Contain("verify OK");
    }

    [Fact]
    public async Task Fixture_CodeAndOpenSsl_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        var fixturePath = TestData.Csrs("rsa-2048-san.csr");

        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(fixturePath));
        var result = await OpenSslCli.ReqInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        result.StandardOutput.Should().Contain("CN = multi.example.com");
        result.StandardOutput.Should().Contain("DNS:multi.example.com");
        result.FullOutput.Should().Contain("verify OK");

        csr.Subject.Should().Contain("CN=multi.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Value == "multi.example.com");
        csr.Verify().Should().BeTrue();
    }

    [Fact]
    public async Task TongsuoGeneratedSm2Csr_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);

        // Arrange - tongsuo 运行时生成 SM2 密钥与 CSR
        var keyPath = Path.Combine(_tempDir, "sm2-key.pem");
        var csrPath = Path.Combine(_tempDir, "sm2-req.csr");
        var genKey = await TongsuoCli.GenerateSm2KeyPairAsync(keyPath);
        genKey.IsSuccess.Should().BeTrue(genKey.FullOutput);

        var genCsr = await TongsuoCli.GenerateSm2CsrAsync(
            keyPath,
            csrPath,
            "/C=CN/O=SM2 Corp/CN=sm2-csr-interop.example.cn",
            subjectAlternativeNames: "DNS:sm2-csr-interop.example.cn",
            keyUsage: "critical,digitalSignature");
        genCsr.IsSuccess.Should().BeTrue(genCsr.FullOutput);

        // Act - 代码解析
        var csr = CertificateSigningRequest.FromPem(await File.ReadAllTextAsync(csrPath));

        // Assert
        csr.Subject.Should().Contain("CN=sm2-csr-interop.example.cn");
        csr.SignatureAlgorithmName.Should().BeEquivalentTo("SM3WITHSM2");
        csr.Verify().Should().BeTrue();
        csr.KeyUsages.Should().NotBeNull();
        csr.KeyUsages!.Value.Should().HaveFlag(KeyUsage.DigitalSignature);
        csr.SubjectAlternativeNames.Should().Contain(n => n.Value == "sm2-csr-interop.example.cn");
    }

    [Fact]
    public async Task CodeGeneratedSm2Csr_ShouldBeParsedByTongsuo()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);

        // Arrange - 代码生成 SM2 含扩展 CSR
        var keyPair = AsymmetricKeyPair.GenerateSm2();
        var csr = CertificateSigningRequest.Generate(
            "/C=CN/O=SM2 Corp/CN=sm2-b2o-csr.example.cn",
            keyPair,
            "SM3WITHSM2",
            new X509ExtensionOptions
            {
                KeyUsages = KeyUsage.DigitalSignature,
                SubjectAlternativeNames = [new GeneralName(GeneralNameType.DnsName, "sm2-b2o-csr.example.cn")],
            });

        var csrPath = Path.Combine(_tempDir, "sm2-b2o.csr");
        await File.WriteAllTextAsync(csrPath, csr.ToPem());

        // Act - tongsuo 解析并验证
        var result = await TongsuoCli.ReqInfoAsync(csrPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("sm2-b2o-csr.example.cn");
        result.StandardOutput.Should().Contain("DNS:sm2-b2o-csr.example.cn");
        result.FullOutput.Should().Contain("verify OK");
    }

    [Fact]
    public async Task Fixture_Sm2Csr_CodeAndTongsuo_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);

        // 同一素材
        var fixturePath = TestData.Csrs("sm2-ext.csr");

        // 代码解析
        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(fixturePath));

        // tongsuo 解析并验证
        var result = await TongsuoCli.ReqInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        // 双方一致
        result.StandardOutput.Should().Contain("sm2-test.example.cn");
        result.StandardOutput.Should().Contain("DNS:sm2-test.example.cn");
        result.FullOutput.Should().Contain("verify OK");

        csr.Subject.Should().Contain("CN=sm2-test.example.cn");
        csr.SignatureAlgorithmName.Should().BeEquivalentTo("SM3WITHSM2");
        csr.Verify().Should().BeTrue();
    }

    #region EC（openssl）

    [Fact]
    public async Task OpenSslGeneratedEcCsr_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - openssl 运行时生成 EC 密钥与 CSR
        var keyPath = Path.Combine(_tempDir, "ec-key.pem");
        var csrPath = Path.Combine(_tempDir, "ec-req.csr");
        await OpenSslCli.GenerateEcKeyAsync(keyPath);

        var genCsr = await OpenSslCli.GenerateCsrAsync(
            keyPath,
            csrPath,
            "/CN=ec-csr-interop.example.com",
            subjectAlternativeNames: "DNS:ec-csr-interop.example.com,DNS:www.ec-csr-interop.example.com",
            keyUsage: "critical,digitalSignature",
            extendedKeyUsage: "serverAuth");
        genCsr.IsSuccess.Should().BeTrue(genCsr.FullOutput);

        // Act - 代码解析
        var csr = CertificateSigningRequest.FromPem(await File.ReadAllTextAsync(csrPath));

        // Assert
        csr.Subject.Should().Contain("CN=ec-csr-interop.example.com");
        csr.SignatureAlgorithmName.Should().BeEquivalentTo("SHA256WITHECDSA");
        csr.Verify().Should().BeTrue();
        csr.KeyUsages.Should().NotBeNull();
        csr.KeyUsages!.Value.Should().HaveFlag(KeyUsage.DigitalSignature);
        csr.SubjectAlternativeNames.Should().Contain(n => n.Value == "ec-csr-interop.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Value == "www.ec-csr-interop.example.com");
    }

    [Fact]
    public async Task CodeGeneratedEcCsr_ShouldBeParsedByOpenSsl()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - 代码生成 EC 含扩展 CSR
        var keyPair = AsymmetricKeyPair.GenerateEc("secp256r1");
        var csr = CertificateSigningRequest.Generate(
            "/C=CN/O=Interop Corp/CN=ec-csr-b2o.example.com",
            keyPair,
            "SHA256WITHECDSA",
            new X509ExtensionOptions
            {
                KeyUsages = KeyUsage.DigitalSignature,
                ExtendedKeyUsages = ExtendedKeyUsage.ServerAuthentication,
                SubjectAlternativeNames = [new GeneralName(GeneralNameType.DnsName, "ec-csr-b2o.example.com")],
            });

        var csrPath = Path.Combine(_tempDir, "ec-csr-b2o.csr");
        await File.WriteAllTextAsync(csrPath, csr.ToPem());

        // Act - openssl 解析并验证
        var result = await OpenSslCli.ReqInfoAsync(csrPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("CN = ec-csr-b2o.example.com");
        result.StandardOutput.Should().Contain("DNS:ec-csr-b2o.example.com");
        result.StandardOutput.Should().Contain("Digital Signature");
        result.FullOutput.Should().Contain("verify OK");
    }

    [Fact]
    public async Task Fixture_EcCsr_CodeAndOpenSsl_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        var fixturePath = TestData.Csrs("ec-p256-san.csr");

        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(fixturePath));
        var result = await OpenSslCli.ReqInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        result.StandardOutput.Should().Contain("CN = ec-multi.example.com");
        result.StandardOutput.Should().Contain("DNS:ec-multi.example.com");
        result.FullOutput.Should().Contain("verify OK");

        csr.Subject.Should().Contain("CN=ec-multi.example.com");
        csr.SubjectAlternativeNames.Should().Contain(n => n.Value == "ec-multi.example.com");
        csr.Verify().Should().BeTrue();
    }

    #endregion

    #region DSA（openssl）

    [Fact]
    public async Task OpenSslGeneratedDsaCsr_ShouldBeParsedByCode()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - openssl 运行时生成 DSA 密钥与 CSR
        var keyPath = Path.Combine(_tempDir, "dsa-key.pem");
        var csrPath = Path.Combine(_tempDir, "dsa-req.csr");
        var genKey = await OpenSslCli.GenerateDsaKeyAsync(keyPath);
        genKey.IsSuccess.Should().BeTrue(genKey.FullOutput);

        var genCsr = await OpenSslCli.GenerateCsrAsync(
            keyPath,
            csrPath,
            "/CN=dsa-csr-interop.example.com",
            keyUsage: "critical,digitalSignature");
        genCsr.IsSuccess.Should().BeTrue(genCsr.FullOutput);

        // Act - 代码解析
        var csr = CertificateSigningRequest.FromPem(await File.ReadAllTextAsync(csrPath));

        // Assert
        csr.Subject.Should().Contain("CN=dsa-csr-interop.example.com");
        csr.SignatureAlgorithmName.Should().Contain("DSA");
        csr.Verify().Should().BeTrue();
        csr.KeyUsages.Should().NotBeNull();
        csr.KeyUsages!.Value.Should().HaveFlag(KeyUsage.DigitalSignature);
    }

    [Fact]
    public async Task CodeGeneratedDsaCsr_ShouldBeParsedByOpenSsl()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange - 代码生成 DSA 含扩展 CSR
        var keyPair = AsymmetricKeyPair.GenerateDsa(2048);
        var csr = CertificateSigningRequest.Generate(
            "/C=CN/O=DSA Corp/CN=dsa-csr-b2o.example.com",
            keyPair,
            "SHA256WITHDSA",
            new X509ExtensionOptions
            {
                KeyUsages = KeyUsage.DigitalSignature,
            });

        var csrPath = Path.Combine(_tempDir, "dsa-csr-b2o.csr");
        await File.WriteAllTextAsync(csrPath, csr.ToPem());

        // Act - openssl 解析并验证
        var result = await OpenSslCli.ReqInfoAsync(csrPath);

        // Assert
        result.IsSuccess.Should().BeTrue(result.FullOutput);
        result.StandardOutput.Should().Contain("CN = dsa-csr-b2o.example.com");
        result.StandardOutput.Should().Contain("Digital Signature");
        result.FullOutput.Should().Contain("verify OK");
    }

    [Fact]
    public async Task Fixture_DsaCsr_CodeAndOpenSsl_ShouldAgree()
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        var fixturePath = TestData.Csrs("dsa-2048-basic.csr");

        var csr = CertificateSigningRequest.FromPem(File.ReadAllText(fixturePath));
        var result = await OpenSslCli.ReqInfoAsync(fixturePath);
        result.IsSuccess.Should().BeTrue(result.FullOutput);

        result.StandardOutput.Should().Contain("CN = dsa-csr.example.com");
        result.FullOutput.Should().Contain("verify OK");

        csr.Subject.Should().Contain("CN=dsa-csr.example.com");
        csr.Verify().Should().BeTrue();
    }

    #endregion
}
