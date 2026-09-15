namespace DevTrove.Crypto.BouncyCastle.Asn1.X509;

/// <summary>
/// CertificationRequestInfo 扩展方法
/// 提供从 CSR 的 CertificationRequestInfo 中提取 X509Extensions 的功能。
/// </summary>
public static class CertificationRequestInfoExtensions
{
    /// <summary>
    /// 从 CertificationRequestInfo 中提取 X509Extensions
    /// 根据 PKCS#10 标准，扩展信息存储在 attributes 中的扩展请求属性内。
    /// </summary>
    /// <param name="certificationRequestInfo">证书请求信息对象</param>
    /// <returns>X509Extensions 对象，如果不存在则返回 null</returns>
    /// <remarks>
    /// 扩展请求属性的 OID 为 1.2.840.113549.1.9.14 (pkcs-9-at-extensionRequest)
    /// 根据 PKCS#10 标准，扩展请求属性只能有一个值（SINGLE VALUE TRUE）
    /// </remarks>
    public static X509Extensions? GetX509Extensions(this CertificationRequestInfo certificationRequestInfo)
    {
        if (certificationRequestInfo?.Attributes == null)
        {
            return null;
        }

        try
        {
            foreach (var obj in certificationRequestInfo.Attributes)
            {
                // 属性元素为原始 ASN.1 结构（DLSequence），需经 GetInstance 转换。
                var attribute = AttributePkcs.GetInstance(obj);

                // 检查是否为扩展请求属性 (OID: 1.2.840.113549.1.9.14)
                if (attribute.AttrType.Equals(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest))
                {
                    // 根据 PKCS#10 标准，扩展请求属性只有一个值
                    if (attribute.AttrValues.Count > 0)
                    {
                        return X509Extensions.GetInstance(attribute.AttrValues[0]);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not InvalidCastException)
        {
            // 解析失败视为无扩展。
        }

        return null;
    }
}
