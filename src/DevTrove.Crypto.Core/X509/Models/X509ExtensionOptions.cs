using KeyUsage = DevTrove.Crypto.X509.Enums.KeyUsage;
using ExtendedKeyUsage = DevTrove.Crypto.X509.Enums.ExtendedKeyUsage;

namespace DevTrove.Crypto.X509.Models;

/// <summary>
/// X.509 v3 证书/CSR 扩展字段参数值对象。
/// 用于在生成证书或 CSR 时指定 KeyUsage、ExtendedKeyUsage、SubjectAlternativeNames、
/// BasicConstraints、SKI、AKI、CRL 分发点等扩展。
/// </summary>
public sealed class X509ExtensionOptions
{
    /// <summary>
    /// 密钥用途（KeyUsage）。
    /// 为 <c>null</c> 时不添加该扩展。
    /// </summary>
    public KeyUsage? KeyUsages { get; set; }

    /// <summary>
    /// 扩展密钥用途（Extended Key Usage）。
    /// 为 <c>null</c> 时不添加该扩展。
    /// </summary>
    public ExtendedKeyUsage? ExtendedKeyUsages { get; set; }

    /// <summary>
    /// 主题备用名称（Subject Alternative Names）。
    /// 为 <c>null</c> 或空时不添加该扩展。
    /// </summary>
    public IReadOnlyList<GeneralName>? SubjectAlternativeNames { get; set; }

    /// <summary>
    /// 基本约束（Basic Constraints）。
    /// 为 <c>null</c> 时不添加该扩展。
    /// </summary>
    public BasicConstraintsOptions? BasicConstraints { get; set; }

    /// <summary>
    /// 是否包含主题密钥标识符（Subject Key Identifier）。
    /// </summary>
    public bool IncludeSubjectKeyIdentifier { get; set; }

    /// <summary>
    /// 是否包含颁发机构密钥标识符（Authority Key Identifier）。
    /// 生成证书时由颁发者公钥计算；自签名时为自身公钥。
    /// </summary>
    public bool IncludeAuthorityKeyIdentifier { get; set; }

    /// <summary>
    /// CRL 分发点（CRL Distribution Points）URL 列表。
    /// 为 <c>null</c> 或空时不添加该扩展。
    /// </summary>
    public IReadOnlyList<string>? CrlDistributionPointUrls { get; set; }

    /// <summary>
    /// 校验扩展参数，非法值抛出 <see cref="ArgumentException"/>。
    /// </summary>
    public void Validate()
    {
        if (this.SubjectAlternativeNames is { Count: > 0 })
        {
            if (this.SubjectAlternativeNames.Any(name => name == default || string.IsNullOrWhiteSpace(name.Value)))
            {
                throw new ArgumentException("Subject alternative names cannot contain empty values.");
            }
        }

        if (this.CrlDistributionPointUrls is { Count: > 0 })
        {
            foreach (var url in this.CrlDistributionPointUrls)
            {
                if (string.IsNullOrWhiteSpace(url)
                    || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != "ldap"))
                {
                    throw new ArgumentException($"Invalid CRL distribution point URL: '{url}'.");
                }
            }
        }
    }
}

/// <summary>
/// 基本约束（Basic Constraints）扩展参数。
/// </summary>
public sealed class BasicConstraintsOptions
{
    /// <summary>
    /// 是否为 CA 证书。
    /// </summary>
    public bool IsCa { get; set; }

    /// <summary>
    /// 路径长度约束（Path Length Constraint）。
    /// 为 <c>null</c> 时表示无路径长度限制。
    /// </summary>
    public int? PathLengthConstraint { get; set; }

    /// <summary>
    /// 校验基本约束参数。
    /// </summary>
    public void Validate()
    {
        if (this.PathLengthConstraint is < 0)
        {
            throw new ArgumentException("Path length constraint cannot be negative.");
        }
    }
}
