namespace DevTrove.Crypto.X509.Enums;

public static class KeyUsageExtensions
{
    /// <param name="usage">密钥用途枚举值</param>
    extension(KeyUsage usage)
    {
        /// <summary>
        /// 获取密钥用途的枚举名称列表
        /// </summary>
        /// <returns>枚举名称列表</returns>
        public IEnumerable<string> GetNames()
        {
            var names = new List<string>();
            foreach (KeyUsage flag in Enum.GetValues(typeof(KeyUsage)))
            {
                if (flag == KeyUsage.None)
                    continue;

                if (!usage.HasFlag(flag))
                    continue;

                names.Add(Enum.GetName(flag) ?? flag.ToString());
            }

            return names;
        }

        /// <summary>
        /// 获取密钥用途的友好名称列表
        /// </summary>
        /// <returns>用途名称列表</returns>
        public IEnumerable<string> GetFriendlyNames()
        {
            var names = new List<string>();
            foreach (KeyUsage flag in Enum.GetValues(typeof(KeyUsage)))
            {
                if (flag == KeyUsage.None)
                    continue;

                if (!usage.HasFlag(flag))
                    continue;

                names.Add(EnumDisplayNameCache<KeyUsage>.GetDisplayName(flag)
                          ?? Enum.GetName(flag) ?? flag.ToString());
            }

            return names;
        }
    }
}