namespace DevTrove.Crypto.X509.Models;

/// <summary>
/// X.509 通用名称（GeneralName）
/// 表示证书扩展字段中的各种标识类型，如域名、IP 地址、电子邮件地址、URI 等。
/// 是证书主题备用名称（Subject Alternative Name, SAN）和颁发者备用名称（Issuer Alternative Name, IAN）的基本组成单元。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.6"/>
/// </summary>
/// <remarks>
/// <para>
/// GeneralName 提供了灵活的命名机制，允许证书包含多种格式的标识信息。
/// 在现代 TLS/SSL 证书中，SAN 扩展是指定服务器身份的主要方式，支持多域名证书和通配符证书。
/// </para>
/// <para>
/// <strong>常见用法：</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>DnsName</strong>：指定证书可以保护的域名，如 www.example.com、*.example.com</description></item>
/// <item><description><strong>IPAddress</strong>：指定证书可以保护的 IP 地址，适用于直接 IP 访问的服务</description></item>
/// <item><description><strong>Rfc822Name</strong>：电子邮件地址，用于 S/MIME 邮件加密</description></item>
/// <item><description><strong>UniformResourceIdentifier</strong>：URL，如 OCSP 响应器地址、CRL 分发点</description></item>
/// </list>
/// <para>
/// <strong>重要说明：</strong>浏览器和客户端在验证 TLS 证书时，优先检查 SAN 扩展中的 DNS 名称，
/// 证书主题（Subject）中的 CN（Common Name）字段已逐渐被弃用。
/// </para>
/// </remarks>
public record struct GeneralName
{
    /// <summary>
    /// 通用名称类型
    /// 指示此 GeneralName 所使用的命名格式。
    /// </summary>
    /// <value>
    /// 支持的类型包括：DNS 名称、IP 地址、电子邮件地址、URI、目录名称等。
    /// 详见 <see cref="GeneralNameType"/>
    /// </value>
    public GeneralNameType Type { get; }

    /// <summary>
    /// 名称值
    /// 根据 <see cref="Type"/> 的不同，值的格式也不同。
    /// </summary>
    /// <value>
    /// <list type="table">
    /// <listheader>
    /// <term>类型</term>
    /// <description>值格式示例</description>
    /// </listheader>
    /// <item>
    /// <term>DnsName</term>
    /// <description>www.example.com, *.example.com</description>
    /// </item>
    /// <item>
    /// <term>IPAddress</term>
    /// <description>192.168.1.1, 2001:db8::1</description>
    /// </item>
    /// <item>
    /// <term>Rfc822Name</term>
    /// <description>user@example.com</description>
    /// </item>
    /// <item>
    /// <term>UniformResourceIdentifier</term>
    /// <description>http://crl.example.com/ca.crl</description>
    /// </item>
    /// <item>
    /// <term>DirectoryName</term>
    /// <description>CN=Example CA,O=Example Inc,C=US</description>
    /// </item>
    /// </list>
    /// </value>
    public string Value { get; }

    /// <summary>
    /// 使用指定的类型和值创建 GeneralName 实例
    /// </summary>
    /// <param name="type">通用名称类型</param>
    /// <param name="value">名称值，格式应符合指定类型的要求</param>
    /// <example>
    /// <code>
    /// // 创建 DNS 名称
    /// var dnsName = new GeneralName(GeneralNameType.DnsName, "www.example.com");
    /// 
    /// // 创建 IP 地址
    /// var ipAddress = new GeneralName(GeneralNameType.IPAddress, "192.168.1.1");
    /// 
    /// // 创建电子邮件地址
    /// var email = new GeneralName(GeneralNameType.Rfc822Name, "admin@example.com");
    /// </code>
    /// </example>
    public GeneralName(
        GeneralNameType type, string value)
    {
        this.Type = type;
        this.Value = value;
    }

    /// <summary>
    /// 从 BouncyCastle GeneralName 对象创建 GeneralName 实例
    /// 用于从证书扩展中解析 GeneralName 数据。
    /// </summary>
    /// <param name="bcGeneralName">BouncyCastle GeneralName 对象</param>
    /// <remarks>
    /// 此构造函数将 BouncyCastle 的内部表示转换为更易用的强类型模型。
    /// TagNo 对应 ASN.1 CHOICE 标签，直接映射到 <see cref="GeneralNameType"/> 枚举值。
    /// </remarks>
    public GeneralName(Org.BouncyCastle.Asn1.X509.GeneralName bcGeneralName)
    {
        this.Type = (GeneralNameType)bcGeneralName.TagNo;
        this.Value = DecodeValue(bcGeneralName);
    }

    /// <summary>
    /// 转换为 BouncyCastle GeneralName 对象
    /// 用于构建证书扩展或 CSR 时需要原始 BouncyCastle 对象的场景。
    /// </summary>
    /// <returns>等效的 BouncyCastle GeneralName 对象</returns>
    /// <remarks>
    /// 返回的对象可用于 BouncyCastle 的证书生成器、CSR 生成器等 API。
    /// </remarks>
    public Org.BouncyCastle.Asn1.X509.GeneralName GetBouncyCastleGeneralName()
    {
        if (this.Type == GeneralNameType.IPAddress)
        {
            // 将 "192.168.1.100" 编码为 DER OctetString，符合 RFC 5280 IP 地址表示。
            var ip = System.Net.IPAddress.Parse(this.Value);
            return new Org.BouncyCastle.Asn1.X509.GeneralName(
                Org.BouncyCastle.Asn1.X509.GeneralName.IPAddress,
                new DerOctetString(ip.GetAddressBytes()));
        }

        return new Org.BouncyCastle.Asn1.X509.GeneralName((int)this.Type, this.Value);
    }

    /// <summary>
    /// 解码 BouncyCastle GeneralName 值；IP 地址（DER OctetString，形如 "#c0a80164"）转为点分十进制。
    /// </summary>
    private static string DecodeValue(Org.BouncyCastle.Asn1.X509.GeneralName name)
    {
        if (name.TagNo == Org.BouncyCastle.Asn1.X509.GeneralName.IPAddress)
        {
            if (name.Name is Asn1OctetString octets)
            {
                return new System.Net.IPAddress(octets.GetOctets()).ToString();
            }
        }

        var text = name.Name?.ToString();
        if (!string.IsNullOrEmpty(text) && text.StartsWith('#'))
        {
            try
            {
                return new System.Net.IPAddress(Convert.FromHexString(text[1..])).ToString();
            }
            catch (FormatException)
            {
                // 无法解码时回退到原始文本。
            }
        }

        return text ?? string.Empty;
    }

    /// <summary>
    /// 返回 GeneralName 的字符串表示形式
    /// 格式为 "类型: 值"，便于日志记录和调试。
    /// </summary>
    /// <returns>格式化的字符串，如 "DnsName: www.example.com" 或 "IPAddress: 192.168.1.1"</returns>
    public override string ToString() => $"{this.Type}: {this.Value}";
}
