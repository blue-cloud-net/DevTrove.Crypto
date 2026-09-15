namespace DevTrove.Crypto.BouncyCastle.ObjectIdentifiers;

/// <summary>
/// 扩展密钥用途 OID 常量
/// </summary>
public static class ExtendedKeyUsageObjectIdentifiers
{
    /// <summary>
    /// SSH 客户端 (1.3.6.1.5.5.7.3.21)
    /// </summary>
    public static readonly DerObjectIdentifier SshClient = X509ObjectIdentifiers.IdPkix.Branch("3.21");

    /// <summary>
    /// SSH 服务器 (1.3.6.1.5.5.7.3.22)
    /// </summary>
    public static readonly DerObjectIdentifier SshServer = X509ObjectIdentifiers.IdPkix.Branch("3.22");

    /// <summary>
    /// 文档签名 (1.3.6.1.4.1.311.10.3.12)
    /// </summary>
    public static readonly DerObjectIdentifier DocumentSigning = new("1.3.6.1.4.1.311.10.3.12");
}
