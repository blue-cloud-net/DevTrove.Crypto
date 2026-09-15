namespace DevTrove.Crypto.X509.Models;

/// <summary>
/// 证书扩展字段
/// 表示 X.509 v3 证书中的一个扩展项。
/// 扩展字段提供证书的附加信息、约束条件和使用规则。
/// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2"/>
/// </summary>
public class CertificateExtension
{
    /// <summary>
    /// 扩展的对象标识符（OID）
    /// 唯一标识扩展的类型，如 "2.5.29.19" 表示 Basic Constraints。
    /// </summary>
    public string Oid { get; }

    /// <summary>
    /// 扩展的友好名称
    /// OID 的可读名称，如 "Basic Constraints"、"Key Usage" 等。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 是否为关键扩展（Critical）
    /// 如果为 true，表示该扩展是关键的，不理解该扩展的应用程序必须拒绝证书。
    /// 如果为 false，不理解该扩展的应用程序可以忽略它。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2"/>
    /// </summary>
    public bool IsCritical { get; }

    /// <summary>
    /// 扩展值（原始字节数组）
    /// 扩展数据的 ASN.1 DER 编码。
    /// </summary>
    public byte[] Value { get; }

    /// <summary>
    /// 扩展值（十六进制字符串）
    /// 便于显示和调试的格式。
    /// </summary>
    public string ValueHex => Convert.ToHexString(this.Value);

    /// <summary>
    /// 常见扩展 OID 名称映射
    /// </summary>
    private static readonly Dictionary<DerObjectIdentifier, string> OidNames = new()
    {
        { X509Extensions.SubjectKeyIdentifier, "Subject Key Identifier" },
        { X509Extensions.KeyUsage, "Key Usage" },
        { X509Extensions.PrivateKeyUsagePeriod, "Private Key Usage Period" },
        { X509Extensions.SubjectAlternativeName, "Subject Alternative Name" },
        { X509Extensions.IssuerAlternativeName, "Issuer Alternative Name" },
        { X509Extensions.BasicConstraints, "Basic Constraints" },
        { X509Extensions.NameConstraints, "Name Constraints" },
        { X509Extensions.CrlDistributionPoints, "CRL Distribution Points" },
        { X509Extensions.CertificatePolicies, "Certificate Policies" },
        { X509Extensions.PolicyMappings, "Policy Mappings" },
        { X509Extensions.AuthorityKeyIdentifier, "Authority Key Identifier" },
        { X509Extensions.PolicyConstraints, "Policy Constraints" },
        { X509Extensions.ExtendedKeyUsage, "Extended Key Usage" },
        { X509Extensions.FreshestCrl, "Freshest CRL" },
        { X509Extensions.InhibitAnyPolicy, "Inhibit Any Policy" },
        { X509Extensions.AuthorityInfoAccess, "Authority Information Access" },
        { X509Extensions.SubjectInfoAccess, "Subject Information Access" },
        { new DerObjectIdentifier("2.16.840.1.113730.1.1"), "Netscape Certificate Type" },
        { new DerObjectIdentifier("2.16.840.1.113730.1.13"), "Netscape Comment" }
    };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="oid">扩展的 OID</param>
    /// <param name="isCritical">是否为关键扩展</param>
    /// <param name="value">扩展值（字节数组）</param>
    public CertificateExtension(string oid, bool isCritical, byte[] value)
    {
        this.Oid = oid;
        this.IsCritical = isCritical;
        this.Value = value;

        var derOid = new DerObjectIdentifier(oid);
        this.Name = OidNames.TryGetValue(derOid, out var name) ? name : $"Unknown ({oid})";
    }

    public override string ToString() => $"{this.Name} (OID: {this.Oid}, Critical: {this.IsCritical})";
}