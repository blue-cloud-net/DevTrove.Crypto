using DevTrove.Crypto.Crypto.Sm;

namespace DevTrove.Crypto.X509.Extensions;

/// <summary>
/// 指纹计算辅助类
/// 提供指纹格式化和计算的通用方法
/// </summary>
public static class FingerprintHelper
{
    /// <summary>
    /// 计算数据指纹
    /// 对给定的字节数据进行哈希计算，返回指纹字符串
    /// </summary>
    /// <param name="data">要计算指纹的数据字节数组</param>
    /// <param name="algorithm">哈希算法名称，支持 SHA1、SHA256、SHA384、SHA512、MD5、SM3，默认为 SHA256</param>
    /// <param name="format">是否格式化为冒号分隔格式，默认为 false</param>
    /// <returns>指纹字符串，格式化时为 "XX:XX:XX:..."，否则为连续十六进制字符串</returns>
    /// <exception cref="ArgumentException">当指定的哈希算法不支持时抛出</exception>
    public static string ComputeFingerprint(byte[] data, string algorithm = "SHA256", bool format = false)
    {
        byte[] hash;

        switch (algorithm.ToUpper())
        {
            case "SHA1":
            case "SHA-1":
                hash = SHA1.HashData(data);
                break;
            case "SHA256":
            case "SHA-256":
                hash = SHA256.HashData(data);
                break;
            case "SHA384":
            case "SHA-384":
                hash = SHA384.HashData(data);
                break;
            case "SHA512":
            case "SHA-512":
                hash = SHA512.HashData(data);
                break;
            case "MD5":
                hash = MD5.HashData(data);
                break;
            case "SM3":
                hash = SM3.HashData(data);
                break;
            default:
                throw new ArgumentException($"Unsupported hash algorithm: {algorithm}.");
        }

        return format ? FormatFingerprint(hash) : Convert.ToHexString(hash);
    }

    /// <summary>
    /// 格式化指纹字节数组为冒号分隔的十六进制字符串
    /// </summary>
    /// <param name="hash">哈希字节数组</param>
    /// <returns>格式化的指纹字符串，格式为 XX:XX:XX:...</returns>
    /// <example>
    /// 输入: [0x01, 0x23, 0xAB, 0xCD]
    /// 输出: "01:23:AB:CD"
    /// </example>
    public static string FormatFingerprint(byte[] hash) => string.Join(":", hash.Select(b => b.ToString("X2")));
}
