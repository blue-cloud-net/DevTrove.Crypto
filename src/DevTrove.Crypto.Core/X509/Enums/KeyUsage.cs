namespace DevTrove.Crypto.X509.Enums;

/// <summary>
/// X.509 证书密钥用途（Key Usage）
/// 定义证书公钥的使用目的和限制，是证书扩展字段的重要组成部分。
/// 用于约束证书可以用于哪些密码学操作，提高证书使用的安全性。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.3"/>
/// </summary>
[Flags]
public enum KeyUsage
{
    /// <summary>
    /// 无
    /// </summary>
    [Display(Name = "KeyUsage_None", ResourceType = typeof(CryptoUtilCore))]
    None = 0,

    /// <summary>
    /// 数字签名（Digital Signature）
    /// 用于验证数字签名，不包括证书和 CRL 签名。
    /// 适用场景：TLS 客户端认证、电子邮件签名、代码签名等。
    /// </summary>
    [Display(Name = "KeyUsage_DigitalSignature", ResourceType = typeof(CryptoUtilCore))]
    DigitalSignature = 1 << 0,

    /// <summary>
    /// 不可否认性（Non Repudiation / Content Commitment）
    /// 提供防止签名者否认的证据，用于需要法律效力的场景。
    /// 适用场景：电子合同、重要文档签名等。
    /// </summary>
    [Display(Name = "KeyUsage_NonRepudiation", ResourceType = typeof(CryptoUtilCore))]
    NonRepudiation = 1 << 1,

    /// <summary>
    /// 密钥加密（Key Encipherment）
    /// 用于加密密钥或密钥材料，常用于密钥传输。
    /// 适用场景：TLS 握手中的 RSA 密钥交换。
    /// </summary>
    [Display(Name = "KeyUsage_KeyEncipherment", ResourceType = typeof(CryptoUtilCore))]
    KeyEncipherment = 1 << 2,

    /// <summary>
    /// 数据加密（Data Encipherment）
    /// 直接用于加密用户数据，而非密钥。
    /// 注意：现代实践中较少使用，通常使用密钥加密方式。
    /// </summary>
    [Display(Name = "KeyUsage_DataEncipherment", ResourceType = typeof(CryptoUtilCore))]
    DataEncipherment = 1 << 3,

    /// <summary>
    /// 密钥协商（Key Agreement）
    /// 用于密钥协商协议，如 Diffie-Hellman 或 ECDH。
    /// 适用场景：TLS 中的 ECDHE 密钥交换。
    /// </summary>
    [Display(Name = "KeyUsage_KeyAgreement", ResourceType = typeof(CryptoUtilCore))]
    KeyAgreement = 1 << 4,

    /// <summary>
    /// 证书签名（Certificate Signing）
    /// 用于签发和验证其他证书，仅限 CA 证书使用。
    /// 适用场景：根证书、中间证书。
    /// </summary>
    [Display(Name = "KeyUsage_KeyCertSign", ResourceType = typeof(CryptoUtilCore))]
    KeyCertSign = 1 << 5,

    /// <summary>
    /// CRL 签名（CRL Signing）
    /// 用于签发和验证证书吊销列表（CRL）。
    /// 适用场景：CA 签发 CRL。
    /// </summary>
    [Display(Name = "KeyUsage_CrlSign", ResourceType = typeof(CryptoUtilCore))]
    CrlSign = 1 << 6,

    /// <summary>
    /// 仅用于加密（Encipher Only）
    /// 仅当 KeyAgreement 也设置时有效，表示只能用于加密操作。
    /// </summary>
    [Display(Name = "KeyUsage_EncipherOnly", ResourceType = typeof(CryptoUtilCore))]
    EncipherOnly = 1 << 7,

    /// <summary>
    /// 仅用于解密（Decipher Only）
    /// 仅当 KeyAgreement 也设置时有效，表示只能用于解密操作。
    /// </summary>
    [Display(Name = "KeyUsage_DecipherOnly", ResourceType = typeof(CryptoUtilCore))]
    DecipherOnly = 1 << 8
}
