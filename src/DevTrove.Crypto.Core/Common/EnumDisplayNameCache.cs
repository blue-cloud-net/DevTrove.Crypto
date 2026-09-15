using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;

namespace DevTrove.Crypto.Common;

/// <summary>
/// 枚举显示名称缓存工具类
/// 使用静态构造函数预加载所有枚举值的 Display 特性
/// 支持从资源文件中获取本地化的显示名称
/// </summary>
/// <typeparam name="TEnum">枚举类型</typeparam>
public static class EnumDisplayNameCache<TEnum> where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, DisplayAttribute> _displayAttributes;

    static EnumDisplayNameCache()
    {
        _displayAttributes = new Dictionary<TEnum, DisplayAttribute>();

        foreach (var enumValue in Enum.GetValues<TEnum>())
        {
            var displayAttribute = typeof(TEnum)
                .GetField(enumValue.ToString())
                ?.GetCustomAttribute<DisplayAttribute>();

            if (displayAttribute is null)
                continue;

            _displayAttributes[enumValue] = displayAttribute;
        }
    }

    /// <summary>
    /// 获取枚举值的显示名称（支持国际化）
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>Display 特性中定义的名称,如果没有则返回枚举名</returns>
    public static string? GetDisplayName(TEnum enumValue) =>
        _displayAttributes.TryGetValue(enumValue, out var displayAttribute)
            ? displayAttribute.GetName()
            : null;

    /// <summary>
    /// 尝试获取枚举值的显示名称
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <param name="displayName">输出的显示名称</param>
    /// <returns>如果成功获取返回 true,否则返回 false</returns>
#if NETSTANDARD2_1_OR_GREATER
    public static bool TryGetDisplayName(TEnum enumValue, [MaybeNullWhen(false)] out string? displayName)
#else
    public static bool TryGetDisplayName(TEnum enumValue, out string? displayName)
#endif
    {
        if (_displayAttributes.TryGetValue(enumValue, out var displayAttribute))
        {
            displayName = displayAttribute.GetName();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return true;
            }
        }

        displayName = null;
        return false;
    }
}