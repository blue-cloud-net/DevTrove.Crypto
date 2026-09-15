using KeyUsage = DevTrove.Crypto.X509.Enums.KeyUsage;

namespace DevTrove.Crypto.X509.Extensions;

/// <summary>
/// 密钥用途辅助类
/// 提供密钥用途枚举和 BouncyCastle 格式之间的转换功能。
/// </summary>
public static class KeyUsageHelper
{
    /// <summary>
    /// 检查 KeyUsage 数组是否包含指定的密钥用途
    /// </summary>
    /// <param name="keyUsageBytes">BouncyCastle KeyUsage bool 数组</param>
    /// <param name="usage">要检查的密钥用途</param>
    /// <returns>如果包含指定用途返回 true</returns>
    public static bool HasKeyUsage(bool[] keyUsageBytes, KeyUsage usage)
    {
        var keyUsages = FromBoolArray(keyUsageBytes);
        return keyUsages.HasFlag(usage);
    }

    /// <summary>
    /// 将 KeyUsage 枚举转换为 BouncyCastle 的 bool 数组格式
    /// </summary>
    /// <param name="usage">密钥用途枚举值</param>
    /// <returns>BouncyCastle KeyUsage bool 数组</returns>
    public static bool[] ToBoolArray(KeyUsage usage)
    {
        var keyUsage = new bool[9];
        keyUsage[0] = usage.HasFlag(KeyUsage.DigitalSignature);
        keyUsage[1] = usage.HasFlag(KeyUsage.NonRepudiation);
        keyUsage[2] = usage.HasFlag(KeyUsage.KeyEncipherment);
        keyUsage[3] = usage.HasFlag(KeyUsage.DataEncipherment);
        keyUsage[4] = usage.HasFlag(KeyUsage.KeyAgreement);
        keyUsage[5] = usage.HasFlag(KeyUsage.KeyCertSign);
        keyUsage[6] = usage.HasFlag(KeyUsage.CrlSign);
        keyUsage[7] = usage.HasFlag(KeyUsage.EncipherOnly);
        keyUsage[8] = usage.HasFlag(KeyUsage.DecipherOnly);
        return keyUsage;
    }

    /// <summary>
    /// 将 KeyUsage 枚举转换为 BouncyCastle 的 KeyUsage 格式
    /// </summary>
    /// <param name="usage">密钥用途枚举值</param>
    /// <returns>BouncyCastle KeyUsage 对象</returns>
    public static Org.BouncyCastle.Asn1.X509.KeyUsage ToBouncyCastleFormat(KeyUsage usage)
    {
        var keyUsageInt = 0;

        if (usage.HasFlag(KeyUsage.DigitalSignature)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.DigitalSignature;
        if (usage.HasFlag(KeyUsage.NonRepudiation)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.NonRepudiation;
        if (usage.HasFlag(KeyUsage.KeyEncipherment)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.KeyEncipherment;
        if (usage.HasFlag(KeyUsage.DataEncipherment)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.DataEncipherment;
        if (usage.HasFlag(KeyUsage.KeyAgreement)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.KeyAgreement;
        if (usage.HasFlag(KeyUsage.KeyCertSign)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.KeyCertSign;
        if (usage.HasFlag(KeyUsage.CrlSign)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.CrlSign;
        if (usage.HasFlag(KeyUsage.EncipherOnly)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.EncipherOnly;
        if (usage.HasFlag(KeyUsage.DecipherOnly)) keyUsageInt |= Org.BouncyCastle.Asn1.X509.KeyUsage.DecipherOnly;

        return new Org.BouncyCastle.Asn1.X509.KeyUsage(keyUsageInt);
    }


    /// <summary>
    /// 从 BouncyCastle bool 数组格式转换为 KeyUsage 枚举
    /// </summary>
    /// <param name="keyUsage">BouncyCastle KeyUsage bool 数组</param>
    /// <returns>密钥用途枚举值</returns>
    public static KeyUsage FromBoolArray(bool[]? keyUsage)
    {
        if (keyUsage is null || keyUsage.Length == 0)
        {
            return KeyUsage.None;
        }

        var usages = KeyUsage.None;

        if (keyUsage.Length > 0 && keyUsage[0]) usages |= KeyUsage.DigitalSignature;
        if (keyUsage.Length > 1 && keyUsage[1]) usages |= KeyUsage.NonRepudiation;
        if (keyUsage.Length > 2 && keyUsage[2]) usages |= KeyUsage.KeyEncipherment;
        if (keyUsage.Length > 3 && keyUsage[3]) usages |= KeyUsage.DataEncipherment;
        if (keyUsage.Length > 4 && keyUsage[4]) usages |= KeyUsage.KeyAgreement;
        if (keyUsage.Length > 5 && keyUsage[5]) usages |= KeyUsage.KeyCertSign;
        if (keyUsage.Length > 6 && keyUsage[6]) usages |= KeyUsage.CrlSign;
        if (keyUsage.Length > 7 && keyUsage[7]) usages |= KeyUsage.EncipherOnly;
        if (keyUsage.Length > 8 && keyUsage[8]) usages |= KeyUsage.DecipherOnly;

        return usages;
    }


    /// <summary>
    /// 从 BouncyCastle KeyUsage 对象转换为 KeyUsage 枚举
    /// </summary>
    /// <param name="keyUsage">BouncyCastle KeyUsage 对象</param>
    /// <returns>密钥用途枚举值</returns>
    public static KeyUsage FromBouncyCastleFormat(Org.BouncyCastle.Asn1.X509.KeyUsage keyUsage)
    {
        var usages = KeyUsage.None;
        var keyUsageInt = keyUsage.IntValue;

        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.DigitalSignature) != 0) usages |= KeyUsage.DigitalSignature;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.NonRepudiation) != 0) usages |= KeyUsage.NonRepudiation;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.KeyEncipherment) != 0) usages |= KeyUsage.KeyEncipherment;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.DataEncipherment) != 0) usages |= KeyUsage.DataEncipherment;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.KeyAgreement) != 0) usages |= KeyUsage.KeyAgreement;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.KeyCertSign) != 0) usages |= KeyUsage.KeyCertSign;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.CrlSign) != 0) usages |= KeyUsage.CrlSign;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.EncipherOnly) != 0) usages |= KeyUsage.EncipherOnly;
        if ((keyUsageInt & Org.BouncyCastle.Asn1.X509.KeyUsage.DecipherOnly) != 0) usages |= KeyUsage.DecipherOnly;

        return usages;
    }
}
