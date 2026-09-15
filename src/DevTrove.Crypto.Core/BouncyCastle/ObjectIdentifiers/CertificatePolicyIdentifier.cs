namespace DevTrove.Crypto.BouncyCastle.ObjectIdentifiers;

/// <summary>
/// 证书策略 OID 常量
/// </summary>
public static class CertificatePolicyObjectIdentifiers
{
    /// <summary>
    /// 任何策略 (2.5.29.32.0)
    /// </summary>
    public static readonly DerObjectIdentifier AnyPolicy = new("2.5.29.32.0");

    /// <summary>
    /// 域名验证（Domain Validated）证书 (2.23.140.1.2.1)
    /// </summary>
    public static readonly DerObjectIdentifier DomainValidated = new("2.23.140.1.2.1");

    /// <summary>
    /// 组织验证（Organization Validated）证书 (2.23.140.1.2.2)
    /// </summary>
    public static readonly DerObjectIdentifier OrganizationValidated = new("2.23.140.1.2.2");

    /// <summary>
    /// 扩展验证（Extended Validation）证书 (2.23.140.1.1)
    /// </summary>
    public static readonly DerObjectIdentifier ExtendedValidation = new("2.23.140.1.1");

    /// <summary>
    /// Microsoft Document Signing (1.3.6.1.4.1.311.10.3.12)
    /// </summary>
    public static readonly DerObjectIdentifier MicrosoftDocumentSigning = new("1.3.6.1.4.1.311.10.3.12");
}
