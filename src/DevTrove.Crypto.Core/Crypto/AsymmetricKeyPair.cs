namespace DevTrove.Crypto.Crypto;

/// <summary>
/// 非对称密钥对
/// </summary>
/// <remarks>
/// <para>
/// 包含一对相关联的公钥和私钥，公钥用于加密和验证，私钥用于解密和签名。
/// 支持生成 RSA、EC（包括 SM2）、DSA 等多种算法的密钥对。
/// 密钥对是非对称加密体系的基础，广泛应用于 TLS/SSL、数字签名、身份认证等场景。
/// </para>
/// <para>
/// RFC 标准参考：
/// <list type="bullet">
/// <item><description>密钥对结构：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1">RFC 5280 Section 4.1</see></description></item>
/// <item><description>RSA 密钥对：<see href="https://datatracker.ietf.org/doc/html/rfc8017">RFC 8017 (PKCS#1 v2.2)</see></description></item>
/// <item><description>EC 密钥对：<see href="https://datatracker.ietf.org/doc/html/rfc5480">RFC 5480 (EC Subject Public Key Information)</see></description></item>
/// <item><description>DSA 密钥对：<see href="https://datatracker.ietf.org/doc/html/rfc3279#section-2.3.2">RFC 3279 Section 2.3.2</see></description></item>
/// <item><description>密钥管理：<see href="https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-57pt1r5.pdf">NIST SP 800-57 Part 1</see></description></item>
/// </list>
/// </para>
/// </remarks>
public class AsymmetricKeyPair
{
    private readonly Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair _keyPair;

    /// <summary>
    /// 使用 BouncyCastle 密钥对对象初始化 <see cref="AsymmetricKeyPair"/> 类的新实例
    /// </summary>
    /// <param name="keyPair">BouncyCastle 密钥对对象</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="keyPair"/> 为 null 时抛出</exception>
    public AsymmetricKeyPair(
        Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair)
    {
        _keyPair = keyPair ?? throw new ArgumentNullException(nameof(keyPair));
    }

    /// <summary>
    /// 使用 BouncyCastle 私钥和公钥对象初始化 <see cref="AsymmetricKeyPair"/> 类的新实例
    /// </summary>
    /// <param name="privateKey">BouncyCastle 私钥对象</param>
    /// <param name="publicKey">BouncyCastle 公钥对象</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="privateKey"/> 或 <paramref name="publicKey"/> 为 null 时抛出</exception>
    public AsymmetricKeyPair(
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateKey,
         Org.BouncyCastle.Crypto.AsymmetricKeyParameter publicKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey, nameof(privateKey));
        ArgumentNullException.ThrowIfNull(publicKey, nameof(publicKey));

        _keyPair = new(publicKey, privateKey);
    }

    /// <summary>
    /// 使用自定义私钥和公钥参数初始化 <see cref="AsymmetricKeyPair"/> 类的新实例
    /// </summary>
    /// <param name="privateKey">私钥参数对象</param>
    /// <param name="publicKey">公钥参数对象</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="privateKey"/> 或 <paramref name="publicKey"/> 为 null 时抛出</exception>
    public AsymmetricKeyPair(
        AsymmetricPrivateKeyParameter privateKey,
        AsymmetricPublicKeyParameter publicKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey, nameof(privateKey));
        ArgumentNullException.ThrowIfNull(publicKey, nameof(publicKey));

        _keyPair = new(
            publicKey.GetBouncyCastleKey(),
            privateKey.GetBouncyCastleKey()
        );
    }

    /// <summary>
    /// 获取密钥对中的私钥
    /// </summary>
    /// <value>密钥对的私钥部分</value>
    /// <remarks>
    /// 私钥格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208 (PKCS#8 PrivateKeyInfo)</see>
    /// </remarks>
    public AsymmetricPrivateKeyParameter PrivateKey => new(_keyPair.Private);

    /// <summary>
    /// 获取密钥对中的公钥
    /// </summary>
    /// <value>密钥对的公钥部分</value>
    /// <remarks>
    /// 公钥格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7 (SubjectPublicKeyInfo)</see>
    /// </remarks>
    public AsymmetricPublicKeyParameter PublicKey => new(_keyPair.Public);

    /// <summary>
    /// 获取密钥对的算法名称
    /// </summary>
    /// <value>密钥算法标识，如 RSA、EC、SM2、DSA 等</value>
    public string Algorithm => this.PrivateKey.AlgorithmName;

    /// <summary>
    /// 获取密钥大小（仅对 RSA 密钥有效）
    /// </summary>
    /// <value>RSA 模数的位长度；对于非 RSA 密钥返回 <c>null</c></value>
    /// <remarks>
    /// 密钥长度建议参考 <see href="https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-57pt1r5.pdf">NIST SP 800-57 Part 1</see>
    /// </remarks>
    public int? KeySize => this.PrivateKey.KeySize;

    /// <summary>
    /// 获取底层 BouncyCastle 密钥对对象
    /// </summary>
    /// <returns>BouncyCastle <see cref="Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair"/> 对象</returns>
    public Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair GetBouncyCastleKeyPair() => _keyPair;

    #region 生成密钥对

    /// <summary>
    /// 生成 RSA 密钥对
    /// </summary>
    /// <param name="keySize">密钥大小（位），默认 2048。常用值：2048、3072、4096</param>
    /// <returns>生成的 RSA <see cref="AsymmetricKeyPair"/> 对象</returns>
    /// <remarks>
    /// <para>
    /// RSA 密钥生成算法参考：
    /// <list type="bullet">
    /// <item><description>RSA 规范：<see href="https://datatracker.ietf.org/doc/html/rfc8017#section-3">RFC 8017 Section 3 (Key Types)</see></description></item>
    /// <item><description>密钥长度建议：<see href="https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-57pt1r5.pdf">NIST SP 800-57 Part 1</see></description></item>
    /// </list>
    /// </para>
    /// <para>建议使用至少 2048 位的密钥长度以确保足够的安全性。</para>
    /// </remarks>
    public static AsymmetricKeyPair GenerateRsa(int keySize = 2048)
    {
        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));

        var keyPair = keyPairGenerator.GenerateKeyPair();
        return new(keyPair);
    }

    /// <summary>
    /// 生成椭圆曲线（EC）密钥对
    /// </summary>
    /// <param name="curve">
    /// 曲线名称，默认 "secp256r1"。
    /// 常用曲线：secp256r1 (P-256)、secp384r1 (P-384)、secp521r1 (P-521)
    /// </param>
    /// <returns>生成的 EC <see cref="AsymmetricKeyPair"/> 对象</returns>
    /// <exception cref="ArgumentException">当指定的曲线名称未知时抛出</exception>
    /// <remarks>
    /// <para>
    /// EC 密钥生成算法参考：
    /// <list type="bullet">
    /// <item><description>EC 规范：<see href="https://datatracker.ietf.org/doc/html/rfc5480">RFC 5480 (Elliptic Curve Cryptography Subject Public Key Information)</see></description></item>
    /// <item><description>NIST 曲线：<see href="https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.186-4.pdf">FIPS 186-4 (Digital Signature Standard)</see></description></item>
    /// <item><description>SEC 曲线：<see href="https://www.secg.org/sec2-v2.pdf">SEC 2: Recommended Elliptic Curve Domain Parameters</see></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static AsymmetricKeyPair GenerateEc(string curve = "secp256r1")
    {
        var keyPairGenerator = new ECKeyPairGenerator();
        var curveOid = ECNamedCurveTable.GetOid(curve)
            ?? throw new ArgumentException($"Unknown EC curve: {curve}.", nameof(curve));
        var generatorParameters = new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(
            curveOid, new SecureRandom());
        keyPairGenerator.Init(generatorParameters);

        var keyPair = keyPairGenerator.GenerateKeyPair();
        return new(keyPair);
    }

    /// <summary>
    /// 生成商密 SM2 密钥对
    /// </summary>
    /// <returns>生成的 SM2 <see cref="AsymmetricKeyPair"/> 对象</returns>
    /// <remarks>
    /// <para>
    /// SM2 是中国国家密码管理局发布的椭圆曲线公钥密码算法，基于 256 位椭圆曲线。
    /// </para>
    /// <para>
    /// SM2 算法标准参考：
    /// <list type="bullet">
    /// <item><description>SM2 规范：<see href="https://www.oscca.gov.cn/sca/xxgk/2010-12/17/1002386/files/b791a9f908bb4803875ab6aeeb7b4e03.pdf">GM/T 0003.5-2012 (SM2 椭圆曲线公钥密码算法)</see></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static AsymmetricKeyPair GenerateSm2()
    {
        var keyPairGenerator = new ECKeyPairGenerator();
        var generatorParameters = new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(
            GMObjectIdentifiers.sm2p256v1, new SecureRandom());
        keyPairGenerator.Init(generatorParameters);

        var keyPair = keyPairGenerator.GenerateKeyPair();
        return new(keyPair);
    }

    /// <summary>
    /// 生成 DSA（数字签名算法）密钥对
    /// </summary>
    /// <param name="keySize">密钥大小（位），默认 2048。常用值：2048、3072</param>
    /// <returns>生成的 DSA <see cref="AsymmetricKeyPair"/> 对象</returns>
    /// <remarks>
    /// <para>
    /// DSA 密钥生成算法参考：
    /// <list type="bullet">
    /// <item><description>DSA 规范：<see href="https://datatracker.ietf.org/doc/html/rfc3279#section-2.3.2">RFC 3279 Section 2.3.2</see></description></item>
    /// <item><description>FIPS 标准：<see href="https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.186-4.pdf">FIPS 186-4 (Digital Signature Standard)</see></description></item>
    /// </list>
    /// </para>
    /// <para>注意：DSA 仅用于数字签名，不支持加密操作。</para>
    /// </remarks>
    public static AsymmetricKeyPair GenerateDsa(int keySize = 2048)
    {
        if (keySize is not (1024 or 2048 or 3072))
            throw new ArgumentOutOfRangeException(nameof(keySize), "DSA key size must be 1024, 2048, or 3072 bits.");

        // N 与配套哈希：1024 位用 SHA-1 (160)，2048/3072 位用 SHA-256 (256)。
        var n = keySize switch
        {
            1024 => 160,
            _ => 256,
        };

        IDigest digest = keySize switch
        {
            1024 => new Sha1Digest(),
            _ => new Sha256Digest(),
        };

        var parameterGenerator = new DsaParametersGenerator(digest);
        parameterGenerator.Init(new DsaParameterGenerationParameters(keySize, n, 80, new SecureRandom()));
        var dsaParams = parameterGenerator.GenerateParameters();

        var keyPairGenerator = new DsaKeyPairGenerator();
        keyPairGenerator.Init(new DsaKeyGenerationParameters(new SecureRandom(), dsaParams));

        var keyPair = keyPairGenerator.GenerateKeyPair();
        return new(keyPair);
    }

    #endregion

    /// <summary>
    /// 将私钥导出为 PEM 格式
    /// </summary>
    /// <returns>PEM 格式的私钥字符串</returns>
    /// <remarks>
    /// PEM 格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc7468">RFC 7468 (Textual Encodings of PKIX, PKCS, and CMS Structures)</see>，
    /// 私钥格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc5208">RFC 5208 (PKCS#8)</see>
    /// </remarks>
    public string ExportPrivateKeyPem() => this.PrivateKey.ToPem();

    /// <summary>
    /// 将私钥导出为加密的 PEM 格式
    /// </summary>
    /// <param name="password">用于加密私钥的密码</param>
    /// <param name="algorithm">加密算法，默认 "AES-256-CBC"。支持的算法包括 AES-128-CBC、AES-192-CBC、AES-256-CBC、DES-EDE3-CBC 等</param>
    /// <returns>加密的 PEM 格式私钥字符串</returns>
    /// <remarks>
    /// 加密私钥格式参考：
    /// <list type="bullet">
    /// <item><description>PEM 加密：<see href="https://datatracker.ietf.org/doc/html/rfc1423">RFC 1423 (Privacy Enhancement for Internet Electronic Mail: Part III)</see></description></item>
    /// <item><description>PKCS#8 加密：<see href="https://datatracker.ietf.org/doc/html/rfc5958">RFC 5958 (Asymmetric Key Packages)</see></description></item>
    /// </list>
    /// </remarks>
    public string ExportPrivateKeyPemEncrypted(string password, string algorithm = "AES-256-CBC")
        => this.PrivateKey.ToPemEncrypted(password, algorithm);

    /// <summary>
    /// 将公钥导出为 PEM 格式
    /// </summary>
    /// <returns>PEM 格式的公钥字符串</returns>
    /// <remarks>
    /// PEM 格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc7468">RFC 7468</see>，
    /// 公钥格式参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7 (SubjectPublicKeyInfo)</see>
    /// </remarks>
    public string ExportPublicKeyPem() => this.PublicKey.ToPem();

    /// <summary>
    /// 返回表示当前密钥对的字符串
    /// </summary>
    /// <returns>包含算法、私钥和公钥信息的描述性字符串</returns>
    public override string ToString() => $"{this.Algorithm} Key Pair ({this.PrivateKey}, {this.PublicKey})";
}
