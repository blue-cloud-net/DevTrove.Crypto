namespace DevTrove.Crypto.X509.Enums;

public static class GeneralNameTypeExtensions
{
    /// <param name="type">GeneralNameType 枚举值</param>
    extension(GeneralNameType type)
    {
        /// <summary>
        /// 获取通用名称类型的枚举名称
        /// </summary>
        /// <returns>枚举名称</returns>
        public string GetName() => Enum.GetName(type) ?? type.ToString();

        /// <summary>
        /// 获取通用名称类型的友好名称（支持国际化）
        /// </summary>
        /// <returns>友好名称</returns>
        public string GetFriendlyName() =>
            EnumDisplayNameCache<GeneralNameType>.GetDisplayName(type) ?? type.GetName();
    }
}