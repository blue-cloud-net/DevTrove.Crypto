namespace DevTrove.Crypto.X509.Enums;

public static class ExtendedKeyUsageExtensions
{
    /// <param name="usage">扩展密钥用途枚举值</param>
    extension(ExtendedKeyUsage usage)
    {
        /// <summary>
        /// 获取扩展密钥用途的枚举名称列表
        /// </summary>
        /// <returns>枚举名称列表</returns>
        public IEnumerable<string> GetNames()
        {
            var names = new List<string>();

            foreach (ExtendedKeyUsage flag in Enum.GetValues(typeof(ExtendedKeyUsage)))
            {
                if (flag == ExtendedKeyUsage.None)
                    break;

                if (!usage.HasFlag(flag))
                    continue;

                names.Add(Enum.GetName(flag) ?? flag.ToString());
            }

            return names;
        }

        /// <summary>
        /// 获取扩展密钥用途的友好名称列表
        /// </summary>
        /// <returns>用途名称列表</returns>
        public IEnumerable<string> GetFriendlyNames()
        {
            var names = new List<string>();
            foreach (ExtendedKeyUsage flag in Enum.GetValues(typeof(ExtendedKeyUsage)))
            {
                if (flag == ExtendedKeyUsage.None)
                    break;

                if (!usage.HasFlag(flag))
                    continue;

                names.Add(EnumDisplayNameCache<ExtendedKeyUsage>.GetDisplayName(flag)
                          ?? Enum.GetName(flag) ?? flag.ToString());
            }
            return names;
        }
    }
}