using DevTrove.Crypto.X509.Models;

namespace DevTrove.Crypto.X509.Extensions;

/// <summary>
/// X.509 扩展字段构建器。
/// 将 <see cref="X509ExtensionOptions"/> 中的扩展参数应用到 <see cref="X509V3CertificateGenerator"/>
/// 或构建供 CSR（PKCS#10 extensionRequest）使用的 <see cref="Org.BouncyCastle.Asn1.X509.X509Extensions"/>。
/// </summary>
public static class X509ExtensionBuilder
{
    /// <summary>
    /// 将 <paramref name="options"/> 中配置的扩展全部应用到证书生成器。
    /// </summary>
    /// <param name="generator">X.509 v3 证书生成器。</param>
    /// <param name="options">扩展参数。</param>
    /// <param name="subjectPublicKey">证书主体的公钥（用于计算 SKI）。</param>
    /// <param name="authorityPublicKey">颁发者的公钥（用于计算 AKI，可为 <c>null</c>）。</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="generator"/>、<paramref name="options"/> 或 <paramref name="subjectPublicKey"/> 为 <see langword="null"/>。
    /// </exception>
    public static void Apply(
        X509V3CertificateGenerator generator,
        X509ExtensionOptions options,
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter subjectPublicKey,
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter? authorityPublicKey = null)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subjectPublicKey);

        foreach (var (oid, isCritical, value) in BuildExtensionValues(options, subjectPublicKey, authorityPublicKey))
        {
            generator.AddExtension(oid, isCritical, value);
        }
    }

    /// <summary>
    /// 构建供 CSR（PKCS#10 extensionRequest）使用的 X.509 扩展集合。
    /// </summary>
    /// <param name="options">扩展参数。</param>
    /// <param name="subjectPublicKey">申请者的公钥（用于计算 SKI）。</param>
    /// <param name="authorityPublicKey">颁发者的公钥（用于计算 AKI，可为 <c>null</c>）。</param>
    /// <returns>X.509 扩展集合。</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> 或 <paramref name="subjectPublicKey"/> 为 <see langword="null"/>。
    /// </exception>
    public static Org.BouncyCastle.Asn1.X509.X509Extensions BuildX509Extensions(
        X509ExtensionOptions options,
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter subjectPublicKey,
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter? authorityPublicKey = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(subjectPublicKey);

        var extensions = new Dictionary<DerObjectIdentifier, X509Extension>();
        foreach (var (oid, isCritical, value) in BuildExtensionValues(options, subjectPublicKey, authorityPublicKey))
        {
            extensions[oid] = new X509Extension(isCritical, new DerOctetString(value));
        }

        return new Org.BouncyCastle.Asn1.X509.X509Extensions(extensions);
    }

    /// <summary>
    /// 构建扩展值列表（OID、是否关键、ASN.1 值）。
    /// </summary>
    private static List<(DerObjectIdentifier Oid, bool IsCritical, Asn1Encodable Value)> BuildExtensionValues(
        X509ExtensionOptions options,
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter subjectPublicKey,
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter? authorityPublicKey)
    {
        options.Validate();

        var result = new List<(DerObjectIdentifier, bool, Asn1Encodable)>();

        if (options.BasicConstraints is { } basicConstraints)
        {
            basicConstraints.Validate();

            BasicConstraints value;
            if (basicConstraints.IsCa && basicConstraints.PathLengthConstraint.HasValue)
            {
                value = new BasicConstraints(basicConstraints.PathLengthConstraint.Value);
            }
            else
            {
                value = new BasicConstraints(basicConstraints.IsCa);
            }

            result.Add((X509Extensions.BasicConstraints, true, value));
        }

        if (options.KeyUsages is { } keyUsages)
        {
            result.Add((X509Extensions.KeyUsage, true, KeyUsageHelper.ToBouncyCastleFormat(keyUsages)));
        }

        if (options.ExtendedKeyUsages is { } extendedKeyUsages)
        {
            result.Add((X509Extensions.ExtendedKeyUsage, false, ExtendedKeyUsageHelper.ToBouncyCastleFormat(extendedKeyUsages)));
        }

        if (options.SubjectAlternativeNames is { Count: > 0 } san)
        {
            var bcNames = san.Select(name => name.GetBouncyCastleGeneralName()).ToArray();
            result.Add((X509Extensions.SubjectAlternativeName, false, new GeneralNames(bcNames)));
        }

        if (options.IncludeSubjectKeyIdentifier)
        {
            var ski = X509ExtensionUtilities.CreateSubjectKeyIdentifier(
                SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(subjectPublicKey));
            result.Add((X509Extensions.SubjectKeyIdentifier, false, ski));
        }

        if (options.IncludeAuthorityKeyIdentifier && authorityPublicKey is not null)
        {
            var aki = X509ExtensionUtilities.CreateAuthorityKeyIdentifier(
                SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(authorityPublicKey));
            result.Add((X509Extensions.AuthorityKeyIdentifier, false, aki));
        }

        if (options.CrlDistributionPointUrls is { Count: > 0 } crlUrls)
        {
            result.Add((X509Extensions.CrlDistributionPoints, false, BuildCrlDistPoint(crlUrls)));
        }

        return result;
    }

    /// <summary>
    /// 从 URL 列表构建 CRL 分发点扩展。
    /// </summary>
    private static CrlDistPoint BuildCrlDistPoint(IReadOnlyList<string> urls)
    {
        var distributionPoints = urls.Select(url =>
        {
            var generalNames = new GeneralNames(
                new Org.BouncyCastle.Asn1.X509.GeneralName(
                    Org.BouncyCastle.Asn1.X509.GeneralName.UniformResourceIdentifier,
                    url));
            var distributionPointName = new DistributionPointName(DistributionPointName.FullName, generalNames);
            return new DistributionPoint(distributionPointName, null, null);
        }).ToArray();

        return new CrlDistPoint(distributionPoints);
    }
}
