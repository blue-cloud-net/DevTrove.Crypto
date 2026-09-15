using CliWrap;
using CliWrap.Buffered;

namespace DevTrove.Crypto.TestSupport;

/// <summary>
/// Tongsuo（国密版 OpenSSL）命令行工具静态封装，用于测试场景中生成和验证 SM2 / SM3 / SM4 相关密钥、证书、CSR 及签名/加解密互操作。
/// 默认二进制为 <c>/opt/tongsuo/bin/tongsuo</c>，可通过环境变量 <c>TONGSUO_PATH</c> 覆盖。
/// </summary>
public static class TongsuoCli
{
    /// <summary>
    /// 获取 tongsuo 可执行文件路径（默认 <c>/opt/tongsuo/bin/tongsuo</c>，可被环境变量 TONGSUO_PATH 覆盖）。
    /// </summary>
    public static string BinaryPath =>
        Environment.GetEnvironmentVariable("TONGSUO_PATH") ?? "/opt/tongsuo/bin/tongsuo";

    /// <summary>
    /// 执行 tongsuo 命令。
    /// </summary>
    /// <param name="arguments">命令参数（不含 tongsuo 前缀）。</param>
    /// <param name="stdin">标准输入数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>执行结果。</returns>
    public static async Task<OpenSslResult> ExecuteAsync(
        string arguments,
        string? stdin = null,
        CancellationToken cancellationToken = default)
    {
        var cmd = Cli.Wrap(BinaryPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None);

        if (stdin != null)
        {
            cmd = cmd.WithStandardInputPipe(PipeSource.FromString(stdin));
        }

        var result = await cmd.ExecuteBufferedAsync(cancellationToken);

        return new OpenSslResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
        };
    }

    /// <summary>
    /// 获取 tongsuo 版本信息。
    /// </summary>
    public static Task<OpenSslResult> GetVersionAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync("version", cancellationToken: cancellationToken);

    /// <summary>
    /// 执行通用的 tongsuo 命令。
    /// </summary>
    public static Task<OpenSslResult> ExecuteCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(command, cancellationToken: cancellationToken);

    #region SM2 密钥

    /// <summary>
    /// 生成 SM2 密钥对（PEM 格式）。
    /// </summary>
    /// <param name="privateKeyPath">私钥输出路径。</param>
    /// <param name="publicKeyPath">公钥输出路径（可选）。</param>
    public static async Task<OpenSslResult> GenerateSm2KeyPairAsync(
        string privateKeyPath,
        string? publicKeyPath = null,
        CancellationToken cancellationToken = default)
    {
        // 使用 genpkey 生成私钥，避免生成 SM2 PARAMETERS 部分。
        var genResult = await ExecuteAsync(
            $"genpkey -algorithm EC -pkeyopt ec_paramgen_curve:SM2 -out \"{privateKeyPath}\"",
            cancellationToken: cancellationToken);

        if (genResult.ExitCode != 0 || publicKeyPath == null)
        {
            return genResult;
        }

        // 从私钥导出公钥。
        return await ExecuteAsync(
            $"pkey -in \"{privateKeyPath}\" -pubout -out \"{publicKeyPath}\"",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 查看密钥详细信息（pkey -text）。
    /// </summary>
    /// <param name="keyPath">密钥文件路径。</param>
    /// <param name="isPublicKey">是否为公钥。</param>
    /// <param name="format">密钥格式（PEM 或 DER）。</param>
    public static Task<OpenSslResult> ViewKeyInfoAsync(
        string keyPath,
        bool isPublicKey = false,
        string format = "PEM",
        CancellationToken cancellationToken = default)
    {
        var pubinArg = isPublicKey ? "-pubin" : "";
        var informArg = format.Equals("DER", StringComparison.OrdinalIgnoreCase) ? "-inform DER" : "";
        return ExecuteAsync(
            $"pkey {pubinArg} {informArg} -in \"{keyPath}\" -text -noout",
            cancellationToken: cancellationToken);
    }

    #endregion

    #region SM2 签名 / 验签

    /// <summary>
    /// 使用 SM2 私钥对数据签名。
    /// </summary>
    /// <param name="dataPath">待签名数据文件。</param>
    /// <param name="privateKeyPath">私钥路径。</param>
    /// <param name="signaturePath">签名输出路径。</param>
    /// <param name="userId">用户 ID（默认 "1234567812345678"，与 BouncyCastle 一致）。</param>
    public static Task<OpenSslResult> SignWithSm2Async(
        string dataPath,
        string privateKeyPath,
        string signaturePath,
        string userId = "1234567812345678",
        CancellationToken cancellationToken = default)
    {
        var args = $"dgst -sm3 -sign \"{privateKeyPath}\" -sigopt distid:{userId} -out \"{signaturePath}\" \"{dataPath}\"";
        return ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 使用 SM2 公钥验证签名。
    /// </summary>
    /// <param name="dataPath">原始数据文件。</param>
    /// <param name="signaturePath">签名文件。</param>
    /// <param name="publicKeyPath">公钥路径。</param>
    /// <param name="userId">用户 ID（必须与签名时一致）。</param>
    public static Task<OpenSslResult> VerifyWithSm2Async(
        string dataPath,
        string signaturePath,
        string publicKeyPath,
        string userId = "1234567812345678",
        CancellationToken cancellationToken = default)
    {
        var args = $"dgst -sm3 -verify \"{publicKeyPath}\" -signature \"{signaturePath}\" -sigopt distid:{userId} \"{dataPath}\"";
        return ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    #endregion

    #region SM2 加解密

    /// <summary>
    /// 使用 SM2 公钥加密数据。
    /// </summary>
    public static Task<OpenSslResult> EncryptWithSm2Async(
        string inputPath,
        string outputPath,
        string publicKeyPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(
            $"pkeyutl -encrypt -in \"{inputPath}\" -out \"{outputPath}\" -pubin -inkey \"{publicKeyPath}\"",
            cancellationToken: cancellationToken);

    /// <summary>
    /// 使用 SM2 私钥解密数据。
    /// </summary>
    public static Task<OpenSslResult> DecryptWithSm2Async(
        string inputPath,
        string outputPath,
        string privateKeyPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(
            $"pkeyutl -decrypt -in \"{inputPath}\" -out \"{outputPath}\" -inkey \"{privateKeyPath}\"",
            cancellationToken: cancellationToken);

    #endregion

    #region SM2 证书 / CSR

    /// <summary>
    /// 使用 SM2 私钥生成自签名证书。
    /// </summary>
    /// <param name="privateKeyPath">SM2 私钥路径。</param>
    /// <param name="certificatePath">证书输出路径。</param>
    /// <param name="subject">证书主体（如 "/CN=sm2-test.example.cn"）。</param>
    /// <param name="days">有效期天数。</param>
    /// <param name="subjectAlternativeNames">SAN，如 "DNS:sm2-test.example.cn,DNS:www.sm2-test.example.cn"。</param>
    public static Task<OpenSslResult> GenerateSm2SelfSignedCertificateAsync(
        string privateKeyPath,
        string certificatePath,
        string subject,
        int days = 365,
        string? subjectAlternativeNames = null,
        string? keyUsage = null,
        string? extendedKeyUsage = null,
        CancellationToken cancellationToken = default)
    {
        var configPath = certificatePath + ".cnf";
        var config = BuildReqConfig(subjectAlternativeNames, keyUsage, extendedKeyUsage);
        File.WriteAllText(configPath, config);

        var args = $"req -x509 -new -key \"{privateKeyPath}\" -out \"{certificatePath}\" " +
                   $"-days {days} -subj \"{subject}\" -config \"{configPath}\"";
        return ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 使用 SM2 私钥生成 CSR。
    /// </summary>
    public static Task<OpenSslResult> GenerateSm2CsrAsync(
        string privateKeyPath,
        string csrPath,
        string subject,
        string? subjectAlternativeNames = null,
        string? keyUsage = null,
        string? extendedKeyUsage = null,
        CancellationToken cancellationToken = default)
    {
        var configPath = csrPath + ".cnf";
        var config = BuildReqConfig(subjectAlternativeNames, keyUsage, extendedKeyUsage);
        File.WriteAllText(configPath, config);

        var args = $"req -new -key \"{privateKeyPath}\" -out \"{csrPath}\" -subj \"{subject}\" -config \"{configPath}\"";
        return ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 查看证书详细信息（x509 -text）。
    /// </summary>
    public static Task<OpenSslResult> X509InfoAsync(
        string certificatePath,
        string format = "PEM",
        CancellationToken cancellationToken = default)
    {
        var informArg = format.Equals("DER", StringComparison.OrdinalIgnoreCase) ? "-inform DER" : "";
        return ExecuteAsync($"x509 {informArg} -in \"{certificatePath}\" -text -noout", cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 查看 CSR 详细信息并验证签名（req -text -verify）。
    /// </summary>
    public static Task<OpenSslResult> ReqInfoAsync(
        string csrPath,
        string format = "PEM",
        CancellationToken cancellationToken = default)
    {
        var informArg = format.Equals("DER", StringComparison.OrdinalIgnoreCase) ? "-inform DER" : "";
        return ExecuteAsync($"req {informArg} -in \"{csrPath}\" -text -verify -noout", cancellationToken: cancellationToken);
    }

    #endregion

    #region SM2 CRL

    /// <summary>
    /// 在指定目录中初始化最小 SM2 CA 数据库（index.txt / serial / newcerts / ca 配置），供 <see cref="GenerateSm2CrlAsync"/> 使用。
    /// SM2 CRL 使用 SM3 摘要，故配置文件 default_md=sm3。
    /// </summary>
    /// <param name="caDir">CA 目录（会被创建）。</param>
    /// <param name="caKeyPath">CA 私钥路径。</param>
    /// <param name="caCertPath">CA 证书路径。</param>
    /// <param name="configPath">生成的 ca 配置文件路径。</param>
    /// <param name="revokedSerialHex">预置的已吊销证书序列号（十六进制，可选）。</param>
    public static async Task SetupSm2CaDatabaseAsync(
        string caDir,
        string caKeyPath,
        string caCertPath,
        string configPath,
        string? revokedSerialHex = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(caDir);
        Directory.CreateDirectory(Path.Combine(caDir, "newcerts"));

        // index.txt：可选预置一条吊销记录（OpenSSL 格式：R\t<expiry>\t<revocation>\t<serial hex>\t<filename>\t<DN>，日期为 YYMMDDHHMMSSZ）。
        var indexContent = string.IsNullOrEmpty(revokedSerialHex)
            ? string.Empty
            : $"R\t370101000000Z\t260101000000Z\t{revokedSerialHex}\tunknown\t/CN=revoked\n";
        await File.WriteAllTextAsync(Path.Combine(caDir, "index.txt"), indexContent, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(caDir, "index.txt.attr"), "unique_subject = no\n", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(caDir, "serial"), "01\n", cancellationToken);

        var config = $"""
            [ ca ]
            default_ca = CA_default

            [ CA_default ]
            dir = {caDir}
            database = {caDir}/index.txt
            new_certs_dir = {caDir}/newcerts
            certificate = {caCertPath}
            private_key = {caKeyPath}
            serial = {caDir}/serial
            default_md = sm3
            default_days = 365
            default_crl_days = 30
            policy = policy_any

            [ policy_any ]
            commonName = supplied

            """;
        await File.WriteAllTextAsync(configPath, config, cancellationToken);
    }

    /// <summary>
    /// 使用已初始化的 CA 数据库生成 SM2 CRL（tongsuo ca -gencrl）。
    /// </summary>
    public static Task<OpenSslResult> GenerateSm2CrlAsync(
        string configPath,
        string crlPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync($"ca -config \"{configPath}\" -gencrl -out \"{crlPath}\"", cancellationToken: cancellationToken);

    /// <summary>
    /// 查看 CRL 详细信息（crl -text）。
    /// </summary>
    public static Task<OpenSslResult> CrlInfoAsync(
        string crlPath,
        string format = "PEM",
        CancellationToken cancellationToken = default)
    {
        var informArg = format.Equals("DER", StringComparison.OrdinalIgnoreCase) ? "-inform DER" : "";
        return ExecuteAsync($"crl {informArg} -in \"{crlPath}\" -text -noout", cancellationToken: cancellationToken);
    }

    #endregion

    #region 格式转换

    /// <summary>
    /// 将 PEM 私钥转换为 DER (PKCS#8)。
    /// </summary>
    public static Task<OpenSslResult> ConvertPrivateKeyPemToDerAsync(
        string pemPath,
        string derPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync($"pkcs8 -topk8 -nocrypt -in \"{pemPath}\" -out \"{derPath}\" -outform DER", cancellationToken: cancellationToken);

    /// <summary>
    /// 将 DER 私钥转换为 PEM。
    /// </summary>
    public static Task<OpenSslResult> ConvertPrivateKeyDerToPemAsync(
        string derPath,
        string pemPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync($"pkey -in \"{derPath}\" -inform DER -out \"{pemPath}\" -outform PEM", cancellationToken: cancellationToken);

    /// <summary>
    /// 将 PEM 公钥转换为 DER (SPKI)。
    /// </summary>
    public static Task<OpenSslResult> ConvertPublicKeyPemToDerAsync(
        string pemPath,
        string derPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync($"pkey -pubin -in \"{pemPath}\" -out \"{derPath}\" -outform DER", cancellationToken: cancellationToken);

    /// <summary>
    /// 将 DER 公钥转换为 PEM。
    /// </summary>
    public static Task<OpenSslResult> ConvertPublicKeyDerToPemAsync(
        string derPath,
        string pemPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync($"pkey -pubin -in \"{derPath}\" -inform DER -out \"{pemPath}\" -outform PEM", cancellationToken: cancellationToken);

    #endregion

    #region 支持性探测

    /// <summary>
    /// 检查 tongsuo 是否支持 SM2。
    /// </summary>
    public static async Task<bool> IsSm2SupportedAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync("ecparam -list_curves", cancellationToken: cancellationToken);
        return result.StandardOutput.Contains("SM2", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查 tongsuo 是否支持 SM3。
    /// </summary>
    public static async Task<bool> IsSm3SupportedAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync("dgst -list", cancellationToken: cancellationToken);
        return result.StandardOutput.Contains("sm3", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查 tongsuo 是否支持 SM4。
    /// </summary>
    public static async Task<bool> IsSm4SupportedAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync("enc -list", cancellationToken: cancellationToken);
        return result.StandardOutput.Contains("sm4", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 私有工具

    private static string BuildReqConfig(string? subjectAlternativeNames, string? keyUsage = null, string? extendedKeyUsage = null)
    {
        var extensions = new List<string>();
        if (!string.IsNullOrEmpty(keyUsage))
        {
            extensions.Add($"keyUsage = {keyUsage}");
        }
        if (!string.IsNullOrEmpty(extendedKeyUsage))
        {
            extensions.Add($"extendedKeyUsage = {extendedKeyUsage}");
        }
        if (!string.IsNullOrEmpty(subjectAlternativeNames))
        {
            extensions.Add($"subjectAltName = {subjectAlternativeNames}");
        }

        var reqExt = extensions.Count > 0
            ? string.Join(Environment.NewLine, extensions) + Environment.NewLine
            : string.Empty;

        return $"""
            [ req ]
            distinguished_name = dn
            prompt = no
            req_extensions = req_ext
            x509_extensions = req_ext

            [ dn ]

            [ req_ext ]
            {reqExt}
            """;
    }

    #endregion
}
