using DevTrove.Crypto.Crypto;
using DevTrove.Crypto.TestSupport;
using System.Security.Cryptography;

namespace DevTrove.Crypto.Core.Tests.Crypto;

/// <summary>
/// AES 与 OpenSSL 互操作测试：CBC / CFB / OFB 三种模式双向对照。
/// A. 代码加密 → openssl 解密；B. openssl 加密 → 代码解密。
/// CFB/OFB 使用全块（CFB-128 / OFB-128），与 openssl enc -aes-256-cfb / -aes-256-ofb 对应。
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "OpenSSL")]
public class AesOpenSslInteropTests : IDisposable
{
    private readonly string _tempDir;

    public AesOpenSslInteropTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aes_interop_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // 忽略清理错误。
            }
        }
    }

    /// <summary>
    /// 将明文填充到 16 字节块大小的整数倍（CFB/OFB 以块为单位处理）。
    /// </summary>
    private static byte[] PadToBlock(byte[] data)
    {
        var padded = new byte[((data.Length + 15) / 16) * 16];
        data.CopyTo(padded, 0);
        return padded;
    }

    /// <summary>
    /// 使用 AesCrypto 加密。
    /// </summary>
    private static byte[] CodeEncrypt(CipherMode mode, byte[] key, byte[] iv, byte[] plaintext)
    {
        using var aes = new AesCrypto();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = mode;
        aes.Padding = mode == CipherMode.CBC ? PaddingMode.PKCS7 : PaddingMode.None;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }

    /// <summary>
    /// 使用 AesCrypto 解密。
    /// </summary>
    private static byte[] CodeDecrypt(CipherMode mode, byte[] key, byte[] iv, byte[] ciphertext)
    {
        using var aes = new AesCrypto();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = mode;
        aes.Padding = mode == CipherMode.CBC ? PaddingMode.PKCS7 : PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    /// <summary>
    /// 代码加密 → openssl 解密。
    /// </summary>
    /// <param name="mode">密码模式。</param>
    /// <param name="encCipher">openssl enc 密码名（如 aes-256-cbc）。</param>
    /// <param name="usePadding">openssl 解密时是否使用默认 PKCS7 填充。</param>
    private async Task CodeEncrypt_OpenSslDecrypt_ShouldRoundTrip(CipherMode mode, string encCipher, bool usePadding)
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        using var temp = new AesCrypto();
        temp.GenerateKey();
        temp.GenerateIV();
        var key = temp.Key;
        var iv = temp.IV;

        var rawPlaintext = mode == CipherMode.CBC
            ? "AES-CBC interop from code"u8.ToArray()
            : PadToBlock("AES-CFB/OFB interop from code"u8.ToArray());

        var plainPath = Path.Combine(_tempDir, "plain.bin");
        var cipherPath = Path.Combine(_tempDir, "cipher.bin");
        var outPath = Path.Combine(_tempDir, "out.bin");
        await File.WriteAllBytesAsync(plainPath, rawPlaintext);

        // Act - 代码加密
        var ciphertext = CodeEncrypt(mode, key, iv, rawPlaintext);
        await File.WriteAllBytesAsync(cipherPath, ciphertext);

        // openssl 解密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var nopad = usePadding ? "" : " -nopad";
        var decrypt = await OpenSslCli.ExecuteCommandAsync(
            $"enc -d -{encCipher}{nopad} -K {keyHex} -iv {ivHex} -in \"{cipherPath}\" -out \"{outPath}\"");
        decrypt.IsSuccess.Should().BeTrue(decrypt.FullOutput);

        // Assert
        (await File.ReadAllBytesAsync(outPath)).Should().Equal(rawPlaintext);
    }

    /// <summary>
    /// openssl 加密 → 代码解密。
    /// </summary>
    private async Task OpenSslEncrypt_CodeDecrypt_ShouldRoundTrip(CipherMode mode, string encCipher, bool usePadding)
    {
        CliToolGuard.EnsureToolAvailable(OpenSslCli.BinaryPath);

        // Arrange
        using var temp = new AesCrypto();
        temp.GenerateKey();
        temp.GenerateIV();
        var key = temp.Key;
        var iv = temp.IV;

        var rawPlaintext = mode == CipherMode.CBC
            ? "AES-CBC interop from openssl"u8.ToArray()
            : PadToBlock("AES-CFB/OFB interop from openssl"u8.ToArray());

        var plainPath = Path.Combine(_tempDir, "plain.bin");
        var cipherPath = Path.Combine(_tempDir, "cipher.bin");
        await File.WriteAllBytesAsync(plainPath, rawPlaintext);

        // Act - openssl 加密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var nopad = usePadding ? "" : " -nopad";
        var encrypt = await OpenSslCli.ExecuteCommandAsync(
            $"enc -{encCipher}{nopad} -K {keyHex} -iv {ivHex} -in \"{plainPath}\" -out \"{cipherPath}\"");
        encrypt.IsSuccess.Should().BeTrue(encrypt.FullOutput);

        // 代码解密
        var decrypted = CodeDecrypt(mode, key, iv, await File.ReadAllBytesAsync(cipherPath));

        // Assert
        decrypted.Should().Equal(rawPlaintext);
    }

    #region CBC（PKCS7 填充）

    [Fact]
    public async Task AesCbc_CodeEncrypt_OpenSslDecrypt_ShouldSucceed()
        => await CodeEncrypt_OpenSslDecrypt_ShouldRoundTrip(CipherMode.CBC, "aes-256-cbc", usePadding: true);

    [Fact]
    public async Task AesCbc_OpenSslEncrypt_CodeDecrypt_ShouldSucceed()
        => await OpenSslEncrypt_CodeDecrypt_ShouldRoundTrip(CipherMode.CBC, "aes-256-cbc", usePadding: true);

    #endregion

    #region CFB（CFB-128，无填充）

    [Fact]
    public async Task AesCfb_CodeEncrypt_OpenSslDecrypt_ShouldSucceed()
        => await CodeEncrypt_OpenSslDecrypt_ShouldRoundTrip(CipherMode.CFB, "aes-256-cfb", usePadding: false);

    [Fact]
    public async Task AesCfb_OpenSslEncrypt_CodeDecrypt_ShouldSucceed()
        => await OpenSslEncrypt_CodeDecrypt_ShouldRoundTrip(CipherMode.CFB, "aes-256-cfb", usePadding: false);

    #endregion

    #region OFB（OFB-128，无填充）

    [Fact]
    public async Task AesOfb_CodeEncrypt_OpenSslDecrypt_ShouldSucceed()
        => await CodeEncrypt_OpenSslDecrypt_ShouldRoundTrip(CipherMode.OFB, "aes-256-ofb", usePadding: false);

    [Fact]
    public async Task AesOfb_OpenSslEncrypt_CodeDecrypt_ShouldSucceed()
        => await OpenSslEncrypt_CodeDecrypt_ShouldRoundTrip(CipherMode.OFB, "aes-256-ofb", usePadding: false);

    #endregion
}
