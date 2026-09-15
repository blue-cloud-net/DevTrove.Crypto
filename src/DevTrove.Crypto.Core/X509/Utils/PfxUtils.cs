using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.X509.Models;

namespace DevTrove.Crypto.X509.Utils;

/// <summary>
/// PKCS#12 (PFX) 导入导出工具。
/// 提供 PFX 的导出（含私钥、证书链、密码保护）与导入（解析为 <see cref="PfxBundle"/>）。
/// </summary>
public static class PfxUtils
{
    /// <summary>
    /// 导出为 PKCS#12 (PFX) 格式字节数组。
    /// </summary>
    /// <param name="certificate">要导出的证书。</param>
    /// <param name="password">PFX 密码，不能为空。</param>
    /// <param name="privateKey">私钥（可选；仅导出证书链时可省略）。</param>
    /// <param name="chain">证书链（不含本证书，顺序为颁发者依次向上，可选）。</param>
    /// <param name="friendlyName">友好名称（可选，默认使用主题 CN 或 "certificate"）。</param>
    /// <returns>PFX 字节数组。</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="certificate"/> 或 <paramref name="password"/> 为 <see langword="null"/> 时抛出。
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="password"/> 为空时抛出。</exception>
    public static byte[] ToPfx(
        Certificate certificate,
        string password,
        AsymmetricPrivateKeyParameter? privateKey = null,
        IReadOnlyList<Certificate>? chain = null,
        string? friendlyName = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length == 0)
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        var bcCertificate = certificate.GetBouncyCastleCertificate();
        var store = new Pkcs12StoreBuilder().Build();
        var certEntry = new X509CertificateEntry(bcCertificate);
        var name = friendlyName
                   ?? (bcCertificate.SubjectDN.GetValueList(X509Name.CN).Cast<string>().FirstOrDefault()
                       ?? "certificate");

        store.SetCertificateEntry(name, certEntry);

        if (chain is not null)
        {
            foreach (var chainCertificate in chain)
            {
                var chainName = chainCertificate
                    .GetBouncyCastleCertificate()
                    .SubjectDN
                    .GetValueList(X509Name.CN)
                    .Cast<string>()
                    .FirstOrDefault()
                    ?? "ca";
                store.SetCertificateEntry(chainName, new X509CertificateEntry(chainCertificate.GetBouncyCastleCertificate()));
            }
        }

        if (privateKey is not null)
        {
            var keyEntry = new AsymmetricKeyEntry(privateKey.GetBouncyCastleKey());
            store.SetKeyEntry(name, keyEntry, new[] { certEntry });
        }

        using var ms = new MemoryStream();
        store.Save(ms, password.ToCharArray(), new SecureRandom());
        return ms.ToArray();
    }

    /// <summary>
    /// 从 PKCS#12 (PFX) 字节数组导入证书、私钥与证书链。
    /// </summary>
    /// <param name="pfxData">PFX 字节数组。</param>
    /// <param name="password">PFX 密码。</param>
    /// <returns>包含证书、私钥与证书链的 <see cref="PfxBundle"/>。</returns>
    /// <exception cref="ArgumentNullException">参数为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="CryptographicException">密码错误或 PFX 中无有效条目时抛出。</exception>
    public static PfxBundle FromPfx(byte[] pfxData, string password)
    {
        ArgumentNullException.ThrowIfNull(pfxData);
        ArgumentNullException.ThrowIfNull(password);
        if (pfxData.Length == 0)
            throw new ArgumentException("PFX data cannot be empty.", nameof(pfxData));

        Pkcs12Store store;
        try
        {
            store = new Pkcs12StoreBuilder().Build();
            using var ms = new MemoryStream(pfxData, writable: false);
            store.Load(ms, password.ToCharArray());
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new CryptographicException("Failed to open the PKCS#12 store. The password may be incorrect.", ex);
        }

        var aliases = new List<string>();
        foreach (var alias in store.Aliases)
        {
            aliases.Add(alias as string ?? alias?.ToString() ?? string.Empty);
        }

        var keyAlias = aliases.FirstOrDefault(store.IsKeyEntry)
                       ?? aliases.FirstOrDefault(store.IsCertificateEntry);

        if (keyAlias is null)
        {
            throw new CryptographicException("No certificate or key entry found in the PKCS#12 store.");
        }

        var chainEntries = store.GetCertificateChain(keyAlias);
        if (chainEntries is null || chainEntries.Length == 0)
        {
            throw new CryptographicException("No certificate found in the PKCS#12 store.");
        }

        var leaf = new Certificate(chainEntries[0].Certificate);
        var chain = chainEntries.Skip(1).Select(entry => new Certificate(entry.Certificate)).ToList();

        AsymmetricPrivateKeyParameter? privateKey = null;
        if (store.IsKeyEntry(keyAlias))
        {
            var keyEntry = store.GetKey(keyAlias);
            privateKey = keyEntry is null ? null : new AsymmetricPrivateKeyParameter(keyEntry.Key);
        }

        return new PfxBundle(leaf, privateKey, chain, keyAlias);
    }
}
