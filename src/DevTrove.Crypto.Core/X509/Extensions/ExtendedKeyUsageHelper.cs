using DevTrove.Crypto.BouncyCastle.ObjectIdentifiers;
using ExtendedKeyUsage = DevTrove.Crypto.X509.Enums.ExtendedKeyUsage;

namespace DevTrove.Crypto.X509.Extensions;

/// <summary>
/// 扩展密钥用途辅助类
/// 提供扩展密钥用途 OID 和枚举值之间的转换功能。
/// </summary>
public static class ExtendedKeyUsageHelper
{
    /// <summary>
    /// OID 到枚举的映射
    /// </summary>
    private static readonly Dictionary<DerObjectIdentifier, ExtendedKeyUsage> _oIdToEnum = new();

    /// <summary>
    /// 枚举到 OID 的映射
    /// </summary>
    private static readonly Dictionary<ExtendedKeyUsage, DerObjectIdentifier> _enumToOid = new();

    static ExtendedKeyUsageHelper()
    {
        // 使用 DefineEKU 方法初始化映射关系
        DefineEku(KeyPurposeID.id_kp_serverAuth, ExtendedKeyUsage.ServerAuthentication);
        DefineEku(KeyPurposeID.id_kp_clientAuth, ExtendedKeyUsage.ClientAuthentication);
        DefineEku(KeyPurposeID.id_kp_codeSigning, ExtendedKeyUsage.CodeSigning);
        DefineEku(KeyPurposeID.id_kp_emailProtection, ExtendedKeyUsage.EmailProtection);
        DefineEku(KeyPurposeID.id_kp_ipsecEndSystem, ExtendedKeyUsage.IpsecEndSystem);
        DefineEku(KeyPurposeID.id_kp_ipsecTunnel, ExtendedKeyUsage.IpsecTunnel);
        DefineEku(KeyPurposeID.id_kp_ipsecUser, ExtendedKeyUsage.IpsecUser);
        DefineEku(KeyPurposeID.id_kp_timeStamping, ExtendedKeyUsage.TimeStamping);
        DefineEku(KeyPurposeID.id_kp_OCSPSigning, ExtendedKeyUsage.OcspSigning);
        DefineEku(KeyPurposeID.id_kp_dvcs, ExtendedKeyUsage.Dvcs);
        DefineEku(KeyPurposeID.id_kp_sbgpCertAAServerAuth, ExtendedKeyUsage.SbgpCertificate);
        DefineEku(KeyPurposeID.id_kp_eapOverPPP, ExtendedKeyUsage.EapOverPpp);
        DefineEku(KeyPurposeID.id_kp_eapOverLAN, ExtendedKeyUsage.EapOverLan);
        DefineEku(ExtendedKeyUsageObjectIdentifiers.SshClient, ExtendedKeyUsage.SshClient);
        DefineEku(ExtendedKeyUsageObjectIdentifiers.SshServer, ExtendedKeyUsage.SshServer);
        DefineEku(ExtendedKeyUsageObjectIdentifiers.DocumentSigning, ExtendedKeyUsage.DocumentSigning);
        DefineEku(KeyPurposeID.AnyExtendedKeyUsage, ExtendedKeyUsage.AnyPurpose);
        DefineEku(KeyPurposeID.id_kp_smartcardlogon, ExtendedKeyUsage.SmartCardLogon);
    }

    /// <summary>
    /// 定义扩展密钥用途映射关系
    /// </summary>
    /// <param name="oid">OID 对象</param>
    /// <param name="usage">扩展密钥用途枚举值</param>
    private static void DefineEku(DerObjectIdentifier oid, ExtendedKeyUsage usage)
    {
        _oIdToEnum.Add(oid, usage);
        _enumToOid.Add(usage, oid);
    }

    /// <summary>
    /// 从 BouncyCastle ExtendedKeyUsage 对象转换为扩展密钥用途枚举值
    /// </summary>
    /// <param name="extendedKeyUsage">BouncyCastle ExtendedKeyUsage 对象</param>
    /// <returns>扩展密钥用途枚举值（可能包含多个标志）</returns>
    public static ExtendedKeyUsage FromBouncyCastleFormat(Org.BouncyCastle.Asn1.X509.ExtendedKeyUsage extendedKeyUsage)
    {
        var oids = extendedKeyUsage.GetAllUsages();
        return FromOids(oids);
    }

    /// <summary>
    /// 从 OID 字符串集合转换为扩展密钥用途枚举值
    /// </summary>
    /// <param name="oids">OID 字符串集合（可为 <see langword="null"/>，返回 <see cref="ExtendedKeyUsage.None"/>）</param>
    /// <returns>扩展密钥用途枚举值（可能包含多个标志）</returns>
    public static ExtendedKeyUsage FromOids(IEnumerable<string>? oids) =>
        oids is null ? ExtendedKeyUsage.None : FromOids(oids.Select(oid => new DerObjectIdentifier(oid)));

    /// <summary>
    /// 从 OID 对象集合转换为扩展密钥用途枚举值
    /// </summary>
    /// <param name="oids">OID 对象集合（可为 <see langword="null"/>，返回 <see cref="ExtendedKeyUsage.None"/>）</param>
    /// <returns>扩展密钥用途枚举值（可能包含多个标志）</returns>
    public static ExtendedKeyUsage FromOids(IEnumerable<DerObjectIdentifier>? oids)
    {
        var usages = ExtendedKeyUsage.None;
        if (oids is null)
        {
            return usages;
        }

        foreach (var oid in oids)
        {
            if (_oIdToEnum.TryGetValue(oid, out var usage))
            {
                usages |= usage;
            }
        }
        return usages;
    }

    /// <summary>
    /// 将扩展密钥用途枚举值转换为 BouncyCastle ExtendedKeyUsage 对象
    /// </summary>
    /// <param name="usages">扩展密钥用途枚举值</param>
    /// <returns>BouncyCastle ExtendedKeyUsage 对象</returns>
    public static Org.BouncyCastle.Asn1.X509.ExtendedKeyUsage ToBouncyCastleFormat(ExtendedKeyUsage usages)
    {
        var oidList = new List<DerObjectIdentifier>();
        foreach (var kvp in _enumToOid)
        {
            if (usages.HasFlag(kvp.Key))
                oidList.Add(kvp.Value);
        }
        return new Org.BouncyCastle.Asn1.X509.ExtendedKeyUsage(oidList.ToArray());
    }

    /// <summary>
    /// 将扩展密钥用途枚举值转换为 OID 字符串集合
    /// </summary>
    /// <param name="usages">扩展密钥用途枚举值</param>
    /// <returns>OID 字符串集合</returns>
    public static IEnumerable<string> ToOids(ExtendedKeyUsage usages)
    {
        var oids = new List<string>();
        foreach (var kvp in _enumToOid)
        {
            if (usages.HasFlag(kvp.Key))
                oids.Add(kvp.Value.Id);
        }
        return oids;
    }
}
