namespace DevTrove.Crypto.X509.Enums;

/// <summary>
/// X.509 证书策略（Certificate Policies）
/// 定义证书颁发所遵循的策略，标识证书的使用规则、信任级别和验证要求。
/// 不同的策略 OID 代表不同级别的安全保障和身份验证严格程度。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.4"/>
/// </summary>
/// <remarks>
/// <para>
/// 证书策略是 X.509 证书扩展字段的一部分，用于传达证书的预期用途和信任级别。
/// 公共 CA 遵循 CA/Browser Forum 基线要求，使用标准化的策略 OID。
/// </para>
/// <para>
/// <strong>策略级别：</strong>
/// </para>
/// <list type="bullet">
/// <item><description><strong>DV (域名验证)</strong>：仅验证申请者对域名的控制权，验证最快但保障级别较低</description></item>
/// <item><description><strong>OV (组织验证)</strong>：验证组织的合法性和域名控制权，提供中等级别保障</description></item>
/// <item><description><strong>EV (扩展验证)</strong>：最严格的验证，需要深入核实组织身份，提供最高信任级别</description></item>
/// </list>
/// <para>
/// 相关标准：
/// <list type="bullet">
/// <item><description>CA/Browser Forum 基线要求：<see href="https://cabforum.org/baseline-requirements/"/></description></item>
/// <item><description>EV 准则：<see href="https://cabforum.org/extended-validation/"/></description></item>
/// </list>
/// </para>
/// </remarks>
public enum CertificatePolicy
{
    /// <summary>
    /// 未指定策略
    /// </summary>
    [Display(Name = "CertificatePolicy_None", ResourceType = typeof(RS))]
    None = 0,

    /// <summary>
    /// 任何策略（Any Policy）
    /// OID: 2.5.29.32.0
    /// 表示证书可用于任何目的，通常用于策略映射和路径验证。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-4.2.1.4"/>
    /// </summary>
    [Display(Name = "CertificatePolicy_AnyPolicy", ResourceType = typeof(RS))]
    AnyPolicy,

    /// <summary>
    /// 域名验证（Domain Validated, DV）证书策略
    /// OID: 2.23.140.1.2.1
    /// CA 仅验证申请者对域名的控制权（通过 HTTP、DNS 或 Email 验证）。
    /// 验证速度快，通常几分钟到几小时即可签发，适合个人网站和小型应用。
    /// 证书中不包含组织信息，仅显示域名。
    /// CA/Browser Forum 基线要求定义
    /// </summary>
    [Display(Name = "CertificatePolicy_DomainValidated", ResourceType = typeof(RS))]
    DomainValidated,

    /// <summary>
    /// 组织验证（Organization Validated, OV）证书策略
    /// OID: 2.23.140.1.2.2
    /// CA 验证申请者对域名的控制权，并核实组织的合法存在性。
    /// 需要提供工商注册文件等证明材料，验证需要数天时间。
    /// 证书中包含组织名称和所在地信息，提供中等级别信任保障。
    /// 适合企业网站、电子商务平台等。
    /// CA/Browser Forum 基线要求定义
    /// </summary>
    [Display(Name = "CertificatePolicy_OrganizationValidated", ResourceType = typeof(RS))]
    OrganizationValidated,

    /// <summary>
    /// 扩展验证（Extended Validation, EV）证书策略
    /// OID: 2.23.140.1.1
    /// 最严格的证书验证级别，CA 深入核实申请组织的法律、物理和运营存在性。
    /// 需要提供全面的组织文档和第三方数据库验证，验证可能需要数周。
    /// 证书包含完整的组织信息，浏览器通常在地址栏显示绿色标识或组织名称。
    /// 提供最高级别的信任保障，适合金融机构、大型电商等高安全要求场景。
    /// CA/Browser Forum EV 准则定义
    /// </summary>
    [Display(Name = "CertificatePolicy_ExtendedValidation", ResourceType = typeof(RS))]
    ExtendedValidation,

    /// <summary>
    /// Microsoft 文档签名策略
    /// OID: 1.3.6.1.4.1.311.10.3.12
    /// Microsoft 定义的文档签名证书策略，用于 Office 文档、PDF 等的数字签名。
    /// 表示证书可用于签署和验证电子文档的完整性和来源。
    /// </summary>
    [Display(Name = "CertificatePolicy_MicrosoftDocumentSigning", ResourceType = typeof(RS))]
    MicrosoftDocumentSigning
}
