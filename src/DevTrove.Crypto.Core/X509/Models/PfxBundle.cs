using DevTrove.Crypto.Crypto;

namespace DevTrove.Crypto.X509.Models;

/// <summary>
/// PFX / PKCS#12 包（Bundle）。
/// 表示一个 PFX 中的结构化内容：叶子证书、私钥（如有）、证书链和友好名称。
/// </summary>
public sealed class PfxBundle
{
    /// <summary>
    /// 叶子证书（PFX 中与私钥关联或第一个证书条目）。
    /// </summary>
    public Certificate Certificate { get; }

    /// <summary>
    /// 私钥（PFX 中不含私钥时为 <c>null</c>）。
    /// </summary>
    public AsymmetricPrivateKeyParameter? PrivateKey { get; }

    /// <summary>
    /// 证书链（不含叶子证书，顺序为颁发者依次向上）。
    /// </summary>
    public IReadOnlyList<Certificate> Chain { get; }

    /// <summary>
    /// PFX 条目友好名称。
    /// </summary>
    public string? FriendlyName { get; }

    /// <summary>
    /// 初始化 <see cref="PfxBundle"/> 的新实例。
    /// </summary>
    /// <param name="certificate">叶子证书。</param>
    /// <param name="privateKey">私钥（可为 <c>null</c>）。</param>
    /// <param name="chain">证书链。</param>
    /// <param name="friendlyName">友好名称。</param>
    public PfxBundle(
        Certificate certificate,
        AsymmetricPrivateKeyParameter? privateKey,
        IReadOnlyList<Certificate> chain,
        string? friendlyName)
    {
        this.Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        this.PrivateKey = privateKey;
        this.Chain = chain ?? throw new ArgumentNullException(nameof(chain));
        this.FriendlyName = friendlyName;
    }
}
