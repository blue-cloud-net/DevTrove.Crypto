namespace DevTrove.Crypto.X509.Enums;

public static class CertificatePolicyExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="policy">证书策略枚举值</param>
    extension(CertificatePolicy policy)
    {
        /// <summary>
        /// 获取证书策略的枚举名称
        /// </summary>
        /// <returns>枚举名称</returns>
        public string GetName() => Enum.GetName(policy) ?? policy.ToString();

        /// <summary>
        /// 获取证书策略的友好名称（使用 Display 特性，支持国际化）
        /// </summary>
        /// <returns>Display 特性中定义的本地化名称，如果没有则返回枚举名</returns>
        public string GetFriendlyName() =>
            EnumDisplayNameCache<CertificatePolicy>.GetDisplayName(policy) ?? policy.GetName();
    }
}