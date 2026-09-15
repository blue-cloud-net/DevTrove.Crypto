using System.Runtime.Serialization;

namespace DevTrove.Crypto.Crypto.Sm;

/// <summary>
/// SM2 非对称算法实现，参考 RSA 的 API 风格，基于 BouncyCastle
/// 支持：密钥生成、导入/导出（SPKI/PKCS#8）、签名/验证、加/解密
/// </summary>
public sealed class SM2 : AsymmetricAlgorithm
{
    private const string CurveName = nameof(GMObjectIdentifiers.sm2p256v1);
    private static readonly DerObjectIdentifier CurveOid = GMObjectIdentifiers.sm2p256v1;

    private Org.BouncyCastle.Crypto.AsymmetricKeyParameter? _publicKey;
    private Org.BouncyCastle.Crypto.AsymmetricKeyParameter? _privateKey;

    /// <summary>
    /// SM2 使用的推荐密钥大小（位）
    /// </summary>
    public const int RecommendedKeySize = 256;

    /// <summary>
    /// 构造函数，初始化 KeySize
    /// </summary>
    public SM2()
    {
        LegalKeySizesValue = new[] { new KeySizes(RecommendedKeySize, RecommendedKeySize, 0) };
        KeySizeValue = RecommendedKeySize;
    }

    /// <summary>
    /// 生成新的 SM2 密钥对
    /// </summary>
    public void GenerateKeyPair()
    {
        var keyPairGenerator = new ECKeyPairGenerator();
        var generatorParameters = new ECKeyGenerationParameters(
            CurveOid, new SecureRandom());
        keyPairGenerator.Init(generatorParameters);

        var keyPair = keyPairGenerator.GenerateKeyPair();
        _privateKey = keyPair.Private;
        _publicKey = keyPair.Public;
    }

    /// <summary>
    /// 导出公钥为 SubjectPublicKeyInfo (SPKI) 的 DER 编码
    /// </summary>
    public byte[] ExportPublicKey()
    {
        if (_publicKey is null)
            throw new CryptographicException("Key pair not initialized or public key not imported.");
        var spki = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(_publicKey);
        return spki.GetDerEncoded();
    }

    /// <summary>
    /// 导出私钥为 PrivateKeyInfo (PKCS#8) 的 DER 编码
    /// </summary>
    public byte[] ExportPrivateKey()
    {
        if (_privateKey is null)
            throw new CryptographicException("Key pair not initialized or private key not imported.");
        var pki = PrivateKeyInfoFactory.CreatePrivateKeyInfo(_privateKey);
        return pki.GetDerEncoded();
    }

    /// <summary>
    /// 从 SPKI (DER) 导入公钥
    /// </summary>
    public void ImportPublicKey(byte[] spkiDer)
    {
        if (spkiDer is null)
            throw new ArgumentNullException(nameof(spkiDer));
        if (spkiDer.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(spkiDer));

        var publicKey = PublicKeyFactory.CreateKey(spkiDer);
        if (!(publicKey is ECPublicKeyParameters ecPublicKey
            && ecPublicKey.PublicKeyParamSet.Id == CurveOid.Id))
            throw new CryptographicException("Not an SM2 public key.");

        _privateKey = null;
        _publicKey = publicKey;
    }

    /// <summary>
    /// 从 PKCS#8 (DER) 导入私钥
    /// </summary>
    public void ImportPrivateKey(byte[] pkcs8Der)
    {
        if (pkcs8Der is null)
            throw new ArgumentNullException(nameof(pkcs8Der));
        if (pkcs8Der.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(pkcs8Der));

        var privateKey = PrivateKeyFactory.CreateKey(pkcs8Der);
        if (!(privateKey is ECPrivateKeyParameters ecPrivateKey
            && ecPrivateKey.PublicKeyParamSet.Id == CurveOid.Id))
            throw new CryptographicException("Not an SM2 private key.");


        var q = ecPrivateKey.Parameters.G.Multiply(ecPrivateKey.D).Normalize();
        var publicKey = new ECPublicKeyParameters(ecPrivateKey.AlgorithmName, q, ecPrivateKey.Parameters);

        _privateKey = privateKey;
        _publicKey = publicKey;
    }

    /// <summary>
    /// 使用 SM2 签名（SM2Signer - 使用 SM3 摘要），返回 ASN.1 DER 编码签名
    /// </summary>
    public byte[] SignData(byte[] data, byte[]? userId = null)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        if (data.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(data));
        if (_privateKey is null)
            throw new CryptographicException("Private key not available for signing.");

        var signer = new SM2Signer(StandardDsaEncoding.Instance);
        signer.Init(true, userId is null ? _privateKey : new ParametersWithID(_privateKey, userId));
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    /// <summary>
    /// 验证 SM2 签名（输入签名为 ASN.1 DER 编码）
    /// </summary>
    public bool VerifyData(byte[] data, byte[] signature, byte[]? userId = null)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));
        if (data.Length == 0)
            throw new ArgumentException("Input data cannot be empty.", nameof(data));
        if (signature is null)
            throw new ArgumentNullException(nameof(signature));
        if (signature.Length == 0)
            throw new ArgumentException("Signature cannot be empty.", nameof(signature));
        if (_publicKey is null)
            throw new CryptographicException("Public key not available for verification.");

        var verifier = new SM2Signer(StandardDsaEncoding.Instance);
        verifier.Init(false, userId is null ? _publicKey : new ParametersWithID(_publicKey, userId));
        verifier.BlockUpdate(data, 0, data.Length);
        return verifier.VerifySignature(signature);
    }

    /// <summary>
    /// 使用 SM2Engine 加密（公钥），返回密文 bytes (ASN.1 DER 编码格式)
    /// </summary>
    /// <remarks>
    /// 输出格式符合 GM/T 0009-2012 标准的 ASN.1 DER 编码：
    /// SM2Ciphertext ::= SEQUENCE { C1x INTEGER, C1y INTEGER, C3 OCTET STRING, C2 OCTET STRING }
    /// 使用 C1C3C2 顺序，与 OpenSSL 兼容
    /// </remarks>
    public byte[] Encrypt(byte[] plaintext)
    {
        if (plaintext is null)
            throw new ArgumentNullException(nameof(plaintext));
        if (plaintext.Length == 0)
            throw new ArgumentException("Plaintext cannot be empty.", nameof(plaintext));
        if (_publicKey is null)
            throw new CryptographicException("Public key not available for encryption.");

        var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
        engine.Init(true, _publicKey);
        var rawCiphertext = engine.ProcessBlock(plaintext, 0, plaintext.Length);

        // 将原始格式转换为 ASN.1 DER 编码
        return EncodeToAsn1Der(rawCiphertext);
    }

    /// <summary>
    /// 使用 SM2Engine 解密（私钥），支持 ASN.1 DER 编码格式
    /// </summary>
    /// <remarks>
    /// 支持 GM/T 0009-2012 标准的 ASN.1 DER 编码格式和原始格式
    /// </remarks>
    public byte[] Decrypt(byte[] ciphertext)
    {
        if (ciphertext is null)
            throw new ArgumentNullException(nameof(ciphertext));
        if (ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext cannot be empty.", nameof(ciphertext));
        if (_privateKey is null)
            throw new CryptographicException("Private key not available for decryption.");

        // 尝试解析 ASN.1 DER 格式
        byte[] rawCiphertext;
        try
        {
            rawCiphertext = DecodeFromAsn1Der(ciphertext);
        }
        catch
        {
            // 如果不是 ASN.1 格式，使用原始数据
            rawCiphertext = ciphertext;
        }

        var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
        engine.Init(false, _privateKey);
        return engine.ProcessBlock(rawCiphertext, 0, rawCiphertext.Length);
    }

    /// <summary>
    /// 将原始 SM2 密文编码为 ASN.1 DER 格式
    /// </summary>
    private static byte[] EncodeToAsn1Der(byte[] rawCiphertext)
    {
        // 原始格式 (C1C3C2 模式): C1(65 bytes) || C3(32 bytes for SM3) || C2(variable)
        const int c1Length = 65; // 未压缩点: 0x04 + x(32) + y(32)
        const int c3Length = 32; // SM3 摘要长度

        if (rawCiphertext.Length < c1Length + c3Length)
            throw new ArgumentException("Invalid ciphertext length");

        var c2Length = rawCiphertext.Length - c1Length - c3Length;

        // 解析 C1 点: 0x04 || x(32 bytes) || y(32 bytes)
        if (rawCiphertext[0] != 0x04)
            throw new ArgumentException("Invalid C1 point format");

        var c1x = new byte[32];
        var c1y = new byte[32];
        var c3 = new byte[c3Length];
        var c2 = new byte[c2Length];

        Array.Copy(rawCiphertext, 1, c1x, 0, 32);      // 跳过 0x04 前缀
        Array.Copy(rawCiphertext, 33, c1y, 0, 32);
        Array.Copy(rawCiphertext, c1Length, c3, 0, c3Length);
        Array.Copy(rawCiphertext, c1Length + c3Length, c2, 0, c2Length);

        // 构建 ASN.1 SEQUENCE { C1x INTEGER, C1y INTEGER, C3 OCTET STRING, C2 OCTET STRING }
        var seq = new DerSequence(
            new DerInteger(new Org.BouncyCastle.Math.BigInteger(1, c1x)),
            new DerInteger(new Org.BouncyCastle.Math.BigInteger(1, c1y)),
            new DerOctetString(c3),
            new DerOctetString(c2)
        );

        return seq.GetDerEncoded();
    }

    /// <summary>
    /// 从 ASN.1 DER 格式解码为原始 SM2 密文
    /// </summary>
    private static byte[] DecodeFromAsn1Der(byte[] asn1Ciphertext)
    {
        var seq = Asn1Sequence.GetInstance(asn1Ciphertext);
        if (seq.Count != 4)
            throw new ArgumentException("Invalid ASN.1 SM2 ciphertext structure");

        var c1x = DerInteger.GetInstance(seq[0]).Value.ToByteArrayUnsigned();
        var c1y = DerInteger.GetInstance(seq[1]).Value.ToByteArrayUnsigned();
        var c3 = Asn1OctetString.GetInstance(seq[2]).GetOctets();
        var c2 = Asn1OctetString.GetInstance(seq[3]).GetOctets();

        // 确保 c1x 和 c1y 是 32 字节（如果不足则左填充 0）
        var c1xPadded = new byte[32];
        var c1yPadded = new byte[32];
        Array.Copy(c1x, 0, c1xPadded, 32 - c1x.Length, c1x.Length);
        Array.Copy(c1y, 0, c1yPadded, 32 - c1y.Length, c1y.Length);

        // 重组为原始格式 (C1C3C2 模式): 0x04 || C1x || C1y || C3 || C2
        var result = new byte[1 + 32 + 32 + c3.Length + c2.Length];
        result[0] = 0x04; // 未压缩点标记
        Array.Copy(c1xPadded, 0, result, 1, 32);
        Array.Copy(c1yPadded, 0, result, 33, 32);
        Array.Copy(c3, 0, result, 65, c3.Length);
        Array.Copy(c2, 0, result, 65 + c3.Length, c2.Length);

        return result;
    }

    /// <summary>
    /// 清理敏感数据
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _privateKey = null;
            _publicKey = null;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// 创建默认实现
    /// </summary>
    public new static SM2 Create() => new();
}
