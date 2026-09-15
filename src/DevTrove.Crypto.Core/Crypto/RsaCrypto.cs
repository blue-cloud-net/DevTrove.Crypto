using Org.BouncyCastle.Crypto.Encodings;

namespace DevTrove.Crypto.Crypto;

/// <summary>
/// RSA 非对称算法实现，基于 BouncyCastle。
/// 支持：密钥导入/导出（SPKI/PKCS#8）、加密/解密（OAEP / PKCS#1 v1.5）、签名/验签（PSS / PKCS#1 v1.5）。
/// 安全约定：
/// <list type="bullet">
/// <item>加密默认使用 OAEP-SHA256；如需与遗留系统互操作可选 PKCS#1 v1.5。</item>
/// <item>签名默认使用 RSA-PSS；如需与遗留系统互操作可选 PKCS#1 v1.5。</item>
/// </list>
/// </summary>
public sealed class RsaCrypto : IDisposable
{
    private Org.BouncyCastle.Crypto.AsymmetricKeyParameter? _publicKey;
    private Org.BouncyCastle.Crypto.AsymmetricKeyParameter? _privateKey;
    private bool _disposed;

    // ── 密钥导入 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 从 SubjectPublicKeyInfo (SPKI) DER 字节数组导入公钥。
    /// </summary>
    /// <param name="spkiDer">SPKI DER 编码的公钥字节数组。</param>
    /// <exception cref="CryptographicException">当字节数组不是 RSA 公钥时抛出。</exception>
    public void ImportPublicKey(byte[] spkiDer)
    {
        ArgumentNullException.ThrowIfNull(spkiDer);
        if (spkiDer.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(spkiDer));

        var key = PublicKeyFactory.CreateKey(spkiDer);
        if (key is not RsaKeyParameters { IsPrivate: false })
            throw new CryptographicException("Not an RSA public key.");

        _privateKey = null;
        _publicKey = key;
    }

    /// <summary>
    /// 从 PKCS#8 DER 字节数组导入私钥，并自动提取对应公钥。
    /// </summary>
    /// <param name="pkcs8Der">PKCS#8 DER 编码的私钥字节数组。</param>
    /// <exception cref="CryptographicException">当字节数组不是 RSA 私钥时抛出。</exception>
    public void ImportPrivateKey(byte[] pkcs8Der)
    {
        ArgumentNullException.ThrowIfNull(pkcs8Der);
        if (pkcs8Der.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(pkcs8Der));

        var key = PrivateKeyFactory.CreateKey(pkcs8Der);
        if (key is not RsaPrivateCrtKeyParameters rsaPrivate)
            throw new CryptographicException("Not an RSA private key.");

        _privateKey = rsaPrivate;
        _publicKey = new RsaKeyParameters(false, rsaPrivate.Modulus, rsaPrivate.PublicExponent);
    }

    /// <summary>
    /// 从 <see cref="AsymmetricPublicKeyParameter"/> 导入公钥。
    /// </summary>
    public void ImportPublicKey(AsymmetricPublicKeyParameter publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        var bcKey = publicKey.GetBouncyCastleKey();
        if (bcKey is not RsaKeyParameters { IsPrivate: false })
            throw new CryptographicException("Not an RSA public key.");
        _privateKey = null;
        _publicKey = bcKey;
    }

    /// <summary>
    /// 从 <see cref="AsymmetricPrivateKeyParameter"/> 导入私钥，并自动提取对应公钥。
    /// </summary>
    public void ImportPrivateKey(AsymmetricPrivateKeyParameter privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        var bcKey = privateKey.GetBouncyCastleKey();
        if (bcKey is not RsaPrivateCrtKeyParameters rsaPrivate)
            throw new CryptographicException("Not an RSA private key.");
        _privateKey = rsaPrivate;
        _publicKey = new RsaKeyParameters(false, rsaPrivate.Modulus, rsaPrivate.PublicExponent);
    }

    // ── 加密 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 使用公钥加密数据（默认 OAEP-SHA256）。
    /// </summary>
    /// <param name="plaintext">明文字节数组，长度须小于密钥模数长度减去 OAEP 开销。</param>
    /// <param name="useOaep">
    /// <c>true</c>（默认）使用 OAEP-SHA256；
    /// <c>false</c> 使用 PKCS#1 v1.5（仅用于遗留系统互操作）。
    /// </param>
    /// <returns>密文字节数组。</returns>
    /// <exception cref="CryptographicException">公钥未加载或加密失败时抛出。</exception>
    public byte[] Encrypt(byte[] plaintext, bool useOaep = true)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        if (plaintext.Length == 0)
            throw new ArgumentException("Plaintext cannot be empty.", nameof(plaintext));
        if (_publicKey is null)
            throw new CryptographicException("Public key not available for encryption.");

        var engine = new RsaBlindedEngine();

        IAsymmetricBlockCipher cipher = useOaep
            ? new OaepEncoding(engine, new Sha256Digest())
            : new Pkcs1Encoding(engine);

        cipher.Init(true, new ParametersWithRandom(_publicKey, new SecureRandom()));
        try
        {
            return cipher.ProcessBlock(plaintext, 0, plaintext.Length);
        }
        catch (InvalidCipherTextException ex)
        {
            throw new CryptographicException("RSA encryption failed.", ex);
        }
    }

    /// <summary>
    /// 使用私钥解密数据（默认 OAEP-SHA256）。
    /// </summary>
    /// <param name="ciphertext">密文字节数组。</param>
    /// <param name="useOaep">
    /// <c>true</c>（默认）使用 OAEP-SHA256；
    /// <c>false</c> 使用 PKCS#1 v1.5（仅用于遗留系统互操作）。
    /// </param>
    /// <returns>明文字节数组。</returns>
    /// <exception cref="CryptographicException">私钥未加载或解密失败时抛出。</exception>
    public byte[] Decrypt(byte[] ciphertext, bool useOaep = true)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext cannot be empty.", nameof(ciphertext));
        if (_privateKey is null)
            throw new CryptographicException("Private key not available for decryption.");

        var engine = new RsaBlindedEngine();

        IAsymmetricBlockCipher cipher = useOaep
            ? new OaepEncoding(engine, new Sha256Digest())
            : new Pkcs1Encoding(engine);

        cipher.Init(false, _privateKey);
        try
        {
            return cipher.ProcessBlock(ciphertext, 0, ciphertext.Length);
        }
        catch (InvalidCipherTextException ex)
        {
            throw new CryptographicException("RSA decryption failed. The data may be corrupted or the key is incorrect.", ex);
        }
    }

    // ── 签名 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 使用私钥对数据签名（默认 RSA-PSS with SHA-256）。
    /// </summary>
    /// <param name="data">待签名数据。</param>
    /// <param name="hashAlgorithm">哈希算法名称，支持 SHA-256、SHA-384、SHA-512（默认 SHA-256）。</param>
    /// <param name="usePss">
    /// <c>true</c>（默认）使用 RSA-PSS；
    /// <c>false</c> 使用 PKCS#1 v1.5（仅用于遗留系统互操作）。
    /// </param>
    /// <returns>签名字节数组。</returns>
    /// <exception cref="CryptographicException">私钥未加载或签名失败时抛出。</exception>
    public byte[] SignData(byte[] data, string hashAlgorithm = "SHA-256", bool usePss = true)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(data));
        if (_privateKey is null)
            throw new CryptographicException("Private key not available for signing.");

        var signer = CreateSigner(hashAlgorithm, usePss);
        signer.Init(true, new ParametersWithRandom(_privateKey, new SecureRandom()));
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    /// <summary>
    /// 使用公钥验证签名（默认 RSA-PSS with SHA-256）。
    /// </summary>
    /// <param name="data">原始数据。</param>
    /// <param name="signature">待验证的签名字节数组。</param>
    /// <param name="hashAlgorithm">哈希算法名称，支持 SHA-256、SHA-384、SHA-512（默认 SHA-256）。</param>
    /// <param name="usePss">
    /// <c>true</c>（默认）使用 RSA-PSS；
    /// <c>false</c> 使用 PKCS#1 v1.5（仅用于遗留系统互操作）。
    /// </param>
    /// <returns>签名有效返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <exception cref="CryptographicException">公钥未加载时抛出。</exception>
    public bool VerifyData(byte[] data, byte[] signature, string hashAlgorithm = "SHA-256", bool usePss = true)
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
            var verifier = CreateSigner(hashAlgorithm, usePss);
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

    private static ISigner CreateSigner(string hashAlgorithm, bool usePss)
    {
        IDigest digest = hashAlgorithm.ToUpperInvariant().Replace("-", string.Empty) switch
        {
            "SHA256" => new Sha256Digest(),
            "SHA384" => new Sha384Digest(),
            "SHA512" => new Sha512Digest(),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {hashAlgorithm}.", nameof(hashAlgorithm)),
        };

        if (usePss)
        {
            // RSA-PSS：salt 长度等于摘要长度（推荐）
            return new PssSigner(new RsaBlindedEngine(), digest, digest.GetDigestSize());
        }
        else
        {
            return SignerUtilities.GetSigner(
                hashAlgorithm.ToUpperInvariant().Replace("-", string.Empty) + "withRSA");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        // 归零私钥字节（尽力清理）
        if (_privateKey is RsaPrivateCrtKeyParameters rsa)
        {
            // BouncyCastle BigInteger 不可直接清零，触发 GC 后会回收
            _ = rsa;
        }
        _disposed = true;
    }
}
