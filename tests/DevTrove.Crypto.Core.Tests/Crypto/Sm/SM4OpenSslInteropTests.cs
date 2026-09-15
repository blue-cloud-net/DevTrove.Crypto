using DevTrove.Crypto.Crypto.Sm;
using DevTrove.Crypto.TestSupport;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace DevTrove.Crypto.Core.Tests.Crypto.Sm;

/// <summary>
/// SM4 与 OpenSSL 互操作性测试
/// 需要系统安装 OpenSSL 且支持 SM4
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Tongsuo")]
public class SM4OpenSslInteropTests : IDisposable
{
    private string _tempDir = null!;

    public SM4OpenSslInteropTests()
    {
        CliToolGuard.EnsureToolAvailable(TongsuoCli.BinaryPath);
        _tempDir = Path.Combine(Path.GetTempPath(), $"sm4_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// 使用 SM4 加密数据的辅助方法
    /// </summary>
    private static byte[] Sm4Encrypt(SM4 sm4, byte[] plaintext)
    {
        using var encryptor = sm4.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(plaintext, 0, plaintext.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    /// <summary>
    /// 使用 SM4 解密数据的辅助方法
    /// </summary>
    private static byte[] Sm4Decrypt(SM4 sm4, byte[] ciphertext)
    {
        using var decryptor = sm4.CreateDecryptor();
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
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
                // 忽略清理错误
            }
        }
    }

    #region 标准测试向量测试

    [Theory]
    [InlineData("0123456789abcdeffedcba9876543210", "0123456789abcdeffedcba9876543210",
        "681edf34d206965e86b3e94f536e4246")]
    [InlineData("0123456789abcdeffedcba9876543210", "fedcba98765432100123456789abcdef",
        "f0a2b07e64dd2c2590f93e4edd90fbb4")]
    public async Task EncryptDecrypt_WithStandardTestVectors_MatchesOpenSSL(
        string keyHex, string plaintextHex, string expectedCiphertextHex)
    {
        // Arrange
        var key = Convert.FromHexString(keyHex);
        var plaintext = Convert.FromHexString(plaintextHex);
        var expectedCiphertext = Convert.FromHexString(expectedCiphertextHex);

        var keyPath = Path.Combine(_tempDir, "key.bin");
        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");

        await File.WriteAllBytesAsync(keyPath, key);
        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.ECB;
        sm4.Padding = PaddingMode.None;

        byte[] ciphertext;
        using (var encryptor = sm4.CreateEncryptor())
        using (var ms = new MemoryStream())
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            ciphertext = ms.ToArray();
        }

        // Assert - 验证与标准测试向量一致
        ciphertext.Should().Equal(expectedCiphertext,
            "SM4 encryption should match standard test vector");

        // Act - OpenSSL 加密
        var opensslResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -sm4-ecb -nopad -K {keyHex} -in \"{plaintextPath}\" -out \"{ciphertextPath}\"");

        opensslResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(ciphertextPath);

        // Assert - C# 和 OpenSSL 结果一致
        ciphertext.Should().Equal(opensslCiphertext,
            "C# and OpenSSL encryption should produce identical results");
    }

    #endregion

    #region ECB 模式测试

    [Fact]
    public async Task EncryptECB_SimpleText_MatchesOpenSSL()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        var key = tempSm4.Key;
        var plaintext = "Hello, SM4! This is a test message for ECB mode."u8.ToArray();

        // 填充到块大小的倍数
        var paddedLength = ((plaintext.Length + 15) / 16) * 16;
        var paddedPlaintext = new byte[paddedLength];
        Array.Copy(plaintext, paddedPlaintext, plaintext.Length);

        var keyPath = Path.Combine(_tempDir, "key.bin");
        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");
        var decryptedPath = Path.Combine(_tempDir, "decrypted.bin");

        await File.WriteAllBytesAsync(keyPath, key);
        await File.WriteAllBytesAsync(plaintextPath, paddedPlaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.ECB;
        sm4.Padding = PaddingMode.None;

        var ciphertext = sm4.EncryptEcb(paddedPlaintext, PaddingMode.None);
        await File.WriteAllBytesAsync(ciphertextPath, ciphertext);

        // OpenSSL 解密
        var keyHex = Convert.ToHexString(key);
        var decryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -d -sm4-ecb -nopad -K {keyHex} -in \"{ciphertextPath}\" -out \"{decryptedPath}\"");

        decryptResult.ExitCode.Should().Be(0, "OpenSSL decryption should succeed");
        var decrypted = await File.ReadAllBytesAsync(decryptedPath);

        // Assert
        decrypted.Should().Equal(paddedPlaintext,
            "OpenSSL should decrypt C# encrypted data correctly");
    }

    [Fact]
    public async Task DecryptECB_OpenSSLEncrypted_Success()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        var key = tempSm4.Key;
        var plaintext = "Test message for OpenSSL to C# decryption."u8.ToArray();

        // 填充到块大小的倍数
        var paddedLength = ((plaintext.Length + 15) / 16) * 16;
        var paddedPlaintext = new byte[paddedLength];
        Array.Copy(plaintext, paddedPlaintext, plaintext.Length);

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");

        await File.WriteAllBytesAsync(plaintextPath, paddedPlaintext);

        // Act - OpenSSL 加密
        var keyHex = Convert.ToHexString(key);
        var encryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -sm4-ecb -nopad -K {keyHex} -in \"{plaintextPath}\" -out \"{ciphertextPath}\"");

        encryptResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(ciphertextPath);

        // C# 解密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.ECB;
        sm4.Padding = PaddingMode.None;

        var decrypted = sm4.DecryptEcb(opensslCiphertext, PaddingMode.None);

        // Assert
        decrypted.Should().Equal(paddedPlaintext,
            "C# should decrypt OpenSSL encrypted data correctly");
    }

    #endregion

    #region CBC 模式测试

    [Fact]
    public async Task EncryptCBC_SimpleText_MatchesOpenSSL()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;
        var plaintext = "Hello, SM4! This is a test message for CBC mode."u8.ToArray();

        // 填充到块大小的倍数
        var paddedLength = ((plaintext.Length + 15) / 16) * 16;
        var paddedPlaintext = new byte[paddedLength];
        Array.Copy(plaintext, paddedPlaintext, plaintext.Length);

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");
        var decryptedPath = Path.Combine(_tempDir, "decrypted.bin");

        await File.WriteAllBytesAsync(plaintextPath, paddedPlaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.CBC;
        sm4.IV = iv;
        sm4.Padding = PaddingMode.None;

        var ciphertext = sm4.EncryptCbc(paddedPlaintext, iv, PaddingMode.None);
        await File.WriteAllBytesAsync(ciphertextPath, ciphertext);

        // OpenSSL 解密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var decryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -d -sm4-cbc -nopad -K {keyHex} -iv {ivHex} -in \"{ciphertextPath}\" -out \"{decryptedPath}\"");

        decryptResult.ExitCode.Should().Be(0, "OpenSSL decryption should succeed");
        var decrypted = await File.ReadAllBytesAsync(decryptedPath);

        // Assert
        decrypted.Should().Equal(paddedPlaintext,
            "OpenSSL should decrypt C# encrypted data correctly");
    }

    [Fact]
    public async Task DecryptCBC_OpenSSLEncrypted_Success()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;
        var plaintext = "Test message for OpenSSL to C# CBC decryption."u8.ToArray();

        // 填充到块大小的倍数
        var paddedLength = ((plaintext.Length + 15) / 16) * 16;
        var paddedPlaintext = new byte[paddedLength];
        Array.Copy(plaintext, paddedPlaintext, plaintext.Length);

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");

        await File.WriteAllBytesAsync(plaintextPath, paddedPlaintext);

        // Act - OpenSSL 加密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var encryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -sm4-cbc -nopad -K {keyHex} -iv {ivHex} -in \"{plaintextPath}\" -out \"{ciphertextPath}\"");

        encryptResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(ciphertextPath);

        // C# 解密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.CBC;
        sm4.IV = iv;
        sm4.Padding = PaddingMode.None;

        var decrypted = sm4.DecryptCbc(opensslCiphertext, iv, PaddingMode.None);

        // Assert
        decrypted.Should().Equal(paddedPlaintext,
            "C# should decrypt OpenSSL encrypted data correctly");
    }

    #endregion

    #region CFB / OFB 模式测试

    [Theory]
    [InlineData(CipherMode.CFB, "sm4-cfb")]
    [InlineData(CipherMode.OFB, "sm4-ofb")]
    public async Task EncryptStreamMode_SimpleText_MatchesOpenSSL(CipherMode mode, string encCipher)
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;
        var plaintext = "Hello, SM4! Stream mode test message."u8.ToArray();

        // 填充到块大小的倍数（CFB/OFB 以 128 位全块处理）
        var paddedLength = ((plaintext.Length + 15) / 16) * 16;
        var paddedPlaintext = new byte[paddedLength];
        Array.Copy(plaintext, paddedPlaintext, plaintext.Length);

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");
        var decryptedPath = Path.Combine(_tempDir, "decrypted.bin");

        await File.WriteAllBytesAsync(plaintextPath, paddedPlaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.IV = iv;
        sm4.Mode = mode;
        sm4.Padding = PaddingMode.None;

        var ciphertext = Sm4Encrypt(sm4, paddedPlaintext);
        await File.WriteAllBytesAsync(ciphertextPath, ciphertext);

        // OpenSSL 解密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var decryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -d -{encCipher} -nopad -K {keyHex} -iv {ivHex} -in \"{ciphertextPath}\" -out \"{decryptedPath}\"");

        decryptResult.ExitCode.Should().Be(0, "OpenSSL decryption should succeed");
        var decrypted = await File.ReadAllBytesAsync(decryptedPath);

        // Assert
        decrypted.Should().Equal(paddedPlaintext,
            "OpenSSL should decrypt C# encrypted data correctly");
    }

    [Theory]
    [InlineData(CipherMode.CFB, "sm4-cfb")]
    [InlineData(CipherMode.OFB, "sm4-ofb")]
    public async Task DecryptStreamMode_OpenSSLEncrypted_Success(CipherMode mode, string encCipher)
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;
        var plaintext = "Test message for OpenSSL to C# stream mode."u8.ToArray();

        // 填充到块大小的倍数
        var paddedLength = ((plaintext.Length + 15) / 16) * 16;
        var paddedPlaintext = new byte[paddedLength];
        Array.Copy(plaintext, paddedPlaintext, plaintext.Length);

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");

        await File.WriteAllBytesAsync(plaintextPath, paddedPlaintext);

        // Act - OpenSSL 加密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var encryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -{encCipher} -nopad -K {keyHex} -iv {ivHex} -in \"{plaintextPath}\" -out \"{ciphertextPath}\"");

        encryptResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(ciphertextPath);

        // C# 解密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.IV = iv;
        sm4.Mode = mode;
        sm4.Padding = PaddingMode.None;

        var decrypted = Sm4Decrypt(sm4, opensslCiphertext);

        // Assert
        decrypted.Should().Equal(paddedPlaintext,
            "C# should decrypt OpenSSL encrypted data correctly");
    }

    #endregion

    #region PKCS7 填充测试

    [Fact]
    public async Task EncryptECB_WithPKCS7Padding_MatchesOpenSSL()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        var key = tempSm4.Key;
        var plaintext = "This text needs padding!"u8.ToArray();

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");
        var decryptedPath = Path.Combine(_tempDir, "decrypted.bin");

        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.ECB;
        sm4.Padding = PaddingMode.PKCS7;

        var ciphertext = sm4.EncryptEcb(plaintext, PaddingMode.PKCS7);
        await File.WriteAllBytesAsync(ciphertextPath, ciphertext);

        // OpenSSL 解密
        var keyHex = Convert.ToHexString(key);
        var decryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -d -sm4-ecb -K {keyHex} -in \"{ciphertextPath}\" -out \"{decryptedPath}\"");

        decryptResult.ExitCode.Should().Be(0, "OpenSSL decryption should succeed");
        var decrypted = await File.ReadAllBytesAsync(decryptedPath);

        // Assert
        decrypted.Should().Equal(plaintext,
            "OpenSSL should decrypt C# PKCS7-padded data correctly");
    }

    [Fact]
    public async Task DecryptCBC_WithPKCS7Padding_OpenSSLEncrypted_Success()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;
        var plaintext = "Test PKCS7 padding with CBC!"u8.ToArray();

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");

        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - OpenSSL 加密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var encryptResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -sm4-cbc -K {keyHex} -iv {ivHex} -in \"{plaintextPath}\" -out \"{ciphertextPath}\"");

        encryptResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(ciphertextPath);

        // C# 解密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.CBC;
        sm4.IV = iv;
        sm4.Padding = PaddingMode.PKCS7;

        var decrypted = sm4.DecryptCbc(opensslCiphertext, iv, PaddingMode.PKCS7);

        // Assert
        decrypted.Should().Equal(plaintext,
            "C# should decrypt OpenSSL PKCS7-padded data correctly");
    }

    #endregion

    #region 大数据测试

    [Fact]
    public async Task EncryptDecrypt_LargeData_MatchesOpenSSL()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;

        // 生成 1MB 测试数据
        var plaintext = new byte[1024 * 1024];
        RandomNumberGenerator.Fill(plaintext);

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext_cs.bin");
        var opensslCiphertextPath = Path.Combine(_tempDir, "ciphertextTongsuoCli.bin");

        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.CBC;
        sm4.IV = iv;
        sm4.Padding = PaddingMode.PKCS7;

        var csCiphertext = sm4.EncryptCbc(plaintext, iv, PaddingMode.PKCS7);
        await File.WriteAllBytesAsync(ciphertextPath, csCiphertext);

        // OpenSSL 加密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var opensslResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -sm4-cbc -K {keyHex} -iv {ivHex} -in \"{plaintextPath}\" -out \"{opensslCiphertextPath}\"");

        opensslResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(opensslCiphertextPath);

        // Assert
        csCiphertext.Should().Equal(opensslCiphertext,
            "C# and OpenSSL should produce identical ciphertext for large data");
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task Encrypt_EmptyData_MatchesOpenSSL()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;
        var plaintext = Array.Empty<byte>();

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");

        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.CBC;
        sm4.IV = iv;
        sm4.Padding = PaddingMode.PKCS7;

        var csCiphertext = sm4.EncryptCbc(plaintext, iv, PaddingMode.PKCS7);

        // OpenSSL 加密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var opensslResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -sm4-cbc -K {keyHex} -iv {ivHex} -in \"{plaintextPath}\" -out \"{ciphertextPath}\"");

        opensslResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(ciphertextPath);

        // Assert - 空数据使用 PKCS7 填充后应该是一个完整块
        csCiphertext.Should().HaveCount(16, "Empty data with PKCS7 padding should be one block");
        opensslCiphertext.Should().HaveCount(16, "OpenSSL empty data with PKCS7 padding should be one block");
        csCiphertext.Should().Equal(opensslCiphertext,
            "C# and OpenSSL should produce identical ciphertext for empty data");
    }

    [Fact]
    public async Task Encrypt_ExactBlockSize_MatchesOpenSSL()
    {
        // Arrange
        using var tempSm4 = new SM4();
        tempSm4.GenerateKey();
        tempSm4.GenerateIV();
        var key = tempSm4.Key;
        var iv = tempSm4.IV;
        var plaintext = new byte[16]; // 正好一个块
        RandomNumberGenerator.Fill(plaintext);

        var plaintextPath = Path.Combine(_tempDir, "plaintext.bin");
        var ciphertextPath = Path.Combine(_tempDir, "ciphertext.bin");

        await File.WriteAllBytesAsync(plaintextPath, plaintext);

        // Act - C# 加密
        using var sm4 = new SM4();
        sm4.Key = key;
        sm4.Mode = CipherMode.CBC;
        sm4.IV = iv;
        sm4.Padding = PaddingMode.PKCS7;

        var csCiphertext = sm4.EncryptCbc(plaintext, iv, PaddingMode.PKCS7);

        // OpenSSL 加密
        var keyHex = Convert.ToHexString(key);
        var ivHex = Convert.ToHexString(iv);
        var opensslResult = await TongsuoCli.ExecuteCommandAsync(
            $"enc -sm4-cbc -K {keyHex} -iv {ivHex} -in \"{plaintextPath}\" -out \"{ciphertextPath}\"");

        opensslResult.ExitCode.Should().Be(0, "OpenSSL encryption should succeed");
        var opensslCiphertext = await File.ReadAllBytesAsync(ciphertextPath);

        // Assert - 正好一个块使用 PKCS7 填充后应该是两个块
        csCiphertext.Should().HaveCount(32, "One block with PKCS7 padding should result in two blocks");
        opensslCiphertext.Should().HaveCount(32, "OpenSSL one block with PKCS7 padding should be two blocks");
        csCiphertext.Should().Equal(opensslCiphertext,
            "C# and OpenSSL should produce identical ciphertext for exact block size");
    }

    #endregion
}
