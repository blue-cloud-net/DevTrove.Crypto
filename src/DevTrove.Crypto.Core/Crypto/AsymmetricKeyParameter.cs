namespace DevTrove.Crypto.Crypto;

/// <summary>
/// 非对称密钥参数基类
/// </summary>
/// <remarks>
/// <para>
/// 非对称加密使用一对密钥（公钥和私钥），公钥用于加密和验证签名，私钥用于解密和生成签名。
/// 支持 RSA、EC（椭圆曲线）、SM2（商密）、DSA 等算法。
/// </para>
/// <para>
/// RFC 标准参考：
/// <list type="bullet">
/// <item><description>公钥格式：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7 (SubjectPublicKeyInfo)</see></description></item>
/// <item><description>私钥格式：<see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208 (PKCS#8)</see></description></item>
/// <item><description>RSA 算法：<see href="https://datatracker.ietf.org/doc/html/rfc8017">RFC 8017 (PKCS#1 v2.2)</see></description></item>
/// <item><description>EC 算法：<see href="https://datatracker.ietf.org/doc/html/rfc5480">RFC 5480 (Elliptic Curve Cryptography Subject Public Key Information)</see></description></item>
/// <item><description>DSA 算法：<see href="https://datatracker.ietf.org/doc/html/rfc3279#section-2.3.2">RFC 3279 Section 2.3.2</see></description></item>
/// <item><description>SM2 商密算法：<see href="https://www.oscca.gov.cn/sca/xxgk/2010-12/17/1002386/files/b791a9f908bb4803875ab6aeeb7b4e03.pdf">GM/T 0003.5-2012</see></description></item>
/// </list>
/// </para>
/// </remarks>
public abstract class AsymmetricKeyParameter
{
    protected readonly Org.BouncyCastle.Crypto.AsymmetricKeyParameter _key;

    /// <summary>
    /// 使用 BouncyCastle 密钥对象初始化 <see cref="AsymmetricKeyParameter"/> 类的新实例
    /// </summary>
    /// <param name="key">BouncyCastle 密钥对象</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 null 时抛出</exception>
    protected AsymmetricKeyParameter(
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter key)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>
    /// 获取密钥算法名称
    /// </summary>
    /// <value>
    /// 识别密钥所使用的算法类型，如 RSA、EC、SM2、DSA 等
    /// </value>
    /// <remarks>
    /// 算法标识符参考：
    /// <list type="bullet">
    /// <item><description>RSA: <see href="https://datatracker.ietf.org/doc/html/rfc8017#appendix-A.1">RFC 8017 Appendix A.1</see></description></item>
    /// <item><description>EC: <see href="https://datatracker.ietf.org/doc/html/rfc5480#section-2">RFC 5480 Section 2</see></description></item>
    /// <item><description>DSA: <see href="https://datatracker.ietf.org/doc/html/rfc3279#section-2.3.2">RFC 3279 Section 2.3.2</see></description></item>
    /// </list>
    /// </remarks>
    public string AlgorithmName
    {
        get
        {
            if (_key is RsaKeyParameters)
                return "RSA";
            else if (_key is ECKeyParameters ecKey)
            {
                // SM2
                if (GMObjectIdentifiers.sm2p256v1.Equals(ecKey.PublicKeyParamSet))
                {
                    return "SM2";
                }

                return "EC";
            }
            else if (_key is DsaKeyParameters)
                return "DSA";
            else
                return _key.GetType().Name.Replace("KeyParameters", String.Empty);
        }
    }

    /// <summary>
    /// 获取一个值，指示当前密钥是否为私钥
    /// </summary>
    /// <value>如果是私钥返回 <c>true</c>；否则返回 <c>false</c></value>
    /// <remarks>
    /// 私钥格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208 (PKCS#8 PrivateKeyInfo)</see>
    /// </remarks>
    public bool IsPrivate => _key.IsPrivate;

    /// <summary>
    /// 获取一个值，指示当前密钥是否为公钥
    /// </summary>
    /// <value>如果是公钥返回 <c>true</c>；否则返回 <c>false</c></value>
    /// <remarks>
    /// 公钥格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7 (SubjectPublicKeyInfo)</see>
    /// </remarks>
    public bool IsPublic => !_key.IsPrivate;

    /// <summary>
    /// 获取 RSA 密钥大小（仅对 RSA 密钥有效）
    /// </summary>
    /// <value>
    /// 表示 RSA 模数的位长度，常见值为 2048、3072、4096 位；
    /// 对于非 RSA 密钥返回 <c>null</c>
    /// </value>
    /// <remarks>
    /// <para>密钥越长安全性越高，但性能开销也越大。</para>
    /// <para>
    /// 密钥长度建议参考：
    /// <list type="bullet">
    /// <item><description>RSA 密钥：<see href="https://datatracker.ietf.org/doc/html/rfc8017#section-3.1">RFC 8017 Section 3.1</see></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public int? KeySize
    {
        get
        {
            if (_key is RsaKeyParameters rsaKey)
                return rsaKey.Modulus.BitLength;
            return null;
        }
    }

    /// <summary>
    /// 获取椭圆曲线的 OID（对象标识符）
    /// </summary>
    /// <value>
    /// 椭圆曲线的 ASN.1 对象标识符；对于非 EC 密钥返回 <c>null</c>
    /// </value>
    /// <remarks>
    /// 曲线 OID 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5480#section-2.1.1">RFC 5480 Section 2.1.1 (Named Curve)</see>
    /// </remarks>
    public DerObjectIdentifier? CurveOid
    {
        get
        {
            if (_key is ECKeyParameters ecKey)
                return ecKey.PublicKeyParamSet;
            return null;
        }
    }

    /// <summary>
    /// 获取椭圆曲线名称（仅对 EC 密钥有效）
    /// </summary>
    /// <value>
    /// 椭圆曲线的标识名称，如 secp256r1（P-256）、secp384r1（P-384）、sm2p256v1（SM2）等；
    /// 对于非 EC 密钥返回 <c>null</c>
    /// </value>
    /// <remarks>
    /// <para>不同的曲线提供不同级别的安全强度。</para>
    /// <para>
    /// 标准曲线参考：
    /// <list type="bullet">
    /// <item><description>NIST 曲线：<see href="https://datatracker.ietf.org/doc/html/rfc5480#section-2.1.1.1">RFC 5480 Section 2.1.1.1</see></description></item>
    /// <item><description>SEC 曲线：<see href="https://www.secg.org/sec2-v2.pdf">SEC 2: Recommended Elliptic Curve Domain Parameters</see></description></item>
    /// <item><description>商密 SM2 曲线：<see href="https://www.oscca.gov.cn/sca/xxgk/2010-12/17/1002386/files/b791a9f908bb4803875ab6aeeb7b4e03.pdf">GM/T 0003.5-2012</see></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? CurveName
    {
        get
        {
            if (_key is ECKeyParameters ecKey
                && ecKey.Parameters is not null)
            {
                return ECNamedCurveTable.GetName(ecKey.PublicKeyParamSet);
            }

            return null;
        }
    }

    /// <summary>
    /// 获取密钥的算法参数字典
    /// </summary>
    /// <value>
    /// 包含密钥的关键参数信息，如 RSA 的 n（模数）和 e（公钥指数），EC 的坐标点等。
    /// 参数值采用 Base64 编码表示
    /// </value>
    /// <remarks>
    /// <para>
    /// 各算法参数说明：
    /// <list type="bullet">
    /// <item><description>RSA 参数 (N, E, P, Q, DP, DQ, QInv)：<see href="https://datatracker.ietf.org/doc/html/rfc8017#appendix-A.1">RFC 8017 Appendix A.1</see></description></item>
    /// <item><description>EC 参数 (Curve, X, Y, D)：<see href="https://datatracker.ietf.org/doc/html/rfc5480#section-2.1.1">RFC 5480 Section 2.1.1</see></description></item>
    /// <item><description>DSA 参数 (P, Q, G, Y, X)：<see href="https://datatracker.ietf.org/doc/html/rfc3279#section-2.3.2">RFC 3279 Section 2.3.2</see></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public Dictionary<string, string> Parameters
    {
        get
        {
            var parameters = new Dictionary<string, string>();

            if (_key is RsaKeyParameters rsaKey)
            {
                parameters["N"] = Convert.ToBase64String(rsaKey.Modulus.ToByteArrayUnsigned());
                parameters["E"] = Convert.ToBase64String(rsaKey.Exponent.ToByteArrayUnsigned());

                if (_key is RsaPrivateCrtKeyParameters rsaPrivateKey)
                {
                    parameters["P"] = Convert.ToBase64String(rsaPrivateKey.P.ToByteArrayUnsigned());
                    parameters["Q"] = Convert.ToBase64String(rsaPrivateKey.Q.ToByteArrayUnsigned());
                    parameters["DP"] = Convert.ToBase64String(rsaPrivateKey.DP.ToByteArrayUnsigned());
                    parameters["DQ"] = Convert.ToBase64String(rsaPrivateKey.DQ.ToByteArrayUnsigned());
                    parameters["QInv"] = Convert.ToBase64String(rsaPrivateKey.QInv.ToByteArrayUnsigned());
                }
            }
            else if (_key is ECKeyParameters ecKey)
            {
                if (ecKey.PublicKeyParamSet != null)
                {
                    parameters["Curve"] = ecKey.PublicKeyParamSet.Id;
                }

                // 曲线参数
                if (ecKey.Parameters?.Curve != null)
                {
                    var curve = ecKey.Parameters.Curve;

                    // 曲线系数 a 和 b
                    parameters["Curve-A"] = Convert.ToBase64String(curve.A.ToBigInteger().ToByteArrayUnsigned());
                    parameters["Curve-B"] = Convert.ToBase64String(curve.B.ToBigInteger().ToByteArrayUnsigned());

                    // 基点 G 的坐标
                    var g = ecKey.Parameters.G.Normalize();
                    parameters["Curve-Gx"] =
                        Convert.ToBase64String(g.AffineXCoord.ToBigInteger().ToByteArrayUnsigned());
                    parameters["Curve-Gy"] =
                        Convert.ToBase64String(g.AffineYCoord.ToBigInteger().ToByteArrayUnsigned());

                    // 阶 n 和余因子 h
                    // Order
                    parameters["Curve-N"] = Convert.ToBase64String(ecKey.Parameters.N.ToByteArrayUnsigned());
                    if (ecKey.Parameters.H != null)
                    {
                        // Cofactor
                        parameters["Curve-H"] = Convert.ToBase64String(ecKey.Parameters.H.ToByteArrayUnsigned());
                    }

                    // 素数域的模数 p
                    if (curve is Org.BouncyCastle.Math.EC.FpCurve fpCurve)
                    {
                        // Prime
                        parameters["Curve-Q"] = Convert.ToBase64String(fpCurve.Q.ToByteArrayUnsigned());
                    }
                    // 二进制域的参数 m
                    else if (curve is Org.BouncyCastle.Math.EC.F2mCurve f2mCurve)
                    {
                        parameters["Curve-M"] = f2mCurve.M.ToString();
                    }
                }

                if (_key is ECPublicKeyParameters ecPublicKey)
                {
                    parameters["X"] =
                        Convert.ToBase64String(ecPublicKey.Q.AffineXCoord.ToBigInteger().ToByteArrayUnsigned());
                    parameters["Y"] =
                        Convert.ToBase64String(ecPublicKey.Q.AffineYCoord.ToBigInteger().ToByteArrayUnsigned());
                }
                else if (_key is ECPrivateKeyParameters ecPrivateKey)
                {
                    parameters["D"] = Convert.ToBase64String(ecPrivateKey.D.ToByteArrayUnsigned());

                    // 计算公钥坐标
                    var publicPoint = ecPrivateKey.Parameters.G.Multiply(ecPrivateKey.D).Normalize();
                    parameters["X"] =
                        Convert.ToBase64String(publicPoint.AffineXCoord.ToBigInteger().ToByteArrayUnsigned());
                    parameters["Y"] =
                        Convert.ToBase64String(publicPoint.AffineYCoord.ToBigInteger().ToByteArrayUnsigned());
                }
            }
            else if (_key is DsaKeyParameters dsaKey)
            {
                parameters["P"] = Convert.ToBase64String(dsaKey.Parameters.P.ToByteArrayUnsigned());
                parameters["Q"] = Convert.ToBase64String(dsaKey.Parameters.Q.ToByteArrayUnsigned());
                parameters["G"] = Convert.ToBase64String(dsaKey.Parameters.G.ToByteArrayUnsigned());

                if (_key is DsaPublicKeyParameters dsaPublicKey)
                {
                    parameters["Y"] = Convert.ToBase64String(dsaPublicKey.Y.ToByteArrayUnsigned());
                }
                else if (_key is DsaPrivateKeyParameters dsaPrivateKey)
                {
                    parameters["X"] = Convert.ToBase64String(dsaPrivateKey.X.ToByteArrayUnsigned());
                }
            }

            return parameters;
        }
    }

    /// <summary>
    /// 获取密钥的 SHA-256 指纹
    /// </summary>
    /// <value>
    /// 密钥的 SHA-256 哈希值，采用十六进制字符串表示（冒号分隔），用于唯一标识密钥
    /// </value>
    /// <remarks>
    /// 指纹计算基于 DER 编码的密钥数据。SHA-256 算法参考 <see href="https://datatracker.ietf.org/doc/html/rfc6234">RFC 6234</see>
    /// </remarks>
    public string Sha256Fingerprint => this.ComputeFingerprint("SHA-256");

    /// <summary>
    /// 获取底层 BouncyCastle 密钥对象
    /// </summary>
    /// <returns>BouncyCastle <see cref="Org.BouncyCastle.Crypto.AsymmetricKeyParameter"/> 对象</returns>
    public Org.BouncyCastle.Crypto.AsymmetricKeyParameter GetBouncyCastleKey() => _key;

    /// <summary>
    /// 将密钥导出为 PEM 格式
    /// </summary>
    /// <returns>PEM 格式的密钥字符串</returns>
    /// <remarks>
    /// PEM 格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc7468">RFC 7468 (Textual Encodings of PKIX, PKCS, and CMS Structures)</see>
    /// </remarks>
    public virtual string ToPem()
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);

        pemWriter.WriteObject(_key);
        pemWriter.Writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// 将密钥导出为 DER 格式
    /// </summary>
    /// <returns>DER 格式的密钥字节数组</returns>
    /// <remarks>
    /// DER 编码参考 <see href="https://www.itu.int/rec/T-REC-X.690">ITU-T X.690 (ASN.1 encoding rules: BER, CER and DER)</see>
    /// </remarks>
    public abstract byte[] ToDer();

    /// <summary>
    /// 计算证书指纹（Fingerprint）
    /// 证书指纹是证书内容的哈希摘要，用于唯一标识证书。
    /// 常用于证书比对、证书固定（Certificate Pinning）等安全场景。
    /// 支持多种哈希算法：SHA-1（不推荐，用于兼容）、SHA-256（推荐）、SHA-384、SHA-512、MD5、SM3。
    /// </summary>
    /// <param name="algorithm">哈希算法名称，支持 SHA-1、SHA-256、SHA-384、SHA-512、MD5、SM3，默认为 SHA-256</param>
    /// <param name="format">是否格式化为冒号分隔格式，默认为 false</param>
    /// <returns>证书指纹字符串，格式化时为 "XX:XX:XX:..."，否则为连续十六进制字符串</returns>
    public string ComputeFingerprint(string algorithm = "SHA-256", bool format = false)
    {
        var derBytes = this.ToDer();
        return FingerprintHelper.ComputeFingerprint(derBytes, algorithm, format);
    }

    /// <summary>
    /// 返回表示当前密钥的字符串
    /// </summary>
    /// <returns>包含算法、密钥类型和大小的描述性字符串</returns>
    public override string ToString()
    {
        var keyType = this.IsPrivate ? "Private Key" : "Public Key";
        var keySize = this.KeySize.HasValue ? $" ({this.KeySize} bits)" : "";
        return $"{this.AlgorithmName} {keyType}{keySize}";
    }

    /// <summary>
    /// 从 BouncyCastle 密钥对象创建 <see cref="AsymmetricKeyParameter"/> 实例
    /// </summary>
    /// <param name="key">BouncyCastle 密钥对象</param>
    /// <returns>
    /// 如果是私钥，返回 <see cref="AsymmetricPrivateKeyParameter"/>；
    /// 如果是公钥，返回 <see cref="AsymmetricPublicKeyParameter"/>
    /// </returns>
    public static AsymmetricKeyParameter FromBouncyCastleKey(
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter key)
    {
        if (key.IsPrivate)
        {
            return new AsymmetricPrivateKeyParameter(key);
        }
        else
        {
            return new AsymmetricPublicKeyParameter(key);
        }
    }

    /// <summary>
    /// 从 PEM 格式字符串解析密钥
    /// </summary>
    /// <param name="pem">PEM 格式的密钥字符串</param>
    /// <param name="password">可选的解密密码，用于加密的私钥</param>
    /// <returns>解析后的 <see cref="AsymmetricKeyParameter"/> 对象</returns>
    /// <exception cref="InvalidOperationException">当 PEM 格式无效或无法解析时抛出</exception>
    /// <remarks>
    /// PEM 格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc7468">RFC 7468</see>，
    /// 加密私钥格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc1423">RFC 1423 (PEM-based encryption)</see>
    /// </remarks>
    public static AsymmetricKeyParameter FromPem(
        string pem, string? password = null)
    {
        using var reader = new StringReader(pem);
        var pemReader = password != null
            ? new PemReader(reader, new PasswordFinder(password))
            : new PemReader(reader);

        var obj = pemReader.ReadObject();

        var key = obj switch
        {
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair => keyPair.Private,
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter keyParam => keyParam,
            _ => throw new InvalidOperationException("Unable to parse the key from PEM.")
        };

        if (key == null)
            throw new InvalidOperationException("The PEM does not contain a valid key.");

        return FromBouncyCastleKey(key);
    }

    /// <summary>
    /// 从 DER 格式字节数组解析密钥
    /// </summary>
    /// <param name="der">DER 格式的密钥字节数组</param>
    /// <returns>解析后的 <see cref="AsymmetricKeyParameter"/> 对象</returns>
    /// <exception cref="InvalidOperationException">当无法解析 DER 格式时抛出</exception>
    /// <remarks>
    /// <para>自动识别并解析私钥或公钥：</para>
    /// <list type="bullet">
    /// <item><description>私钥格式：<see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208 (PKCS#8 PrivateKeyInfo)</see></description></item>
    /// <item><description>公钥格式：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7 (SubjectPublicKeyInfo)</see></description></item>
    /// </list>
    /// </remarks>
    public static AsymmetricKeyParameter FromDer(
        byte[] der)
    {
        // 尝试解析为私钥 (PKCS#8 PrivateKeyInfo)
        try
        {
            var key = Org.BouncyCastle.Security.PrivateKeyFactory.CreateKey(der);
            return FromBouncyCastleKey(key);
        }
        catch
        {
            // 如果不是私钥，继续尝试公钥
        }

        // 尝试解析为公钥 (SubjectPublicKeyInfo)
        try
        {
            var key = Org.BouncyCastle.Security.PublicKeyFactory.CreateKey(der);
            return FromBouncyCastleKey(key);
        }
        catch
        {
            // 两种格式都失败
        }

        throw new InvalidOperationException("Unable to parse the key from DER; the format is invalid.");
    }

    /// <summary>
    /// PEM 密码查找器，用于解密加密的 PEM 格式私钥
    /// </summary>
    public class PasswordFinder : IPasswordFinder
    {
        private readonly char[] _password;

        /// <summary>
        /// 初始化 <see cref="PasswordFinder"/> 类的新实例
        /// </summary>
        /// <param name="password">用于解密的密码</param>
        public PasswordFinder(string password)
        {
            _password = password.ToCharArray();
        }

        /// <summary>
        /// 获取密码字符数组
        /// </summary>
        /// <returns>密码的字符数组表示</returns>
        public char[] GetPassword() => _password;
    }
}
