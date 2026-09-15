using Org.BouncyCastle.Asn1.Nist;

namespace DevTrove.Crypto.Crypto;

/// <summary>
/// 非对称私钥参数
/// </summary>
/// <remarks>
/// <para>
/// 私钥是密钥对中的私密部分，必须严格保密。用于解密数据、生成数字签名等操作。
/// 支持 PEM 和 DER 格式导出，可使用密码加密存储以增强安全性。
/// </para>
/// <para>
/// 私钥格式标准参考：
/// <list type="bullet">
/// <item><description>PKCS#8 格式：<see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208 (Private-Key Information Syntax)</see></description></item>
/// <item><description>PKCS#8 加密格式：<see href="https://datatracker.ietf.org/doc/html/rfc5958">RFC 5958 (Asymmetric Key Packages)</see></description></item>
/// <item><description>PKCS#1 RSA 格式：<see href="https://datatracker.ietf.org/doc/html/rfc8017#appendix-A.1.2">RFC 8017 Appendix A.1.2 (RSA Private Key Syntax)</see></description></item>
/// <item><description>SEC1 EC 格式：<see href="https://www.secg.org/sec1-v2.pdf">SEC 1 v2.0 Section C.4 (Elliptic Curve Private Key Structure)</see></description></item>
/// <item><description>PEM 文本编码：<see href="https://datatracker.ietf.org/doc/html/rfc7468">RFC 7468 (Textual Encodings of PKIX, PKCS, and CMS Structures)</see></description></item>
/// <item><description>传统 PEM 加密：<see href="https://datatracker.ietf.org/doc/html/rfc1423">RFC 1423 (Privacy Enhancement for Internet Electronic Mail: Part III)</see></description></item>
/// </list>
/// </para>
/// <para>
/// <strong>安全提示：</strong>私钥应当妥善保管，建议使用加密存储。在生产环境中，考虑使用硬件安全模块（HSM）或密钥管理服务（KMS）。
/// </para>
/// </remarks>
public class AsymmetricPrivateKeyParameter : AsymmetricKeyParameter
{
    /// <summary>
    /// 使用 BouncyCastle 私钥对象初始化 <see cref="AsymmetricPrivateKeyParameter"/> 类的新实例
    /// </summary>
    /// <param name="privateKey">BouncyCastle 私钥对象</param>
    /// <exception cref="ArgumentException">当提供的密钥不是私钥时抛出</exception>
    public AsymmetricPrivateKeyParameter(
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateKey) : base(privateKey)
    {
        if (!privateKey.IsPrivate)
            throw new ArgumentException("The provided key is not a private key.", nameof(privateKey));
    }

    /// <summary>
    /// 从私钥中提取对应的公钥
    /// </summary>
    /// <returns>提取的 <see cref="AsymmetricPublicKeyParameter"/> 公钥对象</returns>
    /// <remarks>
    /// <para>
    /// 此方法从私钥中导出对应的公钥。对于不同的算法：
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>RSA</strong>：从私钥的模数 (N) 和公钥指数 (E) 构造公钥</description></item>
    /// <item><description><strong>EC/SM2</strong>：通过私钥标量 (D) 与基点 (G) 相乘计算公钥点 (Q = D × G)</description></item>
    /// <item><description><strong>DSA</strong>：从私钥的参数 (P, Q, G) 和公钥值 (Y) 构造公钥</description></item>
    /// </list>
    /// <para>
    /// <strong>算法参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>RSA 公钥推导：<see href="https://datatracker.ietf.org/doc/html/rfc8017#section-3.2">RFC 8017 Section 3.2</see></description></item>
    /// <item><description>EC 公钥推导：<see href="https://datatracker.ietf.org/doc/html/rfc5480#section-2">RFC 5480 Section 2</see></description></item>
    /// <item><description>DSA 公钥推导：<see href="https://datatracker.ietf.org/doc/html/rfc3279#section-2.3.2">RFC 3279 Section 2.3.2</see></description></item>
    /// </list>
    /// </remarks>
    public AsymmetricPublicKeyParameter GetPublicKey()
    {
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter publicKey;

        if (_key is RsaPrivateCrtKeyParameters rsaPrivateKey)
        {
            // RSA: 从私钥的模数和公钥指数构造公钥
            publicKey = new RsaKeyParameters(false, rsaPrivateKey.Modulus, rsaPrivateKey.PublicExponent);
        }
        else if (_key is ECPrivateKeyParameters ecPrivateKey)
        {
            // EC/SM2: 计算公钥点 Q = D × G
            var q = ecPrivateKey.Parameters.G.Multiply(ecPrivateKey.D).Normalize();
            publicKey = new ECPublicKeyParameters(ecPrivateKey.AlgorithmName, q, ecPrivateKey.Parameters);
        }
        else if (_key is DsaPrivateKeyParameters dsaPrivateKey)
        {
            // DSA: 计算公钥 Y = G^X mod P
            var y = dsaPrivateKey.Parameters.G.ModPow(dsaPrivateKey.X, dsaPrivateKey.Parameters.P);
            publicKey = new DsaPublicKeyParameters(y, dsaPrivateKey.Parameters);
        }
        else
        {
            throw new NotSupportedException($"Extracting a public key from a private key of type {_key.GetType().Name} is not supported.");
        }

        return new AsymmetricPublicKeyParameter(publicKey);
    }

    /// <summary>
    /// 将私钥导出为 PEM 格式（默认使用传统 PKCS#1/SEC1 格式）
    /// </summary>
    /// <returns>PEM 格式的私钥字符串</returns>
    /// <remarks>
    /// 默认使用传统格式（PKCS#1 for RSA, SEC1 for EC）以保持向后兼容性。
    /// 如需使用标准 PKCS#8 格式，请调用 <see cref="ToPem(bool)"/> 并传入 <c>true</c>。
    /// </remarks>
    public override string ToPem() => this.ToPem(pkcs8: false);

    /// <summary>
    /// 将私钥导出为 PEM 格式
    /// </summary>
    /// <param name="pkcs8">
    /// 是否使用 PKCS#8 标准格式。
    /// <c>true</c> 使用 PKCS#8 封装格式（推荐），
    /// <c>false</c> 使用传统算法特定格式（PKCS#1/SEC1）
    /// </param>
    /// <returns>PEM 格式的私钥字符串</returns>
    /// <remarks>
    /// <para>
    /// <strong>格式说明：</strong>
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>格式</term>
    /// <description>PEM 头部</description>
    /// <description>标准</description>
    /// <description>特点</description>
    /// </listheader>
    /// <item>
    /// <term>PKCS#8</term>
    /// <description><c>-----BEGIN PRIVATE KEY-----</c></description>
    /// <description><see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208</see></description>
    /// <description>统一格式，包含算法标识符，跨平台兼容性好</description>
    /// </item>
    /// <item>
    /// <term>PKCS#1 (RSA)</term>
    /// <description><c>-----BEGIN RSA PRIVATE KEY-----</c></description>
    /// <description><see href="https://datatracker.ietf.org/doc/html/rfc8017#appendix-A.1.2">RFC 8017</see></description>
    /// <description>算法特定格式，体积稍小</description>
    /// </item>
    /// <item>
    /// <term>SEC1 (EC)</term>
    /// <description><c>-----BEGIN EC PRIVATE KEY-----</c></description>
    /// <description><see href="https://www.secg.org/sec1-v2.pdf">SEC 1</see></description>
    /// <description>椭圆曲线专用格式</description>
    /// </item>
    /// </list>
    /// <para>
    /// <strong>推荐使用 PKCS#8 格式</strong>（<paramref name="pkcs8"/> = <c>true</c>），因为它是现代标准，具有更好的互操作性。
    /// </para>
    /// </remarks>
    public string ToPem(bool pkcs8)
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);

        if (pkcs8)
        {
            // 使用标准 PKCS#8 格式
            pemWriter.WriteObject(new Pkcs8Generator(_key));
        }
        else
        {
            // 使用传统 PKCS#1 格式
            pemWriter.WriteObject(_key);
        }
        pemWriter.Writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// 将私钥导出为加密的 PEM 格式（传统 OpenSSL 加密格式）
    /// </summary>
    /// <param name="password">用于加密私钥的密码</param>
    /// <param name="algorithm">
    /// 加密算法，默认 "AES-256-CBC"。
    /// 支持的算法：AES-128-CBC、AES-192-CBC、AES-256-CBC、DES-EDE3-CBC 等
    /// </param>
    /// <returns>加密的 PEM 格式私钥字符串</returns>
    /// <remarks>
    /// <para>
    /// 此方法使用传统 OpenSSL PEM 加密格式（RFC 1423），生成的 PEM 包含加密元数据头部。
    /// </para>
    /// <para>
    /// <strong>输出示例：</strong>
    /// <code>
    /// -----BEGIN RSA PRIVATE KEY-----
    /// Proc-Type: 4,ENCRYPTED
    /// DEK-Info: AES-256-CBC,A1B2C3D4E5F6789ABCDEF0123456789A
    /// 
    /// base64编码的加密私钥数据...
    /// -----END RSA PRIVATE KEY-----
    /// </code>
    /// </para>
    /// <para>
    /// <strong>加密格式参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>传统 PEM 加密：<see href="https://datatracker.ietf.org/doc/html/rfc1423">RFC 1423 (PEM-based Encryption)</see></description></item>
    /// <item><description>AES 加密算法：<see href="https://datatracker.ietf.org/doc/html/rfc3565">RFC 3565 (Use of AES in CMS)</see></description></item>
    /// <item><description>3DES 加密算法：<see href="https://datatracker.ietf.org/doc/html/rfc2898">RFC 2898 (PKCS #5: Password-Based Cryptography)</see></description></item>
    /// </list>
    /// <para>
    /// <strong>安全建议：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>使用强密码（至少 12 位，包含大小写字母、数字和特殊字符）</description></item>
    /// <item><description>推荐使用 AES-256-CBC 以获得更高安全性</description></item>
    /// <item><description>对于新应用，考虑使用 PKCS#8 加密格式（参见 ToPkcs8Encrypted 方法）</description></item>
    /// </list>
    /// </remarks>
    public string ToPemEncrypted(string password, string algorithm = "AES-256-CBC")
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);

        pemWriter.WriteObject(_key, algorithm, password.ToCharArray(), new SecureRandom());
        pemWriter.Writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// 将私钥导出为加密的 PKCS#8 格式（<c>-----BEGIN ENCRYPTED PRIVATE KEY-----</c>）
    /// </summary>
    /// <param name="password">用于加密私钥的密码</param>
    /// <param name="algorithm">
    /// 加密算法，默认 "AES-256-CBC"。
    /// 支持的算法：AES-128-CBC、AES-192-CBC、AES-256-CBC
    /// </param>
    /// <returns>加密的 PKCS#8 格式私钥字符串</returns>
    /// <exception cref="ArgumentException">当 <paramref name="password"/> 为空或算法不支持时抛出</exception>
    /// <remarks>
    /// <para>
    /// 此方法使用标准 PKCS#8 加密封装（RFC 5958 EncryptedPrivateKeyInfo），
    /// 基于 PBES2 + PBKDF2（HMAC-SHA-256）派生密钥并加密私钥数据，与 OpenSSL 互操作。
    /// </para>
    /// <para>
    /// <strong>输出示例：</strong>
    /// <code>
    /// -----BEGIN ENCRYPTED PRIVATE KEY-----
    /// base64编码的加密私钥数据...
    /// -----END ENCRYPTED PRIVATE KEY-----
    /// </code>
    /// </para>
    /// <para>
    /// <strong>格式参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>加密 PKCS#8：<see href="https://datatracker.ietf.org/doc/html/rfc5958">RFC 5958 (Asymmetric Key Packages)</see></description></item>
    /// <item><description>PBES2/PBKDF2 密钥派生：<see href="https://datatracker.ietf.org/doc/html/rfc8018">RFC 8018 (PKCS #5: Password-Based Cryptography)</see></description></item>
    /// </list>
    /// <para>
    /// <strong>安全建议：</strong>使用强密码（至少 12 位，包含大小写字母、数字和特殊字符），推荐 AES-256-CBC。
    /// </para>
    /// </remarks>
    public string ToPkcs8Encrypted(string password, string algorithm = "AES-256-CBC")
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password must not be empty.", nameof(password));

        var cipherOid = algorithm.ToUpperInvariant() switch
        {
            "AES-128-CBC" => NistObjectIdentifiers.IdAes128Cbc,
            "AES-192-CBC" => NistObjectIdentifiers.IdAes192Cbc,
            "AES-256-CBC" => NistObjectIdentifiers.IdAes256Cbc,
            _ => throw new ArgumentException($"Unsupported PKCS#8 encryption algorithm: {algorithm}", nameof(algorithm))
        };

        var random = new SecureRandom();
        var salt = new byte[16];
        random.NextBytes(salt);

        var encryptedInfo = EncryptedPrivateKeyInfoFactory.CreateEncryptedPrivateKeyInfo(
            cipherOid,
            PkcsObjectIdentifiers.IdHmacWithSha256,
            password.ToCharArray(),
            salt,
            iterationCount: 2048,
            random,
            _key);

        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);
        pemWriter.WriteObject(new Org.BouncyCastle.Utilities.IO.Pem.PemObject("ENCRYPTED PRIVATE KEY", encryptedInfo.GetEncoded()));
        pemWriter.Writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// 将私钥导出为 DER 格式（PKCS#8 标准格式）
    /// </summary>
    /// <returns>DER 格式的私钥字节数组</returns>
    /// <remarks>
    /// <para>
    /// 导出的 DER 格式遵循 PKCS#8 标准（RFC 5208），结构为：
    /// <code>
    /// PrivateKeyInfo ::= SEQUENCE {
    ///   version         Version (0),
    ///   algorithm       AlgorithmIdentifier,
    ///   privateKey      OCTET STRING (包含算法特定的私钥数据)
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// DER (Distinguished Encoding Rules) 是 ASN.1 的二进制编码规则，提供唯一确定的编码方式。
    /// </para>
    /// <para>
    /// <strong>格式参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>PKCS#8：<see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208 (Private-Key Information Syntax Specification)</see></description></item>
    /// <item><description>DER 编码：<see href="https://www.itu.int/rec/T-REC-X.690">ITU-T X.690 (ASN.1 encoding rules: BER, CER and DER)</see></description></item>
    /// <item><description>算法标识符：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.1.2">RFC 5280 Section 4.1.1.2</see></description></item>
    /// </list>
    /// </remarks>
    public override byte[] ToDer()
    {
        var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(_key);
        return privateKeyInfo.GetDerEncoded();
    }

    /// <summary>
    /// 从 PEM 格式字符串解析私钥
    /// </summary>
    /// <param name="pem">PEM 格式的私钥字符串，支持 PKCS#8、PKCS#1、SEC1 等格式</param>
    /// <param name="password">可选的解密密码，用于加密的私钥</param>
    /// <returns>解析后的 <see cref="AsymmetricPrivateKeyParameter"/> 对象</returns>
    /// <exception cref="InvalidOperationException">当 PEM 格式无效或不包含私钥时抛出</exception>
    /// <remarks>
    /// <para>
    /// 此方法支持解析多种 PEM 格式的私钥：
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>-----BEGIN PRIVATE KEY-----</c> (PKCS#8 未加密)</description></item>
    /// <item><description><c>-----BEGIN ENCRYPTED PRIVATE KEY-----</c> (PKCS#8 加密)</description></item>
    /// <item><description><c>-----BEGIN RSA PRIVATE KEY-----</c> (PKCS#1 RSA)</description></item>
    /// <item><description><c>-----BEGIN EC PRIVATE KEY-----</c> (SEC1 EC)</description></item>
    /// <item><description><c>-----BEGIN DSA PRIVATE KEY-----</c> (DSA)</description></item>
    /// <item><description>带 Proc-Type 和 DEK-Info 头部的加密 PEM (RFC 1423)</description></item>
    /// </list>
    /// <para>
    /// <strong>格式参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>PEM 编码：<see href="https://datatracker.ietf.org/doc/html/rfc7468">RFC 7468</see></description></item>
    /// <item><description>PKCS#8：<see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208</see> 和 <see href="https://datatracker.ietf.org/doc/html/rfc5958">RFC 5958</see></description></item>
    /// <item><description>传统加密：<see href="https://datatracker.ietf.org/doc/html/rfc1423">RFC 1423</see></description></item>
    /// </list>
    /// </remarks>
    public new static AsymmetricPrivateKeyParameter FromPem(string pem, string? password = null)
    {
        using var reader = new StringReader(pem);
        var pemReader = password != null
            ? new PemReader(reader, new PasswordFinder(password))
            : new PemReader(reader);

        var obj = pemReader.ReadObject();

        var privateKey = obj switch
        {
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair => keyPair.Private,
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter key => key,
            _ => throw new InvalidOperationException("Unable to parse the private key from PEM.")
        };

        if (privateKey == null || !privateKey.IsPrivate)
            throw new InvalidOperationException("The PEM does not contain a valid private key.");

        return new AsymmetricPrivateKeyParameter(privateKey);
    }

    /// <summary>
    /// 从 DER 格式字节数组解析私钥
    /// </summary>
    /// <param name="der">DER 格式的私钥字节数组（PKCS#8 格式）</param>
    /// <returns>解析后的 <see cref="AsymmetricPrivateKeyParameter"/> 对象</returns>
    /// <exception cref="ArgumentException">当 DER 格式无效或无法解析时抛出</exception>
    /// <remarks>
    /// <para>
    /// 此方法期望输入的 DER 数据遵循 PKCS#8 PrivateKeyInfo 结构：
    /// <code>
    /// PrivateKeyInfo ::= SEQUENCE {
    ///   version         INTEGER,
    ///   algorithm       AlgorithmIdentifier,
    ///   privateKey      OCTET STRING
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// DER 编码是 ASN.1 的二进制编码格式，比 PEM 更紧凑，但不可读。
    /// </para>
    /// <para>
    /// <strong>格式参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>PKCS#8：<see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208</see></description></item>
    /// <item><description>DER 编码：<see href="https://www.itu.int/rec/T-REC-X.690">ITU-T X.690</see></description></item>
    /// <item><description>ASN.1：<see href="https://www.itu.int/rec/T-REC-X.680">ITU-T X.680</see></description></item>
    /// </list>
    /// </remarks>
    public new static AsymmetricPrivateKeyParameter FromDer(byte[] der)
    {
        var privateKeyInfo = PrivateKeyInfo.GetInstance(der);
        var privateKey = PrivateKeyFactory.CreateKey(privateKeyInfo);
        return new AsymmetricPrivateKeyParameter(privateKey);
    }
}
