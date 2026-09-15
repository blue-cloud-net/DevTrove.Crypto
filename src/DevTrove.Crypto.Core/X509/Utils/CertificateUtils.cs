namespace DevTrove.Crypto.X509.Utils;

/// <summary>
/// X.509 证书工具类
/// </summary>
public static class CertificateUtils
{
    /// <summary>
    /// 从 PEM 格式的字符串中读取并拆分证书链
    /// </summary>
    /// <param name="pem">PEM 格式的证书字符串，可以包含一个或多个证书</param>
    /// <returns>证书集合，按照在 PEM 文件中的顺序返回（通常为：叶子证书 → 中间证书 → 根证书）</returns>
    /// <exception cref="InvalidOperationException">当 PEM 对象不是有效的 X.509 证书时抛出</exception>
    /// <remarks>
    /// 此方法使用 Bouncy Castle 的 PemReader 逐个解析 PEM 格式的证书对象。
    /// 支持解析包含多个证书的证书链文件，每个证书会被包装为 Certificate 对象返回。
    /// </remarks>
    /// <example>
    /// <code>
    /// string pemChain = File.ReadAllText("chain.pem");
    /// var certificates = CertificateUtils.ReadCertificatesFromPem(pemChain);
    /// foreach (var cert in certificates)
    /// {
    ///     Console.WriteLine($"Subject: {cert.SubjectDN}");
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<Certificate> ReadCertificatesFromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);

        // 循环读取 PEM 文件中的所有对象
        while (pemReader.ReadObject() is { } pemObj)
        {
            // 尝试将 PEM 对象转换为 X509Certificate
            var cert = pemObj as Org.BouncyCastle.X509.X509Certificate;
            if (cert is null)
            {
                throw new InvalidOperationException("Unable to parse the certificate from PEM.");
            }

            // 包装为自定义 Certificate 对象并返回
            yield return new Certificate(cert);
        }
    }
}
