using CliWrap;
using CliWrap.Buffered;

namespace DevTrove.Crypto.TestSupport;

/// <summary>
/// OpenSSL 命令行工具静态封装，用于测试场景中生成和验证 RSA / EC / DSA 密钥、证书、CSR、CRL、PFX 及加解密/签名互操作。
/// 默认二进制为 <c>openssl</c>，可通过环境变量 <c>OPENSSL_PATH</c> 覆盖。
/// </summary>
public static class OpenSslCli
{
    /// <summary>
    /// 获取 OpenSSL 可执行文件路径（默认 <c>openssl</c>，可被环境变量 OPENSSL_PATH 覆盖）。
    /// </summary>
    public static string BinaryPath =>
        Environment.GetEnvironmentVariable("OPENSSL_PATH") ?? "openssl";

    /// <summary>
    /// 执行 OpenSSL 命令。
    /// </summary>
    /// <param name="arguments">命令参数（不含 <c>openssl</c> 前缀）。</param>
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
    /// 获取 OpenSSL 版本信息。
    /// </summary>
    public static Task<OpenSslResult> GetVersionAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync("version", cancellationToken: cancellationToken);

    /// <summary>
    /// 执行通用的 OpenSSL 命令。
    /// </summary>
    public static Task<OpenSslResult> ExecuteCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(command, cancellationToken: cancellationToken);

    #region 密钥生成

    /// <summary>
    /// 生成 RSA 私钥（PKCS#8 PEM 格式）。
    /// </summary>
    public static Task<OpenSslResult> GenerateRsaKeyAsync(
        string privateKeyPath,
        int bits = 2048,
        CancellationToken cancellationToken = default)
    {
        var args = $"genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:{bits} -out \"{privateKeyPath}\"";
        return ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 生成 EC 私钥（PKCS#8 PEM 格式）。
    /// </summary>
    public static Task<OpenSslResult> GenerateEcKeyAsync(
        string privateKeyPath,
        string curve = "prime256v1",
        CancellationToken cancellationToken = default)
    {
        var args = $"genpkey -algorithm EC -pkeyopt ec_paramgen_curve:{curve} -out \"{privateKeyPath}\"";
        return ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 生成 DSA 参数与私钥（PEM 格式）。
    /// </summary>
    /// <param name="privateKeyPath">私钥输出路径。</param>
    /// <param name="paramPath">参数输出路径（可为 null 时不导出参数）。</param>
    /// <param name="bits">参数位数，默认 2048。</param>
    public static async Task<OpenSslResult> GenerateDsaKeyAsync(
        string privateKeyPath,
        string? paramPath = null,
        int bits = 2048,
        CancellationToken cancellationToken = default)
    {
        var tempParams = paramPath ?? Path.Combine(Path.GetTempPath(), $"dsa_params_{Guid.NewGuid():N}.pem");
        var genParam = await ExecuteAsync(
            $"genpkey -genparam -algorithm DSA -pkeyopt dsa_paramgen_bits:{bits} -out \"{tempParams}\"",
            cancellationToken: cancellationToken);
        if (genParam.ExitCode != 0)
        {
            return genParam;
        }

        var genKey = await ExecuteAsync(
            $"genpkey -paramfile \"{tempParams}\" -out \"{privateKeyPath}\"",
            cancellationToken: cancellationToken);

        if (paramPath == null)
        {
            try
            {
                File.Delete(tempParams);
            }
            catch
            {
                // 忽略清理错误。
            }
        }

        return genKey;
    }

    /// <summary>
    /// 从私钥导出公钥（SPKI PEM 格式）。
    /// </summary>
    public static Task<OpenSslResult> ExportPublicKeyAsync(
        string privateKeyPath,
        string publicKeyPath,
        CancellationToken cancellationToken = default)
        => ExecuteAsync($"pkey -in \"{privateKeyPath}\" -pubout -out \"{publicKeyPath}\"", cancellationToken: cancellationToken);

    #endregion

    #region 密钥解析

    /// <summary>
    /// 查看密钥详细信息（pkey -text）。
    /// </summary>
    /// <param name="keyPath">密钥文件路径。</param>
    /// <param name="isPublicKey">是否为公钥。</param>
    /// <param name="format">密钥格式（PEM 或 DER）。</param>
    public static Task<OpenSslResult> PkeyInfoAsync(
        string keyPath,
        bool isPublicKey = false,
        string format = "PEM",
        CancellationToken cancellationToken = default)
    {
        var pubinArg = isPublicKey ? "-pubin" : "";
        var informArg = format.Equals("DER", StringComparison.OrdinalIgnoreCase) ? "-inform DER" : "";
        return ExecuteAsync($"pkey {pubinArg} {informArg} -in \"{keyPath}\" -text -noout", cancellationToken: cancellationToken);
    }

    #endregion

    #region 证书

    /// <summary>
    /// 使用现有私钥生成自签名证书（可含 SAN / KU / EKU / CRLDP / CA / SKI 扩展）。
    /// </summary>
    /// <param name="privateKeyPath">私钥路径。</param>
    /// <param name="certificatePath">证书输出路径。</param>
    /// <param name="subject">证书主体（如 "/CN=test.example.com"）。</param>
    /// <param name="days">有效期天数。</param>
    /// <param name="subjectAlternativeNames">SAN，如 "DNS:example.com,DNS:www.example.com,IP:192.168.1.1"。</param>
    /// <param name="keyUsage">KeyUsage，如 "critical,digitalSignature,keyEncipherment"。</param>
    /// <param name="extendedKeyUsage">EKU，如 "serverAuth,clientAuth"。</param>
    /// <param name="crlDistributionPoints">CRL 分发点，如 "URI:http://crl.example.com/ca.crl"。</param>
    /// <param name="isCa">是否 CA 证书（BasicConstraints CA:TRUE）。</param>
    /// <param name="includeSki">是否包含 SubjectKeyIdentifier。</param>
    /// <param name="serialHex">十六进制序列号（可选）。</param>
    public static Task<OpenSslResult> GenerateSelfSignedCertificateAsync(
        string privateKeyPath,
        string certificatePath,
        string subject,
        int days = 365,
        string? subjectAlternativeNames = null,
        string? keyUsage = null,
        string? extendedKeyUsage = null,
        string? crlDistributionPoints = null,
        bool isCa = false,
        bool includeSki = false,
        string? serialHex = null,
        CancellationToken cancellationToken = default)
    {
        var configPath = certificatePath + ".cnf";
        var config = BuildReqConfig(
            subjectAlternativeNames,
            keyUsage,
            extendedKeyUsage,
            crlDistributionPoints,
            isCa,
            includeSki);
        File.WriteAllText(configPath, config);

        var args = $"req -x509 -new -key \"{privateKeyPath}\" -out \"{certificatePath}\" " +
                   $"-days {days} -subj \"{subject}\" -config \"{configPath}\"";
        if (!string.IsNullOrEmpty(serialHex))
        {
            // 统一使用十六进制序列号（数值开头时需 0x 前缀，否则 openssl 按十进制解释）。
            args += serialHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? $" -set_serial {serialHex}"
                : $" -set_serial 0x{serialHex}";
        }

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

    #endregion

    #region CSR

    /// <summary>
    /// 使用现有私钥生成 CSR（可含 SAN / KU / EKU 扩展）。
    /// </summary>
    /// <param name="privateKeyPath">私钥路径。</param>
    /// <param name="csrPath">CSR 输出路径。</param>
    /// <param name="subject">CSR 主体（如 "/CN=test.example.com"）。</param>
    /// <param name="subjectAlternativeNames">SAN，如 "DNS:example.com,DNS:www.example.com,IP:192.168.1.1"。</param>
    /// <param name="keyUsage">KeyUsage，如 "critical,digitalSignature,keyEncipherment"。</param>
    /// <param name="extendedKeyUsage">EKU，如 "serverAuth,clientAuth"。</param>
    public static Task<OpenSslResult> GenerateCsrAsync(
        string privateKeyPath,
        string csrPath,
        string subject,
        string? subjectAlternativeNames = null,
        string? keyUsage = null,
        string? extendedKeyUsage = null,
        CancellationToken cancellationToken = default)
    {
        var configPath = csrPath + ".cnf";
        var config = BuildReqConfig(
            subjectAlternativeNames,
            keyUsage,
            extendedKeyUsage,
            crlDistributionPoints: null,
            isCa: false,
            includeSki: false);
        File.WriteAllText(configPath, config);

        var args = $"req -new -key \"{privateKeyPath}\" -out \"{csrPath}\" -subj \"{subject}\" -config \"{configPath}\"";
        return ExecuteAsync(args, cancellationToken: cancellationToken);
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

    #region CRL

    /// <summary>
    /// 在指定目录中初始化一个最小 CA 数据库（index.txt / serial / newcerts / ca 配置），供 <see cref="GenerateCrlAsync"/> 使用。
    /// </summary>
    /// <param name="caDir">CA 目录（会被创建）。</param>
    /// <param name="caKeyPath">CA 私钥路径。</param>
    /// <param name="caCertPath">CA 证书路径。</param>
    /// <param name="configPath">生成的 ca 配置文件路径。</param>
    /// <param name="revokedSerialHex">预置的已吊销证书序列号（十六进制，可选）。</param>
    public static async Task SetupCaDatabaseAsync(
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
            default_md = sha256
            default_days = 365
            default_crl_days = 30
            policy = policy_any
            x509_extensions = usr_cert

            [ policy_any ]
            commonName = supplied

            [ usr_cert ]
            basicConstraints = CA:FALSE
            keyUsage = critical,digitalSignature,keyEncipherment

            """;
        await File.WriteAllTextAsync(configPath, config, cancellationToken);
    }

    /// <summary>
    /// 使用已初始化的 CA 数据库生成 CRL（openssl ca -gencrl）。
    /// </summary>
    public static Task<OpenSslResult> GenerateCrlAsync(
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

    #region PFX

    /// <summary>
    /// 导出 PKCS#12 (PFX) 文件。
    /// </summary>
    /// <param name="privateKeyPath">私钥路径。</param>
    /// <param name="certificatePath">证书路径。</param>
    /// <param name="pfxPath">PFX 输出路径。</param>
    /// <param name="password">PFX 密码。</param>
    /// <param name="chainPath">证书链文件路径（可选）。</param>
    /// <param name="friendlyName">友好名称（可选）。</param>
    public static Task<OpenSslResult> ExportPfxAsync(
        string privateKeyPath,
        string certificatePath,
        string pfxPath,
        string password,
        string? chainPath = null,
        string? friendlyName = null,
        CancellationToken cancellationToken = default)
    {
        var args = $"pkcs12 -export -out \"{pfxPath}\" -inkey \"{privateKeyPath}\" -in \"{certificatePath}\" -passout pass:{password}";
        if (!string.IsNullOrEmpty(chainPath))
        {
            args += $" -certfile \"{chainPath}\"";
        }
        if (!string.IsNullOrEmpty(friendlyName))
        {
            args += $" -name \"{friendlyName}\"";
        }
        return ExecuteAsync(args, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 查看 PFX 文件信息（pkcs12 -info）。
    /// </summary>
    public static Task<OpenSslResult> Pkcs12InfoAsync(
        string pfxPath,
        string password,
        CancellationToken cancellationToken = default)
        => ExecuteAsync($"pkcs12 -in \"{pfxPath}\" -info -noout -passin pass:{password}", cancellationToken: cancellationToken);

    #endregion

    #region 加解密 / 签名互操作

    /// <summary>
    /// 使用公钥加密数据（pkeyutl -encrypt）。
    /// </summary>
    /// <param name="inputPath">明文文件。</param>
    /// <param name="outputPath">密文输出。</param>
    /// <param name="publicKeyPath">公钥。</param>
    /// <param name="useOaep">是否使用 OAEP-SHA256（false 使用 PKCS#1 v1.5）。</param>
    public static Task<OpenSslResult> EncryptAsync(
        string inputPath,
        string outputPath,
        string publicKeyPath,
        bool useOaep = true,
        CancellationToken cancellationToken = default)
    {
        var padding = useOaep
            ? "-pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:sha256"
            : "-pkeyopt rsa_padding_mode:pkcs1";
        return ExecuteAsync(
            $"pkeyutl -encrypt -in \"{inputPath}\" -out \"{outputPath}\" -pubin -inkey \"{publicKeyPath}\" {padding}",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 使用私钥解密数据（pkeyutl -decrypt）。
    /// </summary>
    public static Task<OpenSslResult> DecryptAsync(
        string inputPath,
        string outputPath,
        string privateKeyPath,
        bool useOaep = true,
        CancellationToken cancellationToken = default)
    {
        var padding = useOaep
            ? "-pkeyopt rsa_padding_mode:oaep -pkeyopt rsa_oaep_md:sha256"
            : "-pkeyopt rsa_padding_mode:pkcs1";
        return ExecuteAsync(
            $"pkeyutl -decrypt -in \"{inputPath}\" -out \"{outputPath}\" -inkey \"{privateKeyPath}\" {padding}",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 使用私钥签名（dgst -sign）。
    /// </summary>
    /// <param name="dataPath">待签名数据文件。</param>
    /// <param name="privateKeyPath">私钥。</param>
    /// <param name="signaturePath">签名输出。</param>
    /// <param name="hashAlgorithm">哈希算法，如 SHA-256。</param>
    /// <param name="usePss">是否使用 RSA-PSS（false 使用 PKCS#1 v1.5）。</param>
    public static Task<OpenSslResult> SignDataAsync(
        string dataPath,
        string privateKeyPath,
        string signaturePath,
        string hashAlgorithm = "SHA-256",
        bool usePss = true,
        CancellationToken cancellationToken = default)
    {
        var hash = hashAlgorithm.ToLowerInvariant().Replace("-", string.Empty);
        // PSS 显式指定 salt 长度为摘要长度，与代码侧（RFC 4055 默认）保持一致，确保双向互操作。
        var sigopt = usePss ? " -sigopt rsa_padding_mode:pss -sigopt rsa_pss_saltlen:digest" : "";
        return ExecuteAsync(
            $"dgst -{hash} -sign \"{privateKeyPath}\"{sigopt} -out \"{signaturePath}\" \"{dataPath}\"",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 使用公钥验证签名（dgst -verify）。
    /// </summary>
    public static Task<OpenSslResult> VerifyDataAsync(
        string dataPath,
        string signaturePath,
        string publicKeyPath,
        string hashAlgorithm = "SHA-256",
        bool usePss = true,
        CancellationToken cancellationToken = default)
    {
        var hash = hashAlgorithm.ToLowerInvariant().Replace("-", string.Empty);
        var sigopt = usePss ? " -sigopt rsa_padding_mode:pss" : "";
        return ExecuteAsync(
            $"dgst -{hash} -verify \"{publicKeyPath}\"{sigopt} -signature \"{signaturePath}\" \"{dataPath}\"",
            cancellationToken: cancellationToken);
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

    #region 私有工具

    private static string BuildReqConfig(
        string? subjectAlternativeNames,
        string? keyUsage,
        string? extendedKeyUsage,
        string? crlDistributionPoints,
        bool isCa,
        bool includeSki)
    {
        var extensions = new List<string>();

        if (isCa)
        {
            extensions.Add("basicConstraints = critical,CA:TRUE,pathlen:0");
        }
        if (includeSki)
        {
            extensions.Add("subjectKeyIdentifier = hash");
        }
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
        if (!string.IsNullOrEmpty(crlDistributionPoints))
        {
            extensions.Add($"crlDistributionPoints = {crlDistributionPoints}");
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
