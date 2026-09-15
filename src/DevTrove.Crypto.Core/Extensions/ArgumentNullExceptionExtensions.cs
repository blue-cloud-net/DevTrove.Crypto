namespace DevTrove.Crypto.Extensions;

/// <summary>
/// <see cref="ArgumentNullException"/> 扩展方法
/// </summary>
#if NETSTANDARD2_0
public static class ArgumentNullExceptionExtensions
{
    extension(ArgumentNullException)
    {
        /// <summary>
        /// 在 .NET Standard 2.0/2.1 中检查参数是否为 null
        /// </summary>
        /// <param name="argument">要验证的参数</param>
        /// <param name="paramName">参数名称</param>
        /// <exception cref="ArgumentNullException">当参数为 null 时抛出</exception>
        public static void ThrowIfNull(object? argument, string paramName)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
#endif
