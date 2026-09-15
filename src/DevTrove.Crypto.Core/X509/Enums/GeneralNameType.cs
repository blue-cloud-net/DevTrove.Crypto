namespace DevTrove.Crypto.X509.Enums;

/// <summary>
/// X.509 通用名称类型（GeneralName Type）
/// 定义 X.509 证书扩展字段中可以使用的各种名称格式。
/// 主要用于主题备用名称（Subject Alternative Name, SAN）和颁发者备用名称（Issuer Alternative Name, IAN）扩展。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.6"/>
/// </summary>
/// <remarks>
/// <para>
/// GeneralName 提供了灵活的命名机制，允许证书包含多种格式的标识信息。
/// 在现代 PKI 中，特别是 TLS/SSL 证书，SAN 扩展已成为指定域名的主要方式。
/// </para>
/// <para>
/// <strong>常见使用场景：</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>DnsName</strong>：Web 服务器证书的域名（最常用）</description></item>
/// <item><description><strong>IPAddress</strong>：直接使用 IP 地址的服务</description></item>
/// <item><description><strong>Rfc822Name</strong>：电子邮件证书的邮箱地址</description></item>
/// <item><description><strong>UniformResourceIdentifier</strong>：CRL 分发点、OCSP 服务器 URL</description></item>
/// </list>
/// <para>
/// GeneralName 结构定义（ASN.1）：
/// <code>
/// GeneralName ::= CHOICE {
///   otherName                  [0] OtherName,
///   rfc822Name                 [1] IA5String,
///   dNSName                    [2] IA5String,
///   x400Address                [3] ORAddress,
///   directoryName              [4] Name,
///   ediPartyName               [5] EDIPartyName,
///   uniformResourceIdentifier  [6] IA5String,
///   iPAddress                  [7] OCTET STRING,
///   registeredID               [8] OBJECT IDENTIFIER
/// }
/// </code>
/// </para>
/// </remarks>
public enum GeneralNameType
{
    /// <summary>
    /// 未知类型
    /// 表示无法识别的 GeneralName 类型，通常用于错误处理。
    /// </summary>
    [Display(Name = "GeneralNameType_Unknown", ResourceType = typeof(RS))]
    Unknown = -1,

    /// <summary>
    /// 其他名称（OtherName）
    /// 标签：[0]
    /// 用于自定义或特殊用途的名称类型，包含 OID 和对应的值。
    /// 允许扩展支持新的命名格式，如 UPN（用户主体名称）、Kerberos 主体等。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.6"/>
    /// </summary>
    /// <example>
    /// Microsoft UPN (User Principal Name): id-on-Microsoft-UPN (1.3.6.1.4.1.311.20.2.3)
    /// Kerberos Principal Name: id-pkinit-san (1.3.6.1.5.2.2)
    /// </example>
    [Display(Name = "GeneralNameType_OtherName", ResourceType = typeof(RS))]
    OtherName = 0,

    /// <summary>
    /// RFC 822 名称（电子邮件地址）
    /// 标签：[1]
    /// 符合 RFC 822 标准的电子邮件地址格式。
    /// 广泛用于 S/MIME 电子邮件证书，标识邮件发送者或接收者。
    /// </summary>
    /// <example>
    /// user@example.com
    /// admin@mail.example.org
    /// </example>
    /// <remarks>
    /// RFC 822 标准：<see href="https://datatracker.ietf.org/doc/html/rfc822"/>
    /// </remarks>
    [Display(Name = "GeneralNameType_Rfc822Name", ResourceType = typeof(RS))]
    Rfc822Name = 1,

    /// <summary>
    /// DNS 名称（域名）
    /// 标签：[2]
    /// 完全限定域名（FQDN）或通配符域名。
    /// 这是 TLS/SSL 证书中最常用的类型，用于指定证书可以保护的域名。
    /// 现代浏览器主要依赖 SAN 中的 DNS 名称进行域名验证，而非证书主题的 CN 字段。
    /// </summary>
    /// <example>
    /// www.example.com
    /// *.example.com (通配符，匹配所有一级子域名)
    /// mail.example.com
    /// </example>
    /// <remarks>
    /// 通配符仅匹配一级子域名，不匹配多级子域名。
    /// 例如：*.example.com 匹配 www.example.com，但不匹配 sub.www.example.com
    /// </remarks>
    [Display(Name = "GeneralNameType_DnsName", ResourceType = typeof(RS))]
    DnsName = 2,

    /// <summary>
    /// X.400 地址
    /// 标签：[3]
    /// 用于 X.400 电子邮件系统的地址。
    /// X.400 是 OSI 模型中的消息处理系统标准，目前较少使用。
    /// </summary>
    /// <remarks>
    /// X.400 标准由 ITU-T 定义，参考 ITU-T X.400 系列建议。
    /// 在现代互联网中，SMTP/RFC 822 邮件系统更为普及。
    /// </remarks>
    [Display(Name = "GeneralNameType_X400Address", ResourceType = typeof(RS))]
    X400Address = 3,

    /// <summary>
    /// 目录名称（Distinguished Name）
    /// 标签：[4]
    /// 完整的 X.500 目录服务可分辨名称。
    /// 用于引用 LDAP 目录中的条目或指定证书主体的完整 DN。
    /// </summary>
    /// <example>
    /// CN=John Doe,OU=IT,O=Example Inc,C=US
    /// </example>
    /// <remarks>
    /// DN 结构参考：<see href="https://datatracker.ietf.org/doc/html/rfc4514">RFC 4514 (LDAP: String Representation of Distinguished Names)</see>
    /// </remarks>
    [Display(Name = "GeneralNameType_DirectoryName", ResourceType = typeof(RS))]
    DirectoryName = 4,

    /// <summary>
    /// EDI 方名称
    /// 标签：[5]
    /// 用于电子数据交换（Electronic Data Interchange, EDI）系统的参与方标识。
    /// EDI 是企业间结构化数据交换的标准，用于订单、发票等商业文档的自动化处理。
    /// </summary>
    /// <remarks>
    /// EDI 标准包括 ANSI X12、UN/EDIFACT 等。
    /// 此名称类型在 PKI 中相对少见，主要用于特定的 B2B 应用场景。
    /// </remarks>
    [Display(Name = "GeneralNameType_EdiPartyName", ResourceType = typeof(RS))]
    EdiPartyName = 5,

    /// <summary>
    /// 统一资源标识符（URI）
    /// 标签：[6]
    /// 符合 URI 规范的资源标识符，如 HTTP、HTTPS、LDAP URL 等。
    /// 常用于证书扩展中指定服务端点，如 CRL 分发点（CDP）、授权信息访问（AIA）等。
    /// </summary>
    /// <example>
    /// http://www.example.com
    /// https://ocsp.example.com
    /// ldap://ldap.example.com/cn=CA,dc=example,dc=com
    /// </example>
    /// <remarks>
    /// URI 语法参考：<see href="https://datatracker.ietf.org/doc/html/rfc3986">RFC 3986 (Uniform Resource Identifier)</see>
    /// </remarks>
    [Display(Name = "GeneralNameType_UniformResourceIdentifier", ResourceType = typeof(RS))]
    UniformResourceIdentifier = 6,

    /// <summary>
    /// IP 地址
    /// 标签：[7]
    /// IPv4 或 IPv6 地址，以八位字节序列编码。
    /// 用于指定证书可以保护的 IP 地址，适用于直接通过 IP 访问的服务。
    /// </summary>
    /// <example>
    /// IPv4: 192.168.1.1 (编码为 4 字节)
    /// IPv6: 2001:db8::1 (编码为 16 字节)
    /// IPv4 网段: 192.168.1.0/24 (编码为 8 字节：地址+掩码)
    /// </example>
    /// <remarks>
    /// <para>
    /// IPv4 地址使用 4 个八位字节，IPv6 地址使用 16 个八位字节。
    /// </para>
    /// <para>
    /// IP 地址范围可以通过附加网络掩码来表示（但实际中较少使用）。
    /// </para>
    /// </remarks>
    [Display(Name = "GeneralNameType_IPAddress", ResourceType = typeof(RS))]
    IPAddress = 7,

    /// <summary>
    /// 注册 ID（OID）
    /// 标签：[8]
    /// 对象标识符（Object Identifier），用于标识已注册的实体、组织或资源。
    /// OID 是全局唯一的分层标识符，广泛用于各种标准和协议中。
    /// </summary>
    /// <example>
    /// 1.2.840.113549 (RSA Security Inc.)
    /// 2.5.29.32.0 (anyPolicy)
    /// 1.3.6.1.4.1.311 (Microsoft)
    /// </example>
    /// <remarks>
    /// <para>
    /// OID 由 ISO/ITU-T 标准化，采用树状结构。
    /// </para>
    /// <para>
    /// OID 参考：<see href="http://www.oid-info.com/">OID Repository</see>
    /// </para>
    /// </remarks>
    [Display(Name = "GeneralNameType_RegisteredID", ResourceType = typeof(RS))]
    RegisteredID = 8
}