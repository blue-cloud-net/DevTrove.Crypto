namespace DevTrove.Crypto.X509.Utils;

/// <summary>
/// X.509 可分辨名称（Distinguished Name）解析工具。
/// 兼容 OpenSSL 斜杠风格（如 "/C=CN/O=Org/CN=example.com"）与 RFC 2253 逗号风格（如 "C=CN,O=Org,CN=example.com"）。
/// </summary>
public static class X509NameParser
{
    /// <summary>
    /// 将可分辨名称字符串解析为 BouncyCastle <see cref="Org.BouncyCastle.Asn1.X509.X509Name"/>。
    /// </summary>
    /// <param name="distinguishedName">可分辨名称字符串。</param>
    /// <returns>解析后的 <see cref="Org.BouncyCastle.Asn1.X509.X509Name"/>。</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="distinguishedName"/> 为 <see langword="null"/> 时抛出。
    /// </exception>
    /// <exception cref="ArgumentException">名称格式非法时抛出。</exception>
    public static Org.BouncyCastle.Asn1.X509.X509Name Parse(string distinguishedName)
    {
        ArgumentNullException.ThrowIfNull(distinguishedName);
        if (string.IsNullOrWhiteSpace(distinguishedName))
            throw new ArgumentException("Distinguished name cannot be empty.", nameof(distinguishedName));

        var dn = distinguishedName.Trim();
        if (dn.StartsWith('/'))
        {
            // OpenSSL 风格：/C=CN/O=Org/CN=example.com → C=CN,O=Org,CN=example.com
            dn = string.Join(',', dn.Split('/').Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        return new Org.BouncyCastle.Asn1.X509.X509Name(dn);
    }
}
