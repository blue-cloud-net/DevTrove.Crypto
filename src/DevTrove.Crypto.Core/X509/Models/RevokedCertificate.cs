using CertificateRevocationReason = DevTrove.Crypto.X509.Enums.CertificateRevocationReason;

namespace DevTrove.Crypto.X509.Models;

/// <summary>
/// 撤销证书信息
/// 表示 CRL 中的一个已撤销证书条目。
/// 包含证书序列号、撤销时间和撤销原因等信息。
/// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.1.2.6"/>
/// </summary>
public class RevokedCertificateInfo
{
    private readonly X509CrlEntry _entry;

    /// <summary>
    /// 证书序列号
    /// 被撤销证书的唯一标识符，以大写十六进制字符串表示。
    /// 通过序列号可以唯一定位 CA 签发的某个证书。
    /// </summary>
    public string SerialNumber => _entry.SerialNumber.ToString(16).ToUpper();

    /// <summary>
    /// 撤销时间
    /// 证书被添加到 CRL 的日期和时间。
    /// 注意：这不一定是证书实际失效的时间，而是 CA 将其加入 CRL 的时间。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.3.2"/>
    /// </summary>
    public DateTime RevocationDate => _entry.RevocationDate;

    /// <summary>
    /// 撤销原因（如果有）
    /// 证书被撤销的原因代码。
    /// 常见原因包括：密钥泄露（KeyCompromise）、CA 泄露（CACompromise）、
    /// 终止使用（CessationOfOperation）、被替代（Superseded）等。
    /// 如果 CRL 条目中未包含原因扩展，则返回 null。
    /// RFC 参考 <see href="https://datatracker.ietf.org/doc/html/rfc5280#section-5.3.1"/>
    /// </summary>
    /// <example>
    /// 撤销原因代码：
    /// - 0: Unspecified（未指定）
    /// - 1: KeyCompromise（密钥泄露）
    /// - 2: CACompromise（CA 泄露）
    /// - 3: AffiliationChanged（关联变更）
    /// - 4: Superseded（被替代）
    /// - 5: CessationOfOperation（停止运营）
    /// - 6: CertificateHold（证书挂起）
    /// - 8: RemoveFromCRL（从 CRL 中移除）
    /// - 9: PrivilegeWithdrawn（权限撤销）
    /// - 10: AACompromise（属性授权泄露）
    /// </example>
    public CertificateRevocationReason? RevocationReason
    {
        get
        {
            var reasonExtension = _entry.GetExtension(X509Extensions.ReasonCode);
            if (reasonExtension is null)
                return null;
            var asn1Object = X509ExtensionUtilities.FromExtensionValue(reasonExtension.Value);
            var reason = DerEnumerated.GetInstance(asn1Object);
            return CertificateRevocationReasonHelper.FromInt(reason.Value.IntValue);
        }
    }

    /// <summary>
    /// 内部构造函数
    /// 从 BouncyCastle CRL 条目创建撤销证书信息对象。
    /// </summary>
    /// <param name="entry">BouncyCastle X509CrlEntry 对象</param>
    /// <exception cref="ArgumentNullException">当 entry 为 null 时抛出</exception>
    internal RevokedCertificateInfo(X509CrlEntry entry)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    /// <summary>
    /// 返回撤销证书的字符串表示形式
    /// 包含序列号、撤销时间和撤销原因（如果有），便于日志记录和调试。
    /// </summary>
    /// <returns>格式化的撤销证书信息字符串</returns>
    public override string ToString()
    {
        var reason = this.RevocationReason is not null ? $", Reason: {this.RevocationReason}" : "";
        return $"Serial: {this.SerialNumber}, Revoked: {this.RevocationDate:yyyy-MM-dd}{reason}";
    }
}
