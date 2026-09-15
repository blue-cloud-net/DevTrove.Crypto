using DevTrove.Crypto.BouncyCastle.Asn1.X509;
using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.X509.Models;
using DevTrove.Crypto.X509.Utils;
using ExtendedKeyUsage = DevTrove.Crypto.X509.Enums.ExtendedKeyUsage;
using GeneralName = DevTrove.Crypto.X509.Models.GeneralName;
using KeyUsage = DevTrove.Crypto.X509.Enums.KeyUsage;

namespace DevTrove.Crypto.X509;

/// <summary>
/// X.509 证书签名请求（Certificate Signing Request, CSR）
/// CSR 是向 CA 申请证书时提交的请求文件，包含申请者的公钥和身份信息。
/// CA 验证 CSR 后会签发相应的数字证书。
/// 标准参考 PKCS#10 <see href="https://datatracker.ietf.org/doc/html/rfc2986"/>
/// </summary>
public class CertificateSigningRequest
{
    private readonly Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest _bcCsr;

    private readonly Org.BouncyCastle.Asn1.Pkcs.CertificationRequestInfo _bcCsrInfo;

    /// <summary>
    /// CSR 主题（Subject）
    /// 证书申请者的身份信息，包含 CN、O、OU、C 等字段。
    /// 主题信息将被包含在由 CA 签发的证书中。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc2986#section-4"/>
    /// </summary>
    public string Subject => _bcCsrInfo.Subject.ToString();

    /// <summary>
    /// 签名算法
    /// 用于对 CSR 进行签名的算法，如 SHA256withRSA、SHA256withECDSA 等。
    /// 申请者使用私钥对 CSR 签名，CA 使用对应的公钥验证签名，以确保 CSR 的真实性和完整性。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc2986#section-4.2"/>
    /// </summary>
    public DerObjectIdentifier SignatureAlgorithmOid => _bcCsr.SignatureAlgorithm.Algorithm;

    /// <summary>
    /// 签名算法
    /// 用于对 CSR 进行签名的算法，如 SHA256withRSA、SHA256withECDSA 等。
    /// 申请者使用私钥对 CSR 签名，CA 使用对应的公钥验证签名，以确保 CSR 的真实性和完整性。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc2986#section-4.2"/>
    /// </summary>
    public string SignatureAlgorithmName =>
        SignerUtilities.GetEncodingName(this.SignatureAlgorithmOid).Replace("-", string.Empty);

    /// <summary>
    /// 主题备用名称（Subject Alternative Names, SAN）
    /// CSR 中请求的备用标识名称，如多个域名、IP 地址、邮箱等。
    /// 这些信息通常作为扩展属性包含在 CSR 中，CA 可以选择将其包含在签发的证书中。
    /// 在现代 TLS 证书中，SAN 是指定多域名支持的标准方式。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.6"/>
    /// </summary>
    /// <example>
    /// SAN 示例：
    /// - DNS: www.example.com
    /// - DNS: *.example.com (通配符域名)
    /// - IP: 192.168.1.1
    /// - Email: admin@example.com
    /// </example>
    public IEnumerable<GeneralName>? SubjectAlternativeNames
    {
        get
        {
            var x509Extensions = _bcCsrInfo.GetX509Extensions();
            var bcGeneralNames = GeneralNames.FromExtensions(x509Extensions, X509Extensions.SubjectAlternativeName);
            return bcGeneralNames?.GetNames()?.Select(p => new GeneralName(p));
        }
    }

    /// <summary>
    /// 密钥用途（Key Usage）
    /// CSR 中请求的密钥用途扩展，定义证书公钥可用于哪些密码学操作。
    /// CA 在签发证书时可以采纳或修改这些用途。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.3"/>
    /// </summary>
    /// <example>
    /// 常见用途组合：
    /// - Web 服务器：DigitalSignature | KeyEncipherment
    /// - 代码签名：DigitalSignature | NonRepudiation
    /// - VPN 客户端：DigitalSignature | KeyAgreement
    /// </example>
    public KeyUsage? KeyUsages
    {
        get
        {
            var x509Extensions = _bcCsrInfo.GetX509Extensions();
            var bcKeyUsage = Org.BouncyCastle.Asn1.X509.KeyUsage.FromExtensions(x509Extensions);
            if (bcKeyUsage is null)
                return null;
            return KeyUsageHelper.FromBouncyCastleFormat(bcKeyUsage);
        }
    }

    /// <summary>
    /// 扩展密钥用途（Extended Key Usage, EKU）
    /// 进一步限定证书的使用场景和应用领域。
    /// 相比 Key Usage，EKU 提供更具体的应用层面的约束。
    /// 例如，指定证书可用于 TLS 服务器认证、客户端认证、代码签名、电子邮件保护等。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.12"/>
    /// </summary>
    /// <example>
    /// 常见 EKU 用途：
    /// - ServerAuthentication：TLS Web 服务器认证
    /// - ClientAuthentication：TLS Web 客户端认证
    /// - CodeSigning：代码签名
    /// - EmailProtection：电子邮件保护
    /// - TimeStamping：时间戳
    /// - OcspSigning：OCSP 签名
    /// </example>
    public ExtendedKeyUsage? ExtendedKeyUsages
    {
        get
        {
            var x509Extensions = _bcCsrInfo.GetX509Extensions();
            var bcExtendedKeyUsage = Org.BouncyCastle.Asn1.X509.ExtendedKeyUsage.FromExtensions(x509Extensions);
            if (bcExtendedKeyUsage is null)
                return null;
            return ExtendedKeyUsageHelper.FromBouncyCastleFormat(bcExtendedKeyUsage);
        }
    }


    /// <summary>
    /// 构造函数
    /// 从 BouncyCastle PKCS#10 证书签名请求对象创建 CertificateSigningRequest 实例。
    /// </summary>
    /// <param name="csr">BouncyCastle PKCS#10 证书签名请求对象，不能为 null</param>
    /// <exception cref="ArgumentNullException">当 csr 为 null 时抛出</exception>
    public CertificateSigningRequest(
        Pkcs10CertificationRequest csr)
    {
        _bcCsr = csr ?? throw new ArgumentNullException(nameof(csr));
        _bcCsrInfo = _bcCsr.GetCertificationRequestInfo();
    }

    /// <summary>
    /// 公钥对象
    /// 从 CSR 中提取的公钥，用于加密和签名验证。
    /// CA 将此公钥绑定到证书申请者的身份信息中。
    /// </summary>
    public AsymmetricPublicKeyParameter GetPublicKey() => new(_bcCsr.GetPublicKey());

    /// <summary>
    /// 验证 CSR 签名
    /// 使用 CSR 中包含的公钥验证其自身签名的有效性。
    /// 这确保 CSR 确实由声称的申请者创建，且内容未被篡改。
    /// 签名验证是 CA 处理 CSR 的第一步。
    /// </summary>
    /// <returns>如果签名验证成功返回 true，验证失败或发生异常返回 false</returns>
    public bool Verify() => _bcCsr.Verify();

    /// <summary>
    /// 验证 CSR 签名
    /// 使用指定的公钥验证 CSR 签名的有效性。
    /// 用于验证 CSR 是否由指定公钥对应的私钥签名。
    /// 这在 CSR 中的公钥与签名公钥不同的场景下很有用。
    /// </summary>
    /// <param name="publicKey">用于验证签名的公钥</param>
    /// <returns>如果签名验证成功返回 true，验证失败或发生异常返回 false</returns>
    public bool Verify(AsymmetricPublicKeyParameter publicKey) => _bcCsr.Verify(publicKey.GetBouncyCastleKey());

    /// <summary>
    /// 获取原始 BouncyCastle CSR 对象
    /// 提供对底层 BouncyCastle 实现的直接访问，用于需要使用 BouncyCastle 特定功能的场景。
    /// </summary>
    /// <returns>底层的 BouncyCastle Pkcs10CertificationRequest 对象</returns>
    public Pkcs10CertificationRequest GetBouncyCastleRequest() => _bcCsr;

    /// <summary>
    /// 导出为 PEM 格式
    /// 将 CSR 编码为 PEM（Privacy Enhanced Mail）格式。
    /// PEM 格式以 "-----BEGIN CERTIFICATE REQUEST-----" 开头，
    /// 以 "-----END CERTIFICATE REQUEST-----" 结尾。
    /// PEM 格式便于在文本环境中传输和存储，是提交给 CA 的标准格式。
    /// </summary>
    /// <returns>PEM 格式的 CSR 字符串</returns>
    public string ToPem()
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);

        pemWriter.WriteObject(_bcCsr);
        pemWriter.Writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// 导出为 DER 格式
    /// 将 CSR 编码为 DER（Distinguished Encoding Rules）格式。
    /// DER 是 ASN.1 的二进制编码方式，是 PKCS#10 的标准编码格式。
    /// </summary>
    /// <returns>DER 格式的 CSR 字节数组</returns>
    public byte[] ToDer() => _bcCsr.GetDerEncoded();

    /// <summary>
    /// 从 PEM 字符串加载 CSR
    /// 解析 PEM 格式的 CSR 字符串并创建 CertificateSigningRequest 对象。
    /// 支持标准的 "BEGIN CERTIFICATE REQUEST" 和旧式的 "BEGIN NEW CERTIFICATE REQUEST" 标记。
    /// 自动处理换行符和空白字符。
    /// </summary>
    /// <param name="pem">PEM 格式的 CSR 字符串</param>
    /// <returns>解析后的 CertificateSigningRequest 对象</returns>
    /// <exception cref="FormatException">当 PEM 格式无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当 CSR 数据无法解析时抛出</exception>
    public static CertificateSigningRequest FromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        var csr = pemReader.ReadObject() as Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest;
        if (csr is null)
        {
            throw new InvalidOperationException("Unable to parse the CSR from PEM.");
        }

        return new(csr);
    }

    /// <summary>
    /// 从 DER 字节数组加载 CSR
    /// 解析 DER 编码的 CSR 字节数组并创建 CertificateSigningRequest 对象。
    /// DER 是 PKCS#10 CSR 的标准二进制编码格式。
    /// </summary>
    /// <param name="der">DER 格式的 CSR 字节数组</param>
    /// <returns>解析后的 CertificateSigningRequest 对象</returns>
    /// <exception cref="ArgumentException">当 DER 数据无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当 CSR 数据无法解析时抛出</exception>
    public static CertificateSigningRequest FromDer(byte[] der)
    {
        var csr = new Pkcs10CertificationRequest(der);
        return new(csr);
    }

    /// <summary>
    /// 返回 CSR 的字符串表示形式
    /// 包含 CSR 的主题和签名算法等关键信息，便于日志记录和调试。
    /// </summary>
    /// <returns>格式化的 CSR 信息字符串</returns>
    public override string ToString() => $"Subject: {this.Subject}, Algorithm: {this.SignatureAlgorithmName}";

    /// <summary>
    /// 生成证书签名请求（CSR）
    /// 创建一个新的 CSR，包含指定的主体信息和公钥，使用私钥进行签名。
    /// CSR 通常提交给 CA 以申请数字证书。
    /// </summary>
    /// <param name="subjectDN">CSR 主体的可分辨名称</param>
    /// <param name="keyPair">密钥对，包含用于 CSR 的公钥和用于签名的私钥</param>
    /// <param name="signatureAlgorithm">签名算法，如 "SHA256WITHRSA"</param>
    /// <returns>生成的 CSR 对象</returns>
    public static CertificateSigningRequest Generate(
        string subjectDN,
        AsymmetricKeyPair keyPair,
        string signatureAlgorithm = "SHA256WITHRSA",
        X509ExtensionOptions? extensions = null) =>
        Generate(subjectDN, keyPair.PublicKey, keyPair.PrivateKey, signatureAlgorithm, extensions);

    /// <summary>
    /// 生成证书签名请求（CSR）
    /// 创建一个新的 CSR，包含指定的主体信息和公钥，使用私钥进行签名。
    /// CSR 通常提交给 CA 以申请数字证书。
    /// </summary>
    /// <param name="subjectDN">CSR 主体的可分辨名称</param>
    /// <param name="publicKey">公钥，将包含在 CSR 中</param>
    /// <param name="privateKey">私钥，用于对 CSR 进行签名</param>
    /// <param name="signatureAlgorithm">签名算法，如 "SHA256WITHRSA"</param>
    /// <returns>生成的 CSR 对象</returns>
    public static CertificateSigningRequest Generate(
        string subjectDN,
        AsymmetricPublicKeyParameter publicKey,
        AsymmetricPrivateKeyParameter privateKey,
        string signatureAlgorithm = "SHA256WITHRSA",
        X509ExtensionOptions? extensions = null)
    {
        var subject = X509NameParser.Parse(subjectDN);
        var bcPublicKey = publicKey.GetBouncyCastleKey();
        var bcPrivateKey = privateKey.GetBouncyCastleKey();

        // 创建 SubjectPublicKeyInfo
        var publicKeyInfo = Org.BouncyCastle.X509.SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(bcPublicKey);

        // 创建 CertificationRequestInfo（含可选扩展属性，经 extensionRequest 携带）
        CertificationRequestInfo csrInfo;
        if (extensions is not null)
        {
            var x509Extensions = X509ExtensionBuilder.BuildX509Extensions(extensions, bcPublicKey);
            var extensionRequest = new AttributePkcs(
                PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                new DerSet(x509Extensions));
            csrInfo = new CertificationRequestInfo(subject, publicKeyInfo, new DerSet(extensionRequest));
        }
        else
        {
            csrInfo = new CertificationRequestInfo(subject, publicKeyInfo, null);
        }

        // 对 CSR 信息进行签名
        var signatureAlgorithmOid = Org.BouncyCastle.Security.SignerUtilities.GetObjectIdentifier(signatureAlgorithm);
        var signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner(signatureAlgorithm);
        signer.Init(true, bcPrivateKey);

        var csrInfoBytes = csrInfo.GetDerEncoded();
        signer.BlockUpdate(csrInfoBytes, 0, csrInfoBytes.Length);
        var signature = signer.GenerateSignature();

        // 创建签名算法标识
        var sigAlgId = new AlgorithmIdentifier(signatureAlgorithmOid);

        // 创建 PKCS#10 CSR
        var certificationRequest = new CertificationRequest(csrInfo, sigAlgId, new DerBitString(signature));
        var csr = new Pkcs10CertificationRequest(certificationRequest.GetEncoded());

        return new CertificateSigningRequest(csr);
    }
}
