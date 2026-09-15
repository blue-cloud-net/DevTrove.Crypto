using DevTrove.Crypto.BouncyCastle.ObjectIdentifiers;
using CertificatePolicy = DevTrove.Crypto.X509.Enums.CertificatePolicy;

namespace DevTrove.Crypto.X509.Extensions;

/// <summary>
/// 证书策略辅助类
/// 提供证书策略 OID 和枚举值之间的转换功能。
/// </summary>
public static class CertificatePolicyHelper
{
    /// <summary>
    /// OID 到枚举的映射
    /// </summary>
    private static readonly Dictionary<DerObjectIdentifier, CertificatePolicy> _oidToEnum = new();

    /// <summary>
    /// 枚举到 OID 的映射
    /// </summary>
    private static readonly Dictionary<CertificatePolicy, DerObjectIdentifier> _enumToOid = new();

    static CertificatePolicyHelper()
    {
        // 使用 DefinePolicy 方法初始化映射关系
        DefinePolicy(CertificatePolicyObjectIdentifiers.AnyPolicy, CertificatePolicy.AnyPolicy);
        DefinePolicy(CertificatePolicyObjectIdentifiers.DomainValidated, CertificatePolicy.DomainValidated);
        DefinePolicy(CertificatePolicyObjectIdentifiers.OrganizationValidated, CertificatePolicy.OrganizationValidated);
        DefinePolicy(CertificatePolicyObjectIdentifiers.ExtendedValidation, CertificatePolicy.ExtendedValidation);
        DefinePolicy(CertificatePolicyObjectIdentifiers.MicrosoftDocumentSigning, CertificatePolicy.MicrosoftDocumentSigning);
    }

    /// <summary>
    /// 定义证书策略映射关系
    /// </summary>
    /// <param name="oid">OID 对象</param>
    /// <param name="policy">证书策略枚举值</param>
    private static void DefinePolicy(DerObjectIdentifier oid, CertificatePolicy policy)
    {
        _oidToEnum.Add(oid, policy);
        _enumToOid.Add(policy, oid);
    }

    /// <summary>
    /// 从 OID 对象获取证书策略枚举值
    /// </summary>
    /// <param name="oid">OID 对象</param>
    /// <returns>证书策略枚举值，如果未找到则返回 None</returns>
    public static CertificatePolicy FromOid(DerObjectIdentifier oid)
        => _oidToEnum.GetValueOrDefault(oid, CertificatePolicy.None);

    /// <summary>
    /// 从多个 OID 对象获取第一个匹配的证书策略枚举值
    /// </summary>
    /// <param name="oids">OID 对象集合</param>
    /// <returns>第一个匹配的证书策略枚举值，如果都未找到则返回 null</returns>
    public static CertificatePolicy? FromOids(IEnumerable<DerObjectIdentifier>? oids)
    {
        if (oids is null)
            return null;

        foreach (var oid in oids)
        {
            var policy = FromOid(oid);
            if (policy != CertificatePolicy.None)
            {
                return policy;
            }
        }
        return null;
    }

    /// <summary>
    /// 将证书策略枚举值转换为 OID 字符串
    /// </summary>
    /// <param name="policy">证书策略枚举值</param>
    /// <returns>OID 字符串，如果未找到则返回 null</returns>
    public static string? ToOid(CertificatePolicy policy)
        => _enumToOid.TryGetValue(policy, out var oid) ? oid.Id : null;

    /// <summary>
    /// 将多个证书策略枚举值转换为 OID 字符串集合
    /// </summary>
    /// <param name="policies">证书策略枚举值集合</param>
    /// <returns>OID 字符串集合</returns>
    public static IEnumerable<string> ToOids(IEnumerable<CertificatePolicy> policies)
    {
        var oids = new List<string>();
        foreach (var policy in policies)
        {
            var oid = ToOid(policy);
            if (oid is not null)
            {
                oids.Add(oid);
            }
        }
        return oids;
    }
}
