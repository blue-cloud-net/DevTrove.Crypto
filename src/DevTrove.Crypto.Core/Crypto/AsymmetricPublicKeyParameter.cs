namespace DevTrove.Crypto.Crypto;

/// <summary>
/// 非对称公钥参数
/// </summary>
/// <remarks>
/// <para>
/// 公钥是密钥对中的公开部分，可以自由分发。用于加密数据、验证数字签名等操作。
/// 公钥通常包含在 X.509 证书中，或作为独立的 SubjectPublicKeyInfo 结构分发。
/// </para>
/// <para>
/// 公钥格式标准参考：
/// <list type="bullet">
/// <item><description>SubjectPublicKeyInfo：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7</see></description></item>
/// <item><description>RSA 公钥：<see href="https://datatracker.ietf.org/doc/html/rfc8017#appendix-A.1.1">RFC 8017 Appendix A.1.1 (RSA Public Key Syntax)</see></description></item>
/// <item><description>EC 公钥：<see href="https://datatracker.ietf.org/doc/html/rfc5480#section-2">RFC 5480 Section 2 (Subject Public Key Info Fields)</see></description></item>
/// <item><description>DSA 公钥：<see href="https://datatracker.ietf.org/doc/html/rfc3279#section-2.3.2">RFC 3279 Section 2.3.2</see></description></item>
/// <item><description>PEM 编码：<see href="https://datatracker.ietf.org/doc/html/rfc7468#section-13">RFC 7468 Section 13 (Public Key)</see></description></item>
/// </list>
/// </para>
/// <para>
/// <strong>使用场景：</strong>
/// </para>
/// <list type="bullet">
/// <item><description>加密敏感数据（使用接收方的公钥）</description></item>
/// <item><description>验证数字签名（使用签名方的公钥）</description></item>
/// <item><description>密钥交换协议（如 ECDH）</description></item>
/// <item><description>TLS/SSL 握手过程</description></item>
/// </list>
/// </remarks>
public class AsymmetricPublicKeyParameter : AsymmetricKeyParameter
{
    /// <summary>
    /// 使用 BouncyCastle 公钥对象初始化 <see cref="AsymmetricPublicKeyParameter"/> 类的新实例
    /// </summary>
    /// <param name="publicKey">BouncyCastle 公钥对象</param>
    /// <exception cref="ArgumentException">当提供的密钥不是公钥时抛出</exception>
    public AsymmetricPublicKeyParameter(
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter publicKey) : base(publicKey)
    {
        if (publicKey.IsPrivate)
            throw new ArgumentException("The provided key is not a public key.", nameof(publicKey));
    }

    /// <summary>
    /// 将公钥导出为 DER 格式（SubjectPublicKeyInfo 结构）
    /// </summary>
    /// <returns>DER 格式的公钥字节数组</returns>
    /// <remarks>
    /// <para>
    /// 导出的 DER 格式遵循 SubjectPublicKeyInfo 结构（RFC 5280），定义为：
    /// <code>
    /// SubjectPublicKeyInfo ::= SEQUENCE {
    ///   algorithm         AlgorithmIdentifier,
    ///   subjectPublicKey  BIT STRING
    /// }
    /// 
    /// AlgorithmIdentifier ::= SEQUENCE {
    ///   algorithm   OBJECT IDENTIFIER,
    ///   parameters  ANY DEFINED BY algorithm OPTIONAL
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
    /// <item><description>SubjectPublicKeyInfo：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7</see></description></item>
    /// <item><description>DER 编码：<see href="https://www.itu.int/rec/T-REC-X.690">ITU-T X.690 (ASN.1 encoding rules: BER, CER and DER)</see></description></item>
    /// <item><description>算法标识符：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.1.2">RFC 5280 Section 4.1.1.2</see></description></item>
    /// </list>
    /// </remarks>
    public override byte[] ToDer()
    {
        var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(_key);
        return publicKeyInfo.GetDerEncoded();
    }

    /// <summary>
    /// 从 PEM 格式字符串解析公钥
    /// </summary>
    /// <param name="pem">PEM 格式的公钥字符串</param>
    /// <returns>解析后的 <see cref="AsymmetricPublicKeyParameter"/> 对象</returns>
    /// <exception cref="InvalidOperationException">当 PEM 格式无效或不包含公钥时抛出</exception>
    /// <remarks>
    /// <para>
    /// 此方法支持解析标准 PEM 格式的公钥：
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>-----BEGIN PUBLIC KEY-----</c> (SubjectPublicKeyInfo，最常用)</description></item>
    /// <item><description><c>-----BEGIN RSA PUBLIC KEY-----</c> (PKCS#1 RSA 公钥)</description></item>
    /// </list>
    /// <para>
    /// <strong>示例输入：</strong>
    /// <code>
    /// -----BEGIN PUBLIC KEY-----
    /// MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...
    /// -----END PUBLIC KEY-----
    /// </code>
    /// </para>
    /// <para>
    /// <strong>格式参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>PEM 编码：<see href="https://datatracker.ietf.org/doc/html/rfc7468#section-13">RFC 7468 Section 13 (Public Key)</see></description></item>
    /// <item><description>SubjectPublicKeyInfo：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7</see></description></item>
    /// </list>
    /// </remarks>
    public static AsymmetricPublicKeyParameter FromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        var obj = pemReader.ReadObject();

        var publicKey = obj switch
        {
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter key when !key.IsPrivate => key,
            _ => throw new InvalidOperationException("Unable to parse the public key from PEM.")
        };

        if (publicKey == null)
            throw new InvalidOperationException("The PEM does not contain a valid public key.");

        return new AsymmetricPublicKeyParameter(publicKey);
    }

    /// <summary>
    /// 从 DER 格式字节数组解析公钥
    /// </summary>
    /// <param name="der">DER 格式的公钥字节数组（SubjectPublicKeyInfo 结构）</param>
    /// <returns>解析后的 <see cref="AsymmetricPublicKeyParameter"/> 对象</returns>
    /// <exception cref="ArgumentException">当 DER 格式无效或无法解析时抛出</exception>
    /// <remarks>
    /// <para>
    /// 此方法期望输入的 DER 数据遵循 SubjectPublicKeyInfo 结构：
    /// <code>
    /// SubjectPublicKeyInfo ::= SEQUENCE {
    ///   algorithm         AlgorithmIdentifier,
    ///   subjectPublicKey  BIT STRING
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// DER 编码是 ASN.1 的二进制编码格式，比 PEM 更紧凑，但不可读。
    /// 通常用于证书、密钥交换等二进制协议场景。
    /// </para>
    /// <para>
    /// <strong>格式参考：</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>SubjectPublicKeyInfo：<see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7">RFC 5280 Section 4.1.2.7</see></description></item>
    /// <item><description>DER 编码：<see href="https://www.itu.int/rec/T-REC-X.690">ITU-T X.690</see></description></item>
    /// <item><description>ASN.1：<see href="https://www.itu.int/rec/T-REC-X.680">ITU-T X.680</see></description></item>
    /// </list>
    /// </remarks>
    public new static AsymmetricPublicKeyParameter FromDer(byte[] der)
    {
        var publicKeyInfo = SubjectPublicKeyInfo.GetInstance(der);
        var publicKey = PublicKeyFactory.CreateKey(publicKeyInfo);
        return new AsymmetricPublicKeyParameter(publicKey);
    }
}
