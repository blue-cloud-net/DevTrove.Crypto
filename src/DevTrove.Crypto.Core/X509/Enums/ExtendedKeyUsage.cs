namespace DevTrove.Crypto.X509.Enums;

/// <summary>
/// X.509 证书扩展密钥用途（Extended Key Usage, EKU）
/// 定义证书的具体应用场景和使用目的，是对 Key Usage 的进一步细化。
/// EKU 提供应用层面的约束，限制证书在特定场景下的使用。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.12"/>
/// </summary>
[Flags]
public enum ExtendedKeyUsage
{
    [Display(Name = "ExtendedKeyUsage_None", ResourceType = typeof(RS))]
    None = 0,

    [Display(Name = "ExtendedKeyUsage_ServerAuthentication", ResourceType = typeof(RS))]
    ServerAuthentication = 1 << 0,

    [Display(Name = "ExtendedKeyUsage_ClientAuthentication", ResourceType = typeof(RS))]
    ClientAuthentication = 1 << 1,

    [Display(Name = "ExtendedKeyUsage_CodeSigning", ResourceType = typeof(RS))]
    CodeSigning = 1 << 2,

    [Display(Name = "ExtendedKeyUsage_EmailProtection", ResourceType = typeof(RS))]
    EmailProtection = 1 << 3,

    [Display(Name = "ExtendedKeyUsage_IpsecEndSystem", ResourceType = typeof(RS))]
    IpsecEndSystem = 1 << 4,

    [Display(Name = "ExtendedKeyUsage_IpsecTunnel", ResourceType = typeof(RS))]
    IpsecTunnel = 1 << 5,

    [Display(Name = "ExtendedKeyUsage_IpsecUser", ResourceType = typeof(RS))]
    IpsecUser = 1 << 6,

    [Display(Name = "ExtendedKeyUsage_TimeStamping", ResourceType = typeof(RS))]
    TimeStamping = 1 << 7,

    [Display(Name = "ExtendedKeyUsage_OcspSigning", ResourceType = typeof(RS))]
    OcspSigning = 1 << 8,

    [Display(Name = "ExtendedKeyUsage_Dvcs", ResourceType = typeof(RS))]
    Dvcs = 1 << 9,

    [Display(Name = "ExtendedKeyUsage_SbgpCertificate", ResourceType = typeof(RS))]
    SbgpCertificate = 1 << 10,

    [Display(Name = "ExtendedKeyUsage_EapOverPpp", ResourceType = typeof(RS))]
    EapOverPpp = 1 << 11,

    [Display(Name = "ExtendedKeyUsage_EapOverLan", ResourceType = typeof(RS))]
    EapOverLan = 1 << 12,

    [Display(Name = "ExtendedKeyUsage_SshClient", ResourceType = typeof(RS))]
    SshClient = 1 << 13,

    [Display(Name = "ExtendedKeyUsage_SshServer", ResourceType = typeof(RS))]
    SshServer = 1 << 14,

    [Display(Name = "ExtendedKeyUsage_DocumentSigning", ResourceType = typeof(RS))]
    DocumentSigning = 1 << 15,

    [Display(Name = "ExtendedKeyUsage_AnyPurpose", ResourceType = typeof(RS))]
    AnyPurpose = 1 << 16,

    [Display(Name = "ExtendedKeyUsage_SmartCardLogon", ResourceType = typeof(RS))]
    SmartCardLogon = 1 << 17
}
