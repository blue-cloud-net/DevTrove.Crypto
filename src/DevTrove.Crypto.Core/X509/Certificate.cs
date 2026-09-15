using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.X509.Models;
using DevTrove.Crypto.X509.Utils;
using ExtendedKeyUsage = DevTrove.Crypto.X509.Enums.ExtendedKeyUsage;
using GeneralName = DevTrove.Crypto.X509.Models.GeneralName;
using KeyUsage = DevTrove.Crypto.X509.Enums.KeyUsage;

namespace DevTrove.Crypto.X509;

/// <summary>
/// X.509 数字证书
/// X.509 证书是一种用于身份认证和密钥分发的数字证书标准。
/// 证书包含公钥、主体信息、颁发者信息、有效期、签名等重要信息。
/// 广泛应用于 TLS/SSL、代码签名、电子邮件加密等场景。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280"/>
/// </summary>
public class Certificate
{
    private readonly Org.BouncyCastle.X509.X509Certificate _bcCertificate;

    /// <summary>
    /// 证书版本号
    /// X.509 v1 = 0, v2 = 1, v3 = 2（最常用）。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.1"/>
    /// </summary>
    public int Version => _bcCertificate.Version;

    /// <summary>
    /// 证书序列号
    /// 由 CA 分配的唯一标识符，在同一 CA 下必须唯一。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.2"/>
    /// </summary>
    public string SerialNumber => _bcCertificate.SerialNumber.ToString(16).ToUpper();

    /// <summary>
    /// 签名算法Oid
    /// 用于签名证书的算法，如 SHA256withRSA、SHA256withECDSA、SM3withSM2 等。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.1.2"/>
    /// </summary>
    public DerObjectIdentifier SignatureAlgorithmOid => new(_bcCertificate.SigAlgOid);

    /// <summary>
    /// 签名算法名称
    /// 用于签名证书的算法，如 SHA256withRSA、SHA256withECDSA、SM3withSM2 等。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.1.2"/>
    /// </summary>
    public string SignatureAlgorithmName => _bcCertificate.SigAlgName.Replace("-", String.Empty);

    /// <summary>
    /// 证书颁发者（Issuer）
    /// 签发该证书的 CA（证书颁发机构）的身份信息。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.4"/>
    /// </summary>
    public string Issuer => _bcCertificate.IssuerDN.ToString();

    /// <summary>
    /// 证书颁发者（Issuer）详细信息
    /// 以结构化方式访问颁发者的各个组成部分。
    /// </summary>
    public X509DistinguishedName IssuerDN => new(_bcCertificate.IssuerDN);

    /// <summary>
    /// 证书主题（Subject）
    /// 证书所有者的身份信息，包含 CN、O、OU、C 等字段。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.6"/>
    /// </summary>
    public string Subject => _bcCertificate.SubjectDN.ToString();

    /// <summary>
    /// 证书主题（Subject）详细信息
    /// 以结构化方式访问主题的各个组成部分。
    /// </summary>
    public X509DistinguishedName SubjectDN => new(_bcCertificate.SubjectDN);

    /// <summary>
    /// 证书生效时间（Not Before）
    /// 证书有效期的开始时间，在此时间之前证书不可用。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.5"/>
    /// </summary>
    public DateTime NotBefore => _bcCertificate.NotBefore;

    /// <summary>
    /// 证书过期时间（Not After）
    /// 证书有效期的结束时间，在此时间之后证书不再有效。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.5"/>
    /// </summary>
    public DateTime NotAfter => _bcCertificate.NotAfter;

    /// <summary>
    /// 证书指纹（SHA-256）
    /// 证书内容的 SHA-256 哈希值的十六进制表示，用于快速识别和比对证书。
    /// 推荐使用 <see cref="ComputeFingerprint"/> 方法来获取其他算法的指纹或有更多控制。
    /// </summary>
    public string Sha256Thumbprint => this.ComputeFingerprint("SHA-256");

    /// <summary>
    /// 是否为 CA 证书
    /// 指示该证书是否具有签发其他证书的权限。
    /// CA 证书的 Basic Constraints 扩展中 cA 字段设置为 TRUE。
    /// 只有 CA 证书才能用于签发和验证其他证书。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.9"/>
    /// </summary>
    public bool IsCertificateAuthority => _bcCertificate.GetBasicConstraints() >= 0;

    /// <summary>
    /// CA 证书的路径长度约束（Path Length Constraint）
    /// 指示 CA 证书可以签发的下级 CA 证书的最大层级数。
    /// 如果为 null，表示没有路径长度限制或该证书不是 CA 证书。
    /// 如果为 0，表示该 CA 只能签发终端实体证书，不能签发下级 CA 证书。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.9"/>
    /// </summary>
    public int? PathLengthConstraint =>
        _bcCertificate.GetBasicConstraints() >= 0
            ? _bcCertificate.GetBasicConstraints() == int.MaxValue
                ? null
                : _bcCertificate.GetBasicConstraints()
            : null;

    /// <summary>
    /// 主题备用名称（Subject Alternative Names, SAN）
    /// 证书可以使用的其他标识名称，补充主题 CN 字段。
    /// 常用于指定多个域名、IP 地址、电子邮件地址等。
    /// 在现代 TLS 中，浏览器主要依赖 SAN 而非 CN 进行域名验证。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.6"/>
    /// </summary>
    /// <example>
    /// SAN 示例：
    /// - DNS: www.example.com
    /// - DNS: example.com  
    /// - IP: 192.168.1.1
    /// - Email: admin@example.com
    /// </example>
    public IEnumerable<GeneralName>? SubjectAlternativeNames =>
        _bcCertificate
            .GetSubjectAlternativeNameExtension()
            ?.GetNames()
            ?.Select(name => new GeneralName(name));

    /// <summary>
    /// 密钥用途（Key Usage）
    /// 定义证书公钥可以用于哪些密码学操作。
    /// 这是一个关键的安全约束，限制证书的使用范围，防止证书滥用。
    /// 例如，用于 TLS 服务器的证书通常包含 DigitalSignature 和 KeyEncipherment，
    /// 而 CA 证书必须包含 KeyCertSign。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.3"/>
    /// </summary>
    /// <example>
    /// 常见用途组合：
    /// - TLS 服务器：DigitalSignature | KeyEncipherment
    /// - TLS 客户端：DigitalSignature
    /// - CA 证书：KeyCertSign | CrlSign
    /// - 代码签名：DigitalSignature
    /// </example>
    public KeyUsage KeyUsages =>
        KeyUsageHelper.FromBoolArray(_bcCertificate.GetKeyUsage());

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
    public ExtendedKeyUsage ExtendedKeyUsages =>
        ExtendedKeyUsageHelper.FromOids(_bcCertificate.GetExtendedKeyUsage());

    /// <summary>
    /// 扩展密钥用途 OID 列表
    /// 获取证书中包含的所有扩展密钥用途的 OID 字符串。
    /// 包括标准 OID 和自定义 OID。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.12"/>
    /// </summary>
    public IList<DerObjectIdentifier> ExtendedKeyUsageOids => _bcCertificate.GetExtendedKeyUsage();

    /// <summary>
    /// 主题密钥标识符（Subject Key Identifier, SKI）
    /// 提供证书公钥的唯一标识符，通常是公钥的哈希值。
    /// 用于在证书链验证时快速识别和匹配证书，特别是在构建证书路径时。
    /// 便于区分同一主体的不同证书（如密钥更新后的新证书）。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.2"/>
    /// </summary>
    public string? SubjectKeyIdentifier
    {
        get
        {
            var skiAsn1Obj = _bcCertificate.GetExtensionParsedValue(X509Extensions.SubjectKeyIdentifier);
            if (skiAsn1Obj is null)
                return null;

            var ski = Org.BouncyCastle.Asn1.X509.SubjectKeyIdentifier.GetInstance(skiAsn1Obj);
            return Convert.ToHexString(ski.GetKeyIdentifier());
        }
    }

    /// <summary>
    /// 颁发机构密钥标识符（Authority Key Identifier, AKI）
    /// 标识签发该证书的 CA 证书的公钥。
    /// 包含 CA 证书的 SKI，用于在证书链验证时快速定位颁发者证书。
    /// 对于构建和验证证书链至关重要，特别是当 CA 有多个证书时。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.1"/>
    /// </summary>
    public string? AuthorityKeyIdentifier
    {
        get
        {
            var akiExtension = _bcCertificate.GetExtensionValue(X509Extensions.AuthorityKeyIdentifier);
            if (akiExtension is not null)
            {
                var asn1Object = X509ExtensionUtilities.FromExtensionValue(akiExtension);
                var aki = Org.BouncyCastle.Asn1.X509.AuthorityKeyIdentifier.GetInstance(asn1Object);
                var keyId = aki.GetKeyIdentifier();
                return keyId is null ? null : Convert.ToHexString(keyId);
            }
            return null;
        }
    }

    /// <summary>
    /// CRL 分发点（CRL Distribution Points）
    /// 指定证书吊销列表（CRL）的下载位置（URL）。
    /// 客户端可以从这些位置下载 CRL 来检查证书是否已被吊销。
    /// 通常包含 HTTP 或 LDAP URL。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.13"/>
    /// </summary>
    /// <example>
    /// 示例 URL：
    /// - http://crl.example.com/example.crl
    /// - ldap://ldap.example.com/cn=Example%20CA,dc=example,dc=com?certificateRevocationList
    /// </example>
    public CrlDistPoint? CrlDistributionPointsInstance
    {
        get
        {
            var crlDpExtension = _bcCertificate.GetExtensionValue(X509Extensions.CrlDistributionPoints);
            if (crlDpExtension is null)
                return null;

            var asn1Object = X509ExtensionUtilities.FromExtensionValue(crlDpExtension);
            var crlDistPoint = CrlDistPoint.GetInstance(asn1Object);
            return crlDistPoint;
        }
    }

    public IEnumerable<string>? CrlDistributionPointUrls
    {
        get
        {
            if (this.CrlDistributionPointsInstance is null)
                return null;

            var urls = new HashSet<string>();
            foreach (var dp in this.CrlDistributionPointsInstance.GetDistributionPoints())
            {
                if (dp.DistributionPointName?.Type == DistributionPointName.FullName)
                {
                    var generalNames = GeneralNames.GetInstance(dp.DistributionPointName.Name);
                    foreach (var name in generalNames.GetNames())
                    {
                        if (name.TagNo == Org.BouncyCastle.Asn1.X509.GeneralName.UniformResourceIdentifier)
                        {
                            var uri = name.Name.ToString();
                            if (!String.IsNullOrWhiteSpace(uri))
                            {
                                urls.Add(uri);
                            }
                        }
                    }
                }
            }

            return urls;
        }
    }

    /// <summary>
    /// 颁发机构信息访问（Authority Information Access, AIA）
    /// 提供获取颁发者证书和 OCSP 服务的位置信息。
    /// 包含两种主要类型：
    /// - CA Issuers：用于下载颁发者（CA）证书，帮助构建证书链
    /// - OCSP：在线证书状态协议服务器地址，用于实时查询证书吊销状态
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.2.1"/>
    /// </summary>
    public AuthorityInformationAccess? AuthorityInformationAccessInstance
    {
        get
        {
            var aiaExtension = _bcCertificate.GetExtensionValue(X509Extensions.AuthorityInfoAccess);
            if (aiaExtension is null)
                return null;

            var asn1Object = X509ExtensionUtilities.FromExtensionValue(aiaExtension);
            var authorityInfoAccess = Org.BouncyCastle.Asn1.X509.AuthorityInformationAccess.GetInstance(asn1Object);
            return authorityInfoAccess;
        }
    }
    public IEnumerable<string>? AuthorityInformationAccessOscp
    {
        get
        {
            if (this.AuthorityInformationAccessInstance is null)
                return null;

            var ocspUrls = new List<string>();
            foreach (var accessDescription in this.AuthorityInformationAccessInstance.GetAccessDescriptions())
            {
                var location = accessDescription.AccessLocation;
                if (location.TagNo == Org.BouncyCastle.Asn1.X509.GeneralName.UniformResourceIdentifier
                    && accessDescription.AccessMethod == X509ObjectIdentifiers.OcspAccessMethod)
                {
                    var url = location.Name.ToString() ?? string.Empty;
                    if (!String.IsNullOrWhiteSpace(url))
                    {
                        ocspUrls.Add(url);
                    }
                }
            }
            return ocspUrls;
        }
    }

    public IEnumerable<string>? AuthorityInformationAccessCaIssuers
    {
        get
        {
            if (this.AuthorityInformationAccessInstance is null)
                return null;

            var ocspUrls = new List<string>();
            foreach (var accessDescription in this.AuthorityInformationAccessInstance.GetAccessDescriptions())
            {
                var location = accessDescription.AccessLocation;
                if (location.TagNo == Org.BouncyCastle.Asn1.X509.GeneralName.UniformResourceIdentifier
                    && accessDescription.AccessMethod == X509ObjectIdentifiers.IdADCAIssuers)
                {
                    var url = location.Name.ToString() ?? string.Empty;
                    if (!String.IsNullOrWhiteSpace(url))
                    {
                        ocspUrls.Add(url);
                    }
                }
            }
            return ocspUrls;
        }
    }

    /// <summary>
    /// 证书策略（Certificate Policies）
    /// 标识证书颁发所遵循的策略，定义证书的使用规则和信任级别。
    /// 不同的策略 OID 代表不同级别的安全保障和验证要求。
    /// 对于扩展验证（EV）证书，会包含特定的策略 OID。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.4"/>
    /// </summary>
    /// <example>
    /// 示例策略 OID：
    /// - 2.23.140.1.2.1：域名验证（DV）证书
    /// - 2.23.140.1.2.2：组织验证（OV）证书
    /// - 2.23.140.1.1：扩展验证（EV）证书
    /// </example>
    public CertificatePolicies? CertificatePoliciesInstance
    {
        get
        {
            var policyExtension = _bcCertificate.GetExtensionValue(X509Extensions.CertificatePolicies);
            if (policyExtension is not null)
            {
                var asn1Object = X509ExtensionUtilities.FromExtensionValue(policyExtension);
                var certificatePolicies = Org.BouncyCastle.Asn1.X509.CertificatePolicies.GetInstance(asn1Object);
                return certificatePolicies;
            }
            return null;
        }
    }

    /// <summary>
    /// 证书策略（Certificate Policies）
    /// 标识证书颁发所遵循的策略，定义证书的使用规则和信任级别。
    /// 不同的策略 OID 代表不同级别的安全保障和验证要求。
    /// 对于扩展验证（EV）证书，会包含特定的策略 OID。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.4"/>
    /// </summary>
    /// <example>
    /// 示例策略 OID：
    /// - 2.5.29.32.0：任何策略
    /// - 2.23.140.1.2.1：域名验证（DV）证书
    /// - 2.23.140.1.2.2：组织验证（OV）证书
    /// - 2.23.140.1.1：扩展验证（EV）证书
    /// </example>
    public IEnumerable<DerObjectIdentifier>? CertificatePolicyOids
    {
        get
        {
            if (this.CertificatePoliciesInstance is null)
                return null;

            return this.CertificatePoliciesInstance
                .GetPolicyInformation()
                .Select(pi => pi.PolicyIdentifier);
        }
    }

    public CertificatePolicy? CertificatePolicies => CertificatePolicyHelper.FromOids(this.CertificatePolicyOids);

    /// <summary>
    /// 证书签名值
    /// 证书颁发者使用其私钥对证书内容生成的数字签名。
    /// 签名值用于验证证书的真实性和完整性，确保证书未被篡改。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.1.3"/>
    /// </summary>
    public string Signature => Convert.ToHexString(_bcCertificate.GetSignature());

    /// <summary>
    /// 所有扩展字段
    /// 获取证书中包含的所有 X.509 v3 扩展字段的详细信息。
    /// 扩展字段提供了证书的附加信息和约束条件。
    /// 每个扩展包含 OID、是否关键（Critical）以及扩展值。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2"/>
    /// </summary>
    public IEnumerable<CertificateExtension> Extensions
    {
        get
        {
            var extensions = new List<CertificateExtension>();
            try
            {
                var criticalOids = _bcCertificate.GetCriticalExtensionOids();
                var nonCriticalOids = _bcCertificate.GetNonCriticalExtensionOids();

                if (criticalOids != null)
                {
                    foreach (var oidObj in criticalOids)
                    {
                        var oid = oidObj?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(oid))
                        {
                            var extValue = _bcCertificate.GetExtensionValue(new DerObjectIdentifier(oid));
                            if (extValue != null)
                            {
                                extensions.Add(new CertificateExtension(oid, true, extValue.GetOctets()));
                            }
                        }
                    }
                }

                if (nonCriticalOids != null)
                {
                    foreach (var oidObj in nonCriticalOids)
                    {
                        var oid = oidObj?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(oid))
                        {
                            var extValue = _bcCertificate.GetExtensionValue(new DerObjectIdentifier(oid));
                            if (extValue != null)
                            {
                                extensions.Add(new CertificateExtension(oid, false, extValue.GetOctets()));
                            }
                        }
                    }
                }
            }
            catch
            {
                // 如果解析失败，返回空列表
            }
            return extensions;
        }
    }

    /// <summary>
    /// 构造函数
    /// 从 BouncyCastle X509Certificate 对象创建 Certificate 实例。
    /// </summary>
    /// <param name="certificate">BouncyCastle X509 证书对象，不能为 null</param>
    /// <exception cref="ArgumentNullException">当 certificate 为 null 时抛出</exception>
    public Certificate(Org.BouncyCastle.X509.X509Certificate certificate)
    {
        _bcCertificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
    }

    /// <summary>
    /// 获取证书公钥
    /// 提取证书中包含的公钥信息。
    /// 公钥用于验证签名、加密数据等操作，是证书的核心组成部分。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.1.2.7"/>
    /// </summary>
    /// <returns>证书的公钥对象</returns>
    public AsymmetricPublicKeyParameter GetPublicKey() => new(_bcCertificate.GetPublicKey());

    /// <summary>
    /// 检查证书在指定时间是否有效
    /// 验证指定时间是否在证书的有效期（NotBefore 到 NotAfter）内。
    /// 用于验证证书在特定时间点的有效性，例如验证历史签名或未来生效的证书。
    /// </summary>
    /// <param name="dateTime">要检查的时间点</param>
    /// <returns>如果证书在指定时间有效则返回 true，否则返回 false</returns>
    public bool IsValidAt(DateTime dateTime) => _bcCertificate.IsValid(dateTime);

    /// <summary>
    /// 证书是否有效（基于当前 UTC 时间）
    /// 检查当前时间是否在证书的有效期内。
    /// 等同于调用 <c>IsValidAt(DateTime.UtcNow)</c>。
    /// 用于快速判断证书当前是否可用。
    /// </summary>
    public bool IsValidNow => this.IsValidAt(DateTime.UtcNow);

    /// <summary>
    /// 证书有效期长度
    /// 从证书生效时间到过期时间的时间跨度。
    /// </summary>
    public TimeSpan ValidityPeriod => this.NotAfter - this.NotBefore;

    /// <summary>
    /// 距离证书到期的剩余时间
    /// 从当前时间（UTC）到证书过期时间的时间跨度。
    /// 如果证书已过期，返回负值。
    /// </summary>
    public TimeSpan TimeUntilExpiry => this.NotAfter - DateTime.UtcNow;

    /// <summary>
    /// 证书是否即将过期
    /// 检查证书是否在指定天数内即将过期（默认 30 天）。
    /// </summary>
    /// <param name="warningDays">提前多少天视为即将过期，默认 30 天</param>
    /// <returns>如果证书在指定天数内即将过期或已过期，返回 true</returns>
    public bool IsExpiringSoon(int warningDays = 30) => this.TimeUntilExpiry.TotalDays <= warningDays;

    /// <summary>
    /// 验证证书签名
    /// 使用颁发者证书的公钥验证当前证书的签名是否有效。
    /// 这是证书链验证的核心步骤，确保证书确实由声称的 CA 签发且未被篡改。
    /// </summary>
    /// <param name="issuerCertificate">颁发者证书，包含用于验证签名的公钥</param>
    /// <returns>如果签名验证成功返回 true，验证失败或发生异常返回 false</returns>
    public bool IsSignatureVerify(Certificate issuerCertificate)
        => _bcCertificate.IsSignatureValid(issuerCertificate._bcCertificate.GetPublicKey());

    /// <summary>
    /// 获取原始 BouncyCastle 证书对象
    /// 提供对底层 BouncyCastle 实现的直接访问，用于需要使用 BouncyCastle 特定功能的场景。
    /// </summary>
    /// <returns>底层的 BouncyCastle X509Certificate 对象</returns>
    public Org.BouncyCastle.X509.X509Certificate GetBouncyCastleCertificate() => _bcCertificate;

    /// <summary>
    /// 导出为 PEM 格式
    /// 将证书编码为 PEM（Privacy Enhanced Mail）格式。
    /// PEM 格式是 Base64 编码的 DER 格式，以 "-----BEGIN CERTIFICATE-----" 开头，
    /// 以 "-----END CERTIFICATE-----" 结尾，便于在文本环境中传输和存储。
    /// 广泛应用于 Web 服务器配置、证书链文件等场景。
    /// </summary>
    /// <returns>PEM 格式的证书字符串</returns>
    public string ToPem()
    {
        using var writer = new StringWriter();
        var pemWriter = new PemWriter(writer);

        pemWriter.WriteObject(_bcCertificate);
        pemWriter.Writer.Flush();

        return writer.ToString();
    }

    /// <summary>
    /// 导出为 DER 格式
    /// 将证书编码为 DER（Distinguished Encoding Rules）格式。
    /// DER 是 ASN.1 的二进制编码方式，是 X.509 证书的标准编码格式。
    /// 通常用于二进制文件存储和某些编程接口。
    /// </summary>
    /// <returns>DER 格式的证书字节数组</returns>
    public byte[] ToDer() => _bcCertificate.GetEncoded();

    /// <summary>
    /// 从 PEM 字符串加载证书
    /// 解析 PEM 格式的证书字符串并创建 Certificate 对象。
    /// PEM 格式示例：
    /// -----BEGIN CERTIFICATE-----
    /// MIIDXTCCAkWgAwIBAgIJAKL...
    /// -----END CERTIFICATE-----
    /// 自动处理换行符和空白字符。
    /// </summary>
    /// <param name="pem">PEM 格式的证书字符串</param>
    /// <returns>解析后的 Certificate 对象</returns>
    /// <exception cref="FormatException">当 PEM 格式无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当证书数据无法解析时抛出</exception>
    public static Certificate FromPem(string pem)
    {
        using var reader = new StringReader(pem);
        var pemReader = new PemReader(reader);
        var cert = pemReader.ReadObject() as Org.BouncyCastle.X509.X509Certificate;
        if (cert is null)
        {
            throw new InvalidOperationException("Unable to parse the certificate from PEM.");
        }
        return new(cert);
    }

    /// <summary>
    /// 从 DER 字节数组加载证书
    /// 解析 DER 编码的证书字节数组并创建 Certificate 对象。
    /// DER 是 X.509 证书的标准二进制编码格式。
    /// </summary>
    /// <param name="der">DER 格式的证书字节数组</param>
    /// <returns>解析后的 Certificate 对象</returns>
    /// <exception cref="ArgumentException">当 DER 数据无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当证书数据无法解析时抛出</exception>
    public static Certificate FromDer(byte[] der)
    {
        var parser = new Org.BouncyCastle.X509.X509CertificateParser();
        var cert = parser.ReadCertificate(der);
        return new Certificate(cert);
    }

    /// <summary>
    /// 计算证书指纹（Fingerprint）
    /// 证书指纹是证书内容的哈希摘要，用于唯一标识证书。
    /// 常用于证书比对、证书固定（Certificate Pinning）等安全场景。
    /// 支持多种哈希算法：SHA-1（不推荐，用于兼容）、SHA-256（推荐）、SHA-384、SHA-512、MD5、SM3。
    /// </summary>
    /// <param name="algorithm">哈希算法名称，支持 SHA-1、SHA-256、SHA-384、SHA-512、MD5、SM3，默认为 SHA-256</param>
    /// <param name="format">是否格式化为冒号分隔格式，默认为 false</param>
    /// <returns>证书指纹字符串，格式化时为 "XX:XX:XX:..."，否则为连续十六进制字符串</returns>
    public string ComputeFingerprint(string algorithm = "SHA-256", bool format = false)
    {
        var derBytes = this.ToDer();
        return FingerprintHelper.ComputeFingerprint(derBytes, algorithm, format);
    }

    /// <summary>
    /// 返回证书的字符串表示形式
    /// 包含证书的主体、颁发者和有效期等关键信息，便于日志记录和调试。
    /// </summary>
    /// <returns>格式化的证书信息字符串</returns>
    public override string ToString() => $"Subject: {this.Subject}, Issuer: {this.Issuer}, Valid: {this.NotBefore:yyyy-MM-dd} to {this.NotAfter:yyyy-MM-dd}";

    /// <summary>
    /// 生成自签名证书
    /// 创建一个主体和颁发者相同的证书，通常用于根 CA 证书或测试环境。
    /// 自签名证书使用自己的私钥对证书进行签名。
    /// </summary>
    /// <param name="subjectDN">证书主体的可分辨名称</param>
    /// <param name="privateKey">用于签名的私钥</param>
    /// <param name="validFrom">证书生效时间</param>
    /// <param name="validTo">证书过期时间</param>
    /// <param name="serialNumber">证书序列号（十六进制字符串）</param>
    /// <param name="signatureAlgorithm">签名算法，如 "SHA256WITHRSA"</param>
    /// <returns>生成的自签名证书</returns>
    public static Certificate GenerateSelfSigned(
        string subjectDN,
        AsymmetricPrivateKeyParameter privateKey,
        DateTime validFrom,
        DateTime validTo,
        string serialNumber,
        string signatureAlgorithm = "SHA256WITHRSA",
        X509ExtensionOptions? extensions = null)
    {
        var certGen = new X509V3CertificateGenerator();
        var dnName = X509NameParser.Parse(subjectDN);
        var serial = new BigInteger(serialNumber, 16);
        var bcPrivateKey = privateKey.GetBouncyCastleKey();

        // 从私钥生成公钥
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter bcPublicKey;
        if (bcPrivateKey is RsaPrivateCrtKeyParameters rsaPrivate)
        {
            bcPublicKey = new RsaKeyParameters(false, rsaPrivate.Modulus, rsaPrivate.PublicExponent);
        }
        else if (bcPrivateKey is ECPrivateKeyParameters ecPrivate)
        {
            var q = ecPrivate.Parameters.G.Multiply(ecPrivate.D);
            bcPublicKey = new ECPublicKeyParameters(q, ecPrivate.Parameters);
        }
        else if (bcPrivateKey is DsaPrivateKeyParameters dsaPrivate)
        {
            // DSA: 计算公钥 Y = G^X mod P
            var y = dsaPrivate.Parameters.G.ModPow(dsaPrivate.X, dsaPrivate.Parameters.P);
            bcPublicKey = new DsaPublicKeyParameters(y, dsaPrivate.Parameters);
        }
        else
        {
            throw new NotSupportedException($"Unsupported private key type: {bcPrivateKey.GetType().Name}.");
        }

        certGen.SetSerialNumber(serial);
        certGen.SetSubjectDN(dnName);
        certGen.SetIssuerDN(dnName); // 自签名：主体和颁发者相同
        certGen.SetNotBefore(validFrom);
        certGen.SetNotAfter(validTo);
        certGen.SetPublicKey(bcPublicKey);

        if (extensions is not null)
        {
            // 应用调用方指定的扩展；自签名证书的 SKI/AKI 均使用证书自身公钥计算。
            X509ExtensionBuilder.Apply(certGen, extensions, bcPublicKey, bcPublicKey);
        }
        else
        {
            // 兼容旧行为：默认添加 CA 基本约束扩展。
            certGen.AddExtension(
                X509Extensions.BasicConstraints,
                true,
                new BasicConstraints(true));
        }

        var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, bcPrivateKey, new SecureRandom());
        var bcCert = certGen.Generate(signatureFactory);
        return new Certificate(bcCert);
    }

    /// <summary>
    /// 基于 CSR 签发证书
    /// CA 使用自己的私钥为 CSR 中的公钥签发证书。
    /// 证书的主体信息来自 CSR，颁发者信息来自 CA 证书。
    /// </summary>
    /// <param name="csr">证书签名请求</param>
    /// <param name="caCertificate">CA 证书</param>
    /// <param name="caPrivateKey">CA 私钥</param>
    /// <param name="validFrom">证书生效时间</param>
    /// <param name="validTo">证书过期时间</param>
    /// <param name="serialNumber">证书序列号（十六进制字符串）</param>
    /// <param name="signatureAlgorithm">签名算法，默认使用 CA 证书的签名算法</param>
    /// <returns>签发的证书</returns>
    public static Certificate SignCsr(
        CertificateSigningRequest csr,
        Certificate caCertificate,
        AsymmetricPrivateKeyParameter caPrivateKey,
        DateTime validFrom,
        DateTime validTo,
        string serialNumber,
        string? signatureAlgorithm = null,
        X509ExtensionOptions? extensions = null)
    {
        var certGen = new X509V3CertificateGenerator();
        var bcCsr = csr.GetBouncyCastleRequest();
        var serial = new BigInteger(serialNumber, 16);

        certGen.SetSerialNumber(serial);
        certGen.SetSubjectDN(bcCsr.GetCertificationRequestInfo().Subject);
        certGen.SetIssuerDN(caCertificate.GetBouncyCastleCertificate().SubjectDN);
        certGen.SetNotBefore(validFrom);
        certGen.SetNotAfter(validTo);
        var subjectPublicKey = bcCsr.GetPublicKey();
        certGen.SetPublicKey(subjectPublicKey);

        if (extensions is not null)
        {
            var caPublicKey = caCertificate.GetBouncyCastleCertificate().GetPublicKey();
            X509ExtensionBuilder.Apply(certGen, extensions, subjectPublicKey, caPublicKey);
        }

        var sigAlg = signatureAlgorithm ?? caCertificate.SignatureAlgorithmName;
        var signatureFactory = new Asn1SignatureFactory(sigAlg, caPrivateKey.GetBouncyCastleKey(), new SecureRandom());
        var bcCert = certGen.Generate(signatureFactory);
        return new Certificate(bcCert);
    }

    /// <summary>
    /// 基于公钥签发证书
    /// CA 使用自己的私钥为指定的公钥签发证书。
    /// 适用于已有公钥但不想生成 CSR 的场景。
    /// </summary>
    /// <param name="publicKey">证书公钥</param>
    /// <param name="subjectDN">证书主体的可分辨名称</param>
    /// <param name="caCertificate">CA 证书</param>
    /// <param name="caPrivateKey">CA 私钥</param>
    /// <param name="validFrom">证书生效时间</param>
    /// <param name="validTo">证书过期时间</param>
    /// <param name="serialNumber">证书序列号（十六进制字符串）</param>
    /// <param name="signatureAlgorithm">签名算法，默认使用 CA 证书的签名算法</param>
    /// <returns>签发的证书</returns>
    public static Certificate SignPublicKey(
        AsymmetricPublicKeyParameter publicKey,
        string subjectDN,
        Certificate caCertificate,
        AsymmetricPrivateKeyParameter caPrivateKey,
        DateTime validFrom,
        DateTime validTo,
        string serialNumber,
        string? signatureAlgorithm = null,
        X509ExtensionOptions? extensions = null)
    {
        var certGen = new X509V3CertificateGenerator();
        var subjectName = X509NameParser.Parse(subjectDN);
        var serial = new BigInteger(serialNumber, 16);

        certGen.SetSerialNumber(serial);
        certGen.SetSubjectDN(subjectName);
        certGen.SetIssuerDN(caCertificate.GetBouncyCastleCertificate().SubjectDN);
        certGen.SetNotBefore(validFrom);
        certGen.SetNotAfter(validTo);
        var bcPublicKey = publicKey.GetBouncyCastleKey();
        certGen.SetPublicKey(bcPublicKey);

        if (extensions is not null)
        {
            var caPublicKey = caCertificate.GetBouncyCastleCertificate().GetPublicKey();
            X509ExtensionBuilder.Apply(certGen, extensions, bcPublicKey, caPublicKey);
        }

        var sigAlg = signatureAlgorithm ?? caCertificate.SignatureAlgorithmName;
        var signatureFactory = new Asn1SignatureFactory(sigAlg, caPrivateKey.GetBouncyCastleKey(), new SecureRandom());
        var bcCert = certGen.Generate(signatureFactory);
        return new Certificate(bcCert);
    }
}
