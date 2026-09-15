namespace DevTrove.Crypto.Crypto;

/// <summary>
/// DSA 签名/验签实现，基于 BouncyCastle。
/// DSA 仅支持签名/验签操作，不支持加密（算法限制）。
/// 签名编码为 ASN.1 DER 格式（与 OpenSSL 等工具互操作）。
/// </summary>
public sealed class DsaCrypto : IDisposable
{
    private DsaPublicKeyParameters? _publicKey;
    private DsaPrivateKeyParameters? _privateKey;

    // ── 密钥导入 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 从 SubjectPublicKeyInfo (SPKI) DER 字节数组导入 DSA 公钥。
    /// </summary>
    /// <param name="spkiDer">SPKI DER 编码的公钥字节数组。</param>
    /// <exception cref="CryptographicException">当字节数组不是 DSA 公钥时抛出。</exception>
    public void ImportPublicKey(byte[] spkiDer)
    {
        ArgumentNullException.ThrowIfNull(spkiDer);
        if (spkiDer.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(spkiDer));

        var key = PublicKeyFactory.CreateKey(spkiDer);
        if (key is not DsaPublicKeyParameters dsaPub)
            throw new CryptographicException("Not a DSA public key.");

        _privateKey = null;
        _publicKey = dsaPub;
    }

    /// <summary>
    /// 从 PKCS#8 DER 字节数组导入 DSA 私钥。
    /// </summary>
    /// <param name="pkcs8Der">PKCS#8 DER 编码的私钥字节数组。</param>
    /// <exception cref="CryptographicException">当字节数组不是 DSA 私钥时抛出。</exception>
    public void ImportPrivateKey(byte[] pkcs8Der)
    {
        ArgumentNullException.ThrowIfNull(pkcs8Der);
        if (pkcs8Der.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(pkcs8Der));

        var key = PrivateKeyFactory.CreateKey(pkcs8Der);
        if (key is not DsaPrivateKeyParameters dsaPrivate)
            throw new CryptographicException("Not a DSA private key.");

        _privateKey = dsaPrivate;
        // DSA 公钥 = g^x mod p
        var parameters = dsaPrivate.Parameters;
        var y = parameters.G.ModPow(dsaPrivate.X, parameters.P);
        _publicKey = new DsaPublicKeyParameters(y, parameters);
    }

    /// <summary>
    /// 从 <see cref="AsymmetricPublicKeyParameter"/> 导入 DSA 公钥。
    /// </summary>
    public void ImportPublicKey(AsymmetricPublicKeyParameter publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        var bcKey = publicKey.GetBouncyCastleKey();
        if (bcKey is not DsaPublicKeyParameters dsaPub)
            throw new CryptographicException("Not a DSA public key.");
        _privateKey = null;
        _publicKey = dsaPub;
    }

    /// <summary>
    /// 从 <see cref="AsymmetricPrivateKeyParameter"/> 导入 DSA 私钥。
    /// </summary>
    public void ImportPrivateKey(AsymmetricPrivateKeyParameter privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        var bcKey = privateKey.GetBouncyCastleKey();
        if (bcKey is not DsaPrivateKeyParameters dsaPrivate)
            throw new CryptographicException("Not a DSA private key.");
        _privateKey = dsaPrivate;
        var parameters = dsaPrivate.Parameters;
        var y = parameters.G.ModPow(dsaPrivate.X, parameters.P);
        _publicKey = new DsaPublicKeyParameters(y, parameters);
    }

    // ── 签名 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 使用 DSA 私钥对数据签名，返回 ASN.1 DER 编码的签名。
    /// </summary>
    /// <param name="data">待签名数据。</param>
    /// <param name="hashAlgorithm">哈希算法名称，支持 SHA-256、SHA-384、SHA-512（默认 SHA-256）。</param>
    /// <returns>DER 编码的签名字节数组。</returns>
    /// <exception cref="CryptographicException">私钥未加载或签名失败时抛出。</exception>
    public byte[] SignData(byte[] data, string hashAlgorithm = "SHA-256")
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(data));
        if (_privateKey is null)
            throw new CryptographicException("Private key not available for signing.");

        var signer = CreateSigner(hashAlgorithm);
        signer.Init(true, new ParametersWithRandom(_privateKey, new SecureRandom()));
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    /// <summary>
    /// 使用 DSA 公钥验证 DER 编码的签名。
    /// </summary>
    /// <param name="data">原始数据。</param>
    /// <param name="signature">DER 编码的签名字节数组。</param>
    /// <param name="hashAlgorithm">哈希算法名称，支持 SHA-256、SHA-384、SHA-512（默认 SHA-256）。</param>
    /// <returns>签名有效返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <exception cref="CryptographicException">公钥未加载时抛出。</exception>
    public bool VerifyData(byte[] data, byte[] signature, string hashAlgorithm = "SHA-256")
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(data));
        ArgumentNullException.ThrowIfNull(signature);
        if (signature.Length == 0)
            throw new ArgumentException("Signature cannot be empty.", nameof(signature));
        if (_publicKey is null)
            throw new CryptographicException("Public key not available for verification.");

        try
        {
            var verifier = CreateSigner(hashAlgorithm);
            verifier.Init(false, _publicKey);
            verifier.BlockUpdate(data, 0, data.Length);
            return verifier.VerifySignature(signature);
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ── 私有工具 ─────────────────────────────────────────────────────────────

    private static ISigner CreateSigner(string hashAlgorithm)
    {
        IDigest digest = hashAlgorithm.ToUpperInvariant().Replace("-", string.Empty) switch
        {
            "SHA256" => new Sha256Digest(),
            "SHA384" => new Sha384Digest(),
            "SHA512" => new Sha512Digest(),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {hashAlgorithm}.", nameof(hashAlgorithm)),
        };

        // DsaDigestSigner + DsaSigner + DER 编码 = 标准 DSA 签名
        return new DsaDigestSigner(new DsaSigner(), digest, StandardDsaEncoding.Instance);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // 无托管敏感资源需要释放。
    }
}
