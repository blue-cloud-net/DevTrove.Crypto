using Org.BouncyCastle.Crypto.Agreement;

namespace DevTrove.Crypto.Crypto;

/// <summary>
/// ECDSA 签名/验签实现，基于 BouncyCastle。
/// 签名编码为 ASN.1 DER 格式（标准 DER 编码，与 OpenSSL 等工具互操作）。
/// 同时支持 ECDH 原始共享密钥计算（Diffie-Hellman 密钥协商）。
/// </summary>
public sealed class EcdsaCrypto : IDisposable
{
    private ECPublicKeyParameters? _publicKey;
    private ECPrivateKeyParameters? _privateKey;

    // ── 密钥导入 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 从 SubjectPublicKeyInfo (SPKI) DER 字节数组导入 EC 公钥。
    /// </summary>
    /// <param name="spkiDer">SPKI DER 编码的公钥字节数组。</param>
    /// <exception cref="CryptographicException">当字节数组不是 EC 公钥时抛出。</exception>
    public void ImportPublicKey(byte[] spkiDer)
    {
        ArgumentNullException.ThrowIfNull(spkiDer);
        if (spkiDer.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(spkiDer));

        var key = PublicKeyFactory.CreateKey(spkiDer);
        if (key is not ECPublicKeyParameters ecPub)
            throw new CryptographicException("Not an EC public key.");

        _privateKey = null;
        _publicKey = ecPub;
    }

    /// <summary>
    /// 从 PKCS#8 DER 字节数组导入 EC 私钥，并自动提取对应公钥。
    /// </summary>
    /// <param name="pkcs8Der">PKCS#8 DER 编码的私钥字节数组。</param>
    /// <exception cref="CryptographicException">当字节数组不是 EC 私钥时抛出。</exception>
    public void ImportPrivateKey(byte[] pkcs8Der)
    {
        ArgumentNullException.ThrowIfNull(pkcs8Der);
        if (pkcs8Der.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(pkcs8Der));

        var key = PrivateKeyFactory.CreateKey(pkcs8Der);
        if (key is not ECPrivateKeyParameters ecPrivate)
            throw new CryptographicException("Not an EC private key.");

        _privateKey = ecPrivate;
        _publicKey = ExtractPublicKey(ecPrivate);
    }

    /// <summary>
    /// 从 <see cref="AsymmetricPublicKeyParameter"/> 导入 EC 公钥。
    /// </summary>
    public void ImportPublicKey(AsymmetricPublicKeyParameter publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        var bcKey = publicKey.GetBouncyCastleKey();
        if (bcKey is not ECPublicKeyParameters ecPub)
            throw new CryptographicException("Not an EC public key.");
        _privateKey = null;
        _publicKey = ecPub;
    }

    /// <summary>
    /// 从 <see cref="AsymmetricPrivateKeyParameter"/> 导入 EC 私钥，并自动提取对应公钥。
    /// </summary>
    public void ImportPrivateKey(AsymmetricPrivateKeyParameter privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        var bcKey = privateKey.GetBouncyCastleKey();
        if (bcKey is not ECPrivateKeyParameters ecPrivate)
            throw new CryptographicException("Not an EC private key.");
        _privateKey = ecPrivate;
        _publicKey = ExtractPublicKey(ecPrivate);
    }

    // ── 签名 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 使用 ECDSA 私钥对数据签名，返回 ASN.1 DER 编码的签名。
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
    /// 使用 ECDSA 公钥验证 DER 编码的签名。
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

    // ── ECDH ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 计算 ECDH 原始共享密钥（z 值字节数组，未经 KDF）。
    /// 调用方应使用合适的 KDF（如 HKDF）从共享密钥派生实际加密密钥。
    /// </summary>
    /// <param name="ownPrivateKey">己方 EC 私钥。</param>
    /// <param name="peerPublicKey">对端 EC 公钥（必须在同一曲线上）。</param>
    /// <returns>ECDH 原始共享密钥字节数组（大端序，与曲线字段宽度一致）。</returns>
    /// <exception cref="CryptographicException">密钥不在同一曲线或密钥协商失败时抛出。</exception>
    public static byte[] ComputeSharedSecret(
        AsymmetricPrivateKeyParameter ownPrivateKey,
        AsymmetricPublicKeyParameter peerPublicKey)
    {
        ArgumentNullException.ThrowIfNull(ownPrivateKey);
        ArgumentNullException.ThrowIfNull(peerPublicKey);

        var bcPrivate = ownPrivateKey.GetBouncyCastleKey();
        var bcPublic = peerPublicKey.GetBouncyCastleKey();

        if (bcPrivate is not ECPrivateKeyParameters ecPrivate)
            throw new CryptographicException("Own key is not an EC private key.");
        if (bcPublic is not ECPublicKeyParameters ecPublic)
            throw new CryptographicException("Peer key is not an EC public key.");
        if (!ecPrivate.Parameters.Equals(ecPublic.Parameters))
            throw new CryptographicException("Keys are not on the same EC curve.");

        var agreement = new ECDHBasicAgreement();
        agreement.Init(ecPrivate);
        var sharedSecret = agreement.CalculateAgreement(ecPublic);

        // 返回大端序字节数组，长度与曲线字段宽度一致
        var fieldSize = (ecPrivate.Parameters.Curve.FieldSize + 7) / 8;
        var bytes = sharedSecret.ToByteArrayUnsigned();

        if (bytes.Length == fieldSize)
            return bytes;

        // 补前导零，确保长度一致
        var result = new byte[fieldSize];
        bytes.CopyTo(result, fieldSize - bytes.Length);
        return result;
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

        // DsaDigestSigner + ECDsaSigner + DER 编码 = 标准 ECDSA 签名（与 OpenSSL 互操作）
        return new DsaDigestSigner(new ECDsaSigner(), digest, StandardDsaEncoding.Instance);
    }

    private static ECPublicKeyParameters ExtractPublicKey(ECPrivateKeyParameters privateKey)
    {
        var domainParams = privateKey.Parameters;
        var publicPoint = domainParams.G.Multiply(privateKey.D);
        return new ECPublicKeyParameters("EC", publicPoint.Normalize(), domainParams);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // 无托管敏感资源需要释放。
    }
}
