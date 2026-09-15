namespace DevTrove.Crypto.X509.Enums;

/// <summary>
/// X.509 证书吊销原因（CRL Reason Code）
/// 定义证书被吊销的具体原因,用于 CRL 扩展中的 Reason Code 字段。
/// RFC 标准参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.3.1"/>
/// </summary>
public enum CertificateRevocationReason
{
    /// <summary>
    /// 未指定原因（Unspecified）
    /// 吊销原因未明确说明或不适合其他分类。
    /// </summary>
    [Display(Name = "CrlReason_Unspecified", ResourceType = typeof(CryptoUtilCore))]
    Unspecified = 0,

    /// <summary>
    /// 密钥泄露（Key Compromise）
    /// 证书私钥已被泄露或可能已被泄露。
    /// 适用场景：私钥被盗、不安全存储等。
    /// </summary>
    [Display(Name = "CrlReason_KeyCompromise", ResourceType = typeof(CryptoUtilCore))]
    KeyCompromise = 1,

    /// <summary>
    /// CA 泄露（CA Compromise）
    /// 证书颁发机构的私钥已被泄露。
    /// 适用场景：CA 密钥被盗、CA 系统被攻击等。
    /// </summary>
    [Display(Name = "CrlReason_CACompromise", ResourceType = typeof(CryptoUtilCore))]
    CACompromise = 2,

    /// <summary>
    /// 从属关系变更（Affiliation Changed）
    /// 证书持有者的身份信息或从属关系发生变更。
    /// 适用场景：员工离职、组织变更等。
    /// </summary>
    [Display(Name = "CrlReason_AffiliationChanged", ResourceType = typeof(CryptoUtilCore))]
    AffiliationChanged = 3,

    /// <summary>
    /// 被取代（Superseded）
    /// 证书已被新证书取代。
    /// 适用场景：证书更新、密钥轮换等。
    /// </summary>
    [Display(Name = "CrlReason_Superseded", ResourceType = typeof(CryptoUtilCore))]
    Superseded = 4,

    /// <summary>
    /// 停止运营（Cessation of Operation）
    /// 证书对应的服务或实体已停止运营。
    /// 适用场景：服务下线、业务终止等。
    /// </summary>
    [Display(Name = "CrlReason_CessationOfOperation", ResourceType = typeof(CryptoUtilCore))]
    CessationOfOperation = 5,

    /// <summary>
    /// 证书挂起（Certificate Hold）
    /// 证书被暂时挂起，可能会被恢复。
    /// 注意：这是唯一可以被撤销的吊销原因。
    /// 适用场景：临时暂停服务、调查期间等。
    /// </summary>
    [Display(Name = "CrlReason_CertificateHold", ResourceType = typeof(CryptoUtilCore))]
    CertificateHold = 6,

    // 注意：值 7 在 RFC 5280 中未定义

    /// <summary>
    /// 从 CRL 中移除（Remove from CRL）
    /// 表示之前处于 CertificateHold 状态的证书现在从 CRL 中移除。
    /// 仅在增量 CRL 中使用。
    /// </summary>
    [Display(Name = "CrlReason_RemoveFromCrl", ResourceType = typeof(CryptoUtilCore))]
    RemoveFromCrl = 8,

    /// <summary>
    /// 特权撤销（Privilege Withdrawn）
    /// 证书持有者的特权或授权被撤销。
    /// 适用场景：权限变更、授权失效等。
    /// </summary>
    [Display(Name = "CrlReason_PrivilegeWithdrawn", ResourceType = typeof(CryptoUtilCore))]
    PrivilegeWithdrawn = 9,

    /// <summary>
    /// AA 泄露（AA Compromise）
    /// 属性授权机构（Attribute Authority）的私钥已被泄露。
    /// 适用场景：AA 密钥被盗、AA 系统被攻击等。
    /// </summary>
    [Display(Name = "CrlReason_AACompromise", ResourceType = typeof(CryptoUtilCore))]
    AACompromise = 10
}
