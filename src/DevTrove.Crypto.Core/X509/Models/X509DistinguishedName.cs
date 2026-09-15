using System.Reflection;

namespace DevTrove.Crypto.X509.Models;

/// <summary>
/// X.509 专有名称（Distinguished Name, DN）
/// DN 是 X.500 标准中用于唯一标识实体的层级化名称结构。
/// 包含 CN（通用名）、O（组织）、OU（组织单位）、C（国家）等属性。
/// 在 X.509 证书中用于标识证书主体（Subject）和颁发者（Issuer）。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.4"/>
/// </summary>
public class X509DistinguishedName
{
    private readonly Org.BouncyCastle.Asn1.X509.X509Name _bcName;
    private readonly Dictionary<string, List<string>> _attributes;
    private static readonly Dictionary<string, string> _oidToFieldNameCache = new Dictionary<string, string>();
    private static readonly object _cacheLock = new object();

    /// <summary>
    /// 国家/地区代码（C - Country）
    /// ISO 3166 标准的两字母国家代码，如 CN（中国）、US（美国）。
    /// </summary>
    public string? Country => this.GetFirstValue(Org.BouncyCastle.Asn1.X509.X509Name.C);

    /// <summary>
    /// 州/省名称（ST - State or Province）
    /// 实体所在的州或省的完整名称，如"北京市"、"California"。
    /// </summary>
    public string? StateOrProvince => this.GetFirstValue(Org.BouncyCastle.Asn1.X509.X509Name.ST);

    /// <summary>
    /// 地区/城市名称（L - Locality）
    /// 实体所在的城市或地区名称，如"海淀区"、"San Francisco"。
    /// </summary>
    public string? Locality => this.GetFirstValue(Org.BouncyCastle.Asn1.X509.X509Name.L);

    /// <summary>
    /// 组织名称（O - Organization）
    /// 实体所属的组织或公司名称，如"某某科技有限公司"。
    /// </summary>
    public string? Organization => this.GetFirstValue(Org.BouncyCastle.Asn1.X509.X509Name.O);

    /// <summary>
    /// 组织单位（OU - Organizational Unit）
    /// 组织内的部门或单位名称，如"研发部"、"IT Department"。
    /// </summary>
    public string? OrganizationalUnit => this.GetFirstValue(Org.BouncyCastle.Asn1.X509.X509Name.OU);

    /// <summary>
    /// 通用名称（CN - Common Name）
    /// 实体的通用标识名，对于网站证书通常是域名，对于个人证书是姓名。
    /// 这是 DN 中最重要的字段之一。
    /// </summary>
    public string? CommonName => this.GetFirstValue(Org.BouncyCastle.Asn1.X509.X509Name.CN);

    /// <summary>
    /// 电子邮件地址（E - Email Address）
    /// 实体的电子邮件地址。
    /// </summary>
    public string? EmailAddress => this.GetFirstValue(Org.BouncyCastle.Asn1.X509.X509Name.E);

    /// <summary>
    /// 所有属性的字典，键为 OID 或常见名称，值为属性值列表
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> Attributes => _attributes;

    /// <summary>
    /// 完整的 DN 字符串
    /// </summary>
    public string DistinguishedName => _bcName.ToString();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="bcName">BouncyCastle X509Name 对象</param>
    public X509DistinguishedName(Org.BouncyCastle.Asn1.X509.X509Name bcName)
    {
        _bcName = bcName ?? throw new ArgumentNullException(nameof(bcName));
        _attributes = this.ParseAttributes();
    }

    /// <summary>
    /// 从字符串解析 X509Name
    /// </summary>
    /// <param name="distinguishedName">DN 字符串，例如 "CN=Example,O=Org,C=US"</param>
    /// <returns>X509Name 实例</returns>
    public static X509DistinguishedName Parse(string distinguishedName)
    {
        var bcName = new Org.BouncyCastle.Asn1.X509.X509Name(distinguishedName);
        return new X509DistinguishedName(bcName);
    }

    /// <summary>
    /// 获取指定 OID 的第一个值
    /// </summary>
    /// <param name="oid">OID 对象</param>
    /// <returns>属性值，如果不存在则返回 null</returns>
    private string? GetFirstValue(DerObjectIdentifier oid)
    {
        var values = _bcName.GetValueList(oid);
        return values?.Count > 0 ? values[0]?.ToString() : null;
    }

    /// <summary>
    /// 获取指定 OID 的所有值
    /// </summary>
    /// <param name="oid">OID 对象</param>
    /// <returns>属性值列表</returns>
    public List<string> GetValues(DerObjectIdentifier oid)
    {
        var values = _bcName.GetValueList(oid);
        return values?.Cast<object>().Select(v => v.ToString() ?? string.Empty).ToList() ?? [];
    }

    /// <summary>
    /// 解析所有属性到字典
    /// </summary>
    private Dictionary<string, List<string>> ParseAttributes()
    {
        var attributes = new Dictionary<string, List<string>>();

        var oids = _bcName.GetOidList();
        var values = _bcName.GetValueList();

        for (var i = 0; i < oids.Count; i++)
        {
            var oid = oids[i] as DerObjectIdentifier;
            if (oid == null) continue;

            var value = values[i]?.ToString() ?? string.Empty;
            var key = this.GetFriendlyName(oid);

            if (!attributes.ContainsKey(key))
            {
                attributes[key] = [];
            }
            attributes[key].Add(value);
        }

        return attributes;
    }

    /// <summary>
    /// 获取 OID 的友好名称（通过反射获取 X509Name 类中的静态字段名）
    /// </summary>
    private string GetFriendlyName(DerObjectIdentifier oid)
    {
        if (oid == null)
            return "Unknown";

        var oidId = oid.Id;

        // 先查找缓存
        lock (_cacheLock)
        {
            if (_oidToFieldNameCache.TryGetValue(oidId, out var cachedName))
                return cachedName;
        }

        // 使用反射查找 Org.BouncyCastle.Asn1.X509.X509Name 中的静态字段
        var x509NameType = typeof(Org.BouncyCastle.Asn1.X509.X509Name);
        var fields = x509NameType.GetFields(BindingFlags.Public | BindingFlags.Static);

        // 收集所有匹配的字段名
        var matchingFieldNames = new List<string>();

        foreach (var field in fields)
        {
            // 只处理 DerObjectIdentifier 类型的字段
            if (field.FieldType == typeof(DerObjectIdentifier))
            {
                var fieldValue = field.GetValue(null) as DerObjectIdentifier;
                if (fieldValue != null && fieldValue.Equals(oid))
                {
                    matchingFieldNames.Add(field.Name);
                }
            }
        }

        string resultName;
        if (matchingFieldNames.Count > 0)
        {
            // 如果有多个字段指向同一个OID，优先选择最短的、常用的名称
            // 例如：E 比 EmailAddress 短，所以优先选择 E
            resultName = matchingFieldNames
                .OrderBy(name => name.Length)  // 按长度排序
                .ThenBy(name => name)          // 如果长度相同，按字母顺序
                .First();
        }
        else
        {
            // 如果没有找到对应的字段名，返回 OID 字符串
            resultName = oidId;
        }

        // 缓存结果
        lock (_cacheLock)
        {
            if (!_oidToFieldNameCache.ContainsKey(oidId))
            {
                _oidToFieldNameCache[oidId] = resultName;
            }
        }

        return resultName;
    }

    /// <summary>
    /// 获取原始 BouncyCastle X509Name 对象
    /// </summary>
    public Org.BouncyCastle.Asn1.X509.X509Name GetBouncyCastleName() => _bcName;

    public override string ToString() => this.DistinguishedName;

    /// <summary>
    /// 获取格式化的属性字符串
    /// </summary>
    public string ToFormattedString()
    {
        var lines = new List<string>();
        foreach (var attr in _attributes)
        {
            foreach (var value in attr.Value)
            {
                lines.Add($"{attr.Key}={value}");
            }
        }
        return string.Join(", ", lines);
    }
}
