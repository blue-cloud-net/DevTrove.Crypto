using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.X509.Models;
using DevTrove.Crypto.X509.Utils;
using CertificateRevocationReason = DevTrove.Crypto.X509.Enums.CertificateRevocationReason;

namespace DevTrove.Crypto.X509;

/// <summary>
/// X.509 证书吊销列表（Certificate Revocation List, CRL）
/// CRL 是由 CA 发布的已吊销证书的列表，用于告知证书使用者哪些证书不再可信。
/// 包含吊销证书的序列号、吊销时间和吊销原因等信息。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5"/>
/// </summary>
public class CertificateRevocationList
{
    private readonly Org.BouncyCastle.X509.X509Crl _bcCrl;

    /// <summary>
    /// CRL 版本
    /// X.509 v1 = 0, v2 = 1。v2 CRL 支持扩展字段，如撤销原因等。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.1.2.1"/>
    /// </summary>
    public int Version => _bcCrl.Version;

    /// <summary>
    /// CRL颁发者
    /// 签发此 CRL 的 CA 的身份信息。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.1.2.3"/>
    /// </summary>
    public string Issuer => _bcCrl.IssuerDN.ToString();

    /// <summary>
    /// CRL颁发者
    /// 签发此CRL的CA的身份信息。
    /// 以结构化方式访问颁发者的各个组成部分。
    /// </summary>
    public X509DistinguishedName IssuerDN => new(_bcCrl.IssuerDN);

    /// <summary>
    /// CRL 发布时间（This Update）
    /// 本次 CRL 的发布时间。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.1.2.4"/>
    /// </summary>
    public DateTime ThisUpdate => _bcCrl.ThisUpdate;

    /// <summary>
    /// CRL 下次更新时间（Next Update）
    /// 下次 CRL 的预计发布时间，在此时间之后应获取新的 CRL。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.1.2.5"/>
    /// </summary>
    public DateTime? NextUpdate => _bcCrl.NextUpdate;

    /// <summary>
    /// 签名算法 OID
    /// 用于签名此 CRL 的算法 OID 字符串。
    /// </summary>
    public string SignatureAlgorithmOid => _bcCrl.SigAlgOid;

    /// <summary>
    /// 签名算法
    /// 用于签名此 CRL 的算法，如 SHA256withRSA、SHA256withECDSA 等。
    /// 确保 CRL 的完整性和真实性。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.1.1.2"/>
    /// </summary>
    public string SignatureAlgorithmName => _bcCrl.SigAlgName.Replace("-", String.Empty);

    /// <summary>
    /// 撤销的证书数量
    /// CRL 中包含的已撤销证书条目总数。
    /// 大型 CA 的 CRL 可能包含数千甚至数万条记录。
    /// </summary>
    public long? RevokedCertificatesCount => _bcCrl.GetRevokedCertificates()?.Count;

    /// <summary>
    /// 撤销的证书列表
    /// 返回 CRL 中所有已撤销证书的详细信息。
    /// 包括证书序列号、撤销时间和撤销原因等。
    /// 使用迭代器模式，适合处理大型 CRL。
    /// </summary>
    public IEnumerable<RevokedCertificateInfo>? RevokedCertificates =>
        _bcCrl.GetRevokedCertificates()?.Select(entry => new RevokedCertificateInfo(entry));

    /// <summary>
    /// 构造函数
    /// 从 BouncyCastle X509 CRL 对象创建 CertificateRevocationList 实例。
    /// </summary>
    /// <param name="crl">BouncyCastle X509 CRL 对象，不能为 null</param>
    /// <exception cref="ArgumentNullException">当 crl 为 null 时抛出</exception>
    public CertificateRevocationList(X509Crl crl)
    {
        _bcCrl = crl ?? throw new ArgumentNullException(nameof(crl));
    }

    /// <summary>
    /// CRL 是否已过期
    /// 检查当前时间是否已超过 NextUpdate 时间。
    /// 过期的 CRL 不应再被信任，应获取最新的 CRL。
    /// </summary>
    public bool IsExpired() => this.NextUpdate.HasValue && this.NextUpdate.Value < DateTime.UtcNow;

    /// <summary>
    /// 检查证书是否被撤销
    /// 根据证书对象检查其是否在此 CRL 的撤销列表中。
    /// 这是验证证书状态的关键步骤。
    /// </summary>
    /// <param name="certificate">要检查的证书</param>
    /// <returns>如果证书在撤销列表中返回 true，否则返回 false</returns>
    /// <exception cref="ArgumentNullException">当 certificate 为 null 时抛出</exception>
    public bool IsRevoked(Certificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        var bcCert = certificate.GetBouncyCastleCertificate();
        return _bcCrl.IsRevoked(bcCert);
    }

    /// <summary>
    /// 检查证书是否被撤销（通过序列号）
    /// 根据证书序列号检查其是否在此 CRL 的撤销列表中。
    /// 当只有序列号信息时使用此方法。
    /// </summary>
    /// <param name="serialNumber">证书序列号（十六进制字符串，不区分大小写）</param>
    /// <returns>如果序列号在撤销列表中返回 true，否则返回 false</returns>
    public bool IsRevoked(string serialNumber)
    {
        ArgumentNullException.ThrowIfNull(serialNumber);

        var revokedCerts = _bcCrl.GetRevokedCertificates();
        if (revokedCerts == null)
            return false;

        // 归一化：去掉前导零，避免与 BigInteger.ToString(16)（不带前导零）比较失配。
        // 例如 CRL 序列号 0x0F0F 对应 "f0f"，输入 "0F0F" 应匹配。
        var normalized = serialNumber.TrimStart('0');
        if (normalized.Length == 0)
        {
            normalized = "0";
        }

        foreach (var entry in revokedCerts)
        {
            if (entry.SerialNumber.ToString(16).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 获取撤销证书的详细信息
    /// 返回证书的撤销时间、撤销原因等详细信息。
    /// 用于审计和日志记录。
    /// </summary>
    /// <param name="certificate">要查询的证书</param>
    /// <returns>撤销证书信息对象，如果证书未被撤销则返回 null</returns>
    /// <exception cref="ArgumentNullException">当 certificate 为 null 时抛出</exception>
    public RevokedCertificateInfo? GetRevokedCertificate(Certificate certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        var bcCert = certificate.GetBouncyCastleCertificate();
        var entry = _bcCrl.GetRevokedCertificate(bcCert.SerialNumber);

        return entry != null ? new RevokedCertificateInfo(entry) : null;
    }

    /// <summary>
    /// 验证 CRL 签名
    /// 使用颁发者证书的公钥验证 CRL 签名的有效性。
    /// 这确保 CRL 确实由声称的 CA 签发且未被篡改。
    /// 验证 CRL 签名是使用 CRL 前的必要步骤。
    /// </summary>
    /// <param name="issuerCertificate">颁发者证书，包含用于验证签名的公钥</param>
    /// <returns>如果签名验证成功返回 true，验证失败或发生异常返回 false</returns>
    public bool VerifySignature(Certificate issuerCertificate) =>
        _bcCrl.IsSignatureValid(issuerCertificate.GetBouncyCastleCertificate().GetPublicKey());

    /// <summary>
    /// 获取原始 BouncyCastle CRL 对象
    /// 提供对底层 BouncyCastle 实现的直接访问，用于需要使用 BouncyCastle 特定功能的场景。
    /// </summary>
    /// <returns>底层的 BouncyCastle X509Crl 对象</returns>
    public Org.BouncyCastle.X509.X509Crl GetBouncyCastleCrl() => _bcCrl;

    /// <summary>
    /// 导出为 PEM 格式
    /// 将 CRL 编码为 PEM（Privacy Enhanced Mail）格式。
    /// PEM 格式以 "-----BEGIN X509 CRL-----" 开头，
    /// 以 "-----END X509 CRL-----" 结尾。
    /// </summary>
    /// <returns>PEM 格式的 CRL 字符串</returns>
    public string ToPem()
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);

        pemWriter.WriteObject(_bcCrl);
        pemWriter.Writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// 导出为 DER 格式
    /// 将 CRL 编码为 DER（Distinguished Encoding Rules）格式。
    /// DER 是 ASN.1 的二进制编码方式，是 X.509 CRL 的标准编码格式。
    /// </summary>
    /// <returns>DER 格式的 CRL 字节数组</returns>
    public byte[] ToDer() => _bcCrl.GetEncoded();

    /// <summary>
    /// 从 PEM 字符串加载 CRL
    /// 解析 PEM 格式的 CRL 字符串并创建 CertificateRevocationList 对象。
    /// 自动处理换行符和空白字符。
    /// </summary>
    /// <param name="pem">PEM 格式的 CRL 字符串</param>
    /// <returns>解析后的 CertificateRevocationList 对象</returns>
    /// <exception cref="FormatException">当 PEM 格式无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当 CRL 数据无法解析时抛出</exception>
    public static CertificateRevocationList FromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        var crl = pemReader.ReadObject() as Org.BouncyCastle.X509.X509Crl;
        if (crl is null)
        {
            throw new InvalidOperationException("Unable to parse the CRL from PEM.");
        }
        return new(crl);
    }

    /// <summary>
    /// 从 DER 字节数组加载 CRL
    /// 解析 DER 编码的 CRL 字节数组并创建 CertificateRevocationList 对象。
    /// DER 是 X.509 CRL 的标准二进制编码格式。
    /// </summary>
    /// <param name="der">DER 格式的 CRL 字节数组</param>
    /// <returns>解析后的 CertificateRevocationList 对象</returns>
    /// <exception cref="ArgumentException">当 DER 数据无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当 CRL 数据无法解析时抛出</exception>
    public static CertificateRevocationList FromDer(byte[] der)
    {
        var parser = new X509CrlParser();
        var crl = parser.ReadCrl(der);
        return new CertificateRevocationList(crl);
    }

    /// <summary>
    /// 返回 CRL 的字符串表示形式
    /// 包含 CRL 的颁发者、发布时间和撤销证书数量等关键信息，便于日志记录和调试。
    /// </summary>
    /// <returns>格式化的 CRL 信息字符串</returns>
    public override string ToString()
    {
        return
            $"CRL Issuer: {this.Issuer}, This Update: {this.ThisUpdate:yyyy-MM-dd}, Revoked: {this.RevokedCertificatesCount}";
    }

    /// <summary>
    /// 生成证书吊销列表（CRL）
    /// 创建一个新的 CRL，包含指定的已撤销证书列表。
    /// CRL 由 CA 定期发布，用于通知证书使用者哪些证书已被吊销。
    /// </summary>
    /// <param name="issuerDN">CRL 颁发者的可分辨名称（通常是 CA 的 DN）</param>
    /// <param name="caPrivateKey">CA 的私钥，用于对 CRL 进行签名</param>
    /// <param name="revokedCertificates">已撤销证书列表，包含序列号、撤销时间和原因</param>
    /// <param name="thisUpdate">CRL 发布时间</param>
    /// <param name="nextUpdate">CRL 下次更新时间，可为 null</param>
    /// <param name="signatureAlgorithm">签名算法，如 "SHA256WITHRSA"</param>
    /// <returns>生成的 CRL 对象</returns>
    public static CertificateRevocationList Generate(
        string issuerDN,
        AsymmetricPrivateKeyParameter caPrivateKey,
        List<(string SerialNumber, DateTime RevocationDate, CertificateRevocationReason? Reason)> revokedCertificates,
        DateTime thisUpdate,
        DateTime? nextUpdate,
        string signatureAlgorithm = "SHA256WITHRSA")
    {
        var issuer = X509NameParser.Parse(issuerDN);
        var crlGen = new X509V2CrlGenerator();

        crlGen.SetIssuerDN(issuer);
        crlGen.SetThisUpdate(thisUpdate);
        if (nextUpdate.HasValue)
        {
            crlGen.SetNextUpdate(nextUpdate.Value);
        }

        // 添加撤销的证书
        foreach (var (serialNumber, revocationDate, reason) in revokedCertificates)
        {
            var serial = new Org.BouncyCastle.Math.BigInteger(serialNumber, 16);
            crlGen.AddCrlEntry(serial, revocationDate, (int)(reason ?? CertificateRevocationReason.Unspecified));
        }

        var signatureFactory = new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory(
            signatureAlgorithm,
            caPrivateKey.GetBouncyCastleKey(),
            new Org.BouncyCastle.Security.SecureRandom());

        var crl = crlGen.Generate(signatureFactory);
        return new CertificateRevocationList(crl);
    }
}
