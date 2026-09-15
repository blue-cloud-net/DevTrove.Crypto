namespace DevTrove.Crypto.X509.Extensions;

/// <summary>
/// 通用名称类型辅助类
/// 提供 GeneralNameType 枚举的辅助方法。
/// </summary>
public static class GeneralNameTypeHelper
{
    /// <summary>
    /// 从整数值转换为 GeneralNameType 枚举
    /// </summary>
    /// <param name="tag">GeneralName 标签值</param>
    /// <returns>GeneralNameType 枚举值</returns>
    public static GeneralNameType FromTag(int tag)
    {
        if (Enum.IsDefined(typeof(GeneralNameType), tag))
        {
            return (GeneralNameType)tag;
        }

        return GeneralNameType.Unknown;
    }

    /// <summary>
    /// 将 GeneralNameType 枚举转换为标签值
    /// </summary>
    /// <param name="type">GeneralNameType 枚举值</param>
    /// <returns>标签值</returns>
    public static int ToTag(GeneralNameType type) => (int)type;

    /// <summary>
    /// 获取所有定义的通用名称类型
    /// </summary>
    /// <returns>GeneralNameType 列表</returns>
    public static IEnumerable<GeneralNameType> GetAllTypes() =>
        Enum.GetValues<GeneralNameType>().Where(t => t != GeneralNameType.Unknown);

    /// <summary>
    /// 获取所有通用名称类型的友好名称字典
    /// </summary>
    /// <returns>GeneralNameType 到友好名称的映射</returns>
    public static Dictionary<GeneralNameType, string> GetTypeNameDictionary()
    {
        return Enum.GetValues<GeneralNameType>()
            .Where(t => t != GeneralNameType.Unknown)
            .ToDictionary(t => t, t => t.GetFriendlyName());
    }
}
