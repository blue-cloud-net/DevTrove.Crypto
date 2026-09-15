namespace DevTrove.Crypto.TestSupport;

/// <summary>
/// OpenSSL / tongsuo 命令执行结果。
/// </summary>
public class OpenSslResult
{
    /// <summary>
    /// 退出码。
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// 标准输出。
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// 标准错误。
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// 命令是否成功执行。
    /// </summary>
    public bool IsSuccess => this.ExitCode == 0;

    /// <summary>
    /// 获取完整输出（标准输出 + 标准错误）。
    /// </summary>
    public string FullOutput =>
        string.IsNullOrEmpty(this.StandardError)
            ? this.StandardOutput
            : $"{this.StandardOutput}\n{this.StandardError}";
}
