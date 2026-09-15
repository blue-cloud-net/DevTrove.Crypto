namespace DevTrove.Crypto.Crypto;

/// <summary>
/// AES 对称加密算法实现，基于 BouncyCastle。
/// 支持模式：CBC（默认，通过 <see cref="SymmetricAlgorithm"/> 接口）、GCM（通过高级方法）。
/// 安全约定：
/// <list type="bullet">
/// <item>严禁 ECB 模式；</item>
/// <item>GCM 模式使用 12 字节随机 Nonce，16 字节认证标签（128-bit tag）；</item>
/// <item>IV/Nonce 由调用方提供或通过 <see cref="GenerateIV"/> 自动生成。</item>
/// </list>
/// </summary>
public sealed class AesCrypto : SymmetricAlgorithm
{
    /// <summary>AES 块大小（位）</summary>
    public const int BlockSizeInBits = 128;

    /// <summary>AES 块大小（字节）</summary>
    public const int BlockSizeInBytes = 16;

    /// <summary>GCM 推荐 Nonce 长度（字节）</summary>
    public const int GcmNonceSizeInBytes = 12;

    /// <summary>GCM 认证标签长度（字节）</summary>
    public const int GcmTagSizeInBytes = 16;

    /// <summary>GCM 认证标签长度（位）</summary>
    public const int GcmTagSizeInBits = GcmTagSizeInBytes * 8;

    #region 构造函数

    /// <summary>
    /// 初始化 <see cref="AesCrypto"/> 的新实例。
    /// </summary>
    public AesCrypto()
    {
        LegalBlockSizesValue = [new KeySizes(BlockSizeInBits, BlockSizeInBits, 0)];
        LegalKeySizesValue =
        [
            new KeySizes(128, 128, 0),
            new KeySizes(192, 192, 0),
            new KeySizes(256, 256, 0),
        ];

        BlockSizeValue = BlockSizeInBits;
        KeySizeValue = 256;
        FeedbackSizeValue = BlockSizeInBits;
        ModeValue = CipherMode.CBC;
        PaddingValue = PaddingMode.PKCS7;

        this.GenerateKey();
        this.GenerateIV();
    }

    #endregion

    #region SymmetricAlgorithm 属性重写

    /// <inheritdoc />
    /// <exception cref="CryptographicException">不允许设置 ECB 模式。</exception>
    public override CipherMode Mode
    {
        get => ModeValue;
        set
        {
            if (value is CipherMode.ECB)
                throw new CryptographicException("ECB mode is not allowed due to security constraints.");
            if (value is not CipherMode.CBC and not CipherMode.CFB and not CipherMode.OFB)
                throw new CryptographicException($"Unsupported cipher mode: {value}. Supported: CBC, CFB, OFB.");
            ModeValue = value;
        }
    }

    /// <inheritdoc />
    public override PaddingMode Padding
    {
        get => PaddingValue;
        set
        {
            if (value is not PaddingMode.None
                and not PaddingMode.PKCS7
                and not PaddingMode.Zeros
                and not PaddingMode.ANSIX923
                and not PaddingMode.ISO10126)
            {
                throw new CryptographicException($"Unsupported padding mode: {value}.");
            }
            PaddingValue = value;
        }
    }

    #endregion

    #region 密钥和 IV 生成

    /// <inheritdoc />
    public override void GenerateKey() => KeyValue = RandomNumberGenerator.GetBytes(KeySizeValue / 8);

    /// <summary>
    /// 生成 CBC 模式用的随机 16 字节 IV。
    /// </summary>
    public override void GenerateIV() => IVValue = RandomNumberGenerator.GetBytes(BlockSizeInBytes);

    /// <summary>
    /// 生成 GCM 模式用的随机 12 字节 Nonce。
    /// </summary>
    /// <returns>12 字节随机 Nonce。</returns>
    public static byte[] GenerateGcmNonce() => RandomNumberGenerator.GetBytes(GcmNonceSizeInBytes);

    #endregion

    #region CBC 加密器/解密器（ICryptoTransform）

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        => this.CreateCbcTransform(rgbKey, rgbIV, forEncryption: true);

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        => this.CreateCbcTransform(rgbKey, rgbIV, forEncryption: false);

    private ICryptoTransform CreateCbcTransform(byte[] key, byte[]? iv, bool forEncryption)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length is not 16 and not 24 and not 32)
            throw new ArgumentException("Key must be 16, 24, or 32 bytes (128, 192, or 256 bits).", nameof(key));
        ArgumentNullException.ThrowIfNull(iv);
        if (iv.Length != BlockSizeInBytes)
            throw new ArgumentException($"IV must be {BlockSizeInBytes} bytes.", nameof(iv));

        IBlockCipher engine = new AesEngine();

        // 按 Mode 选择分组模式：CBC / CFB-128 / OFB-128。
        IBlockCipher cipher = ModeValue switch
        {
            CipherMode.CBC => new CbcBlockCipher(engine),
            CipherMode.CFB => new CfbBlockCipher(engine, BlockSizeInBits),
            CipherMode.OFB => new OfbBlockCipher(engine, BlockSizeInBits),
            _ => throw new CryptographicException($"Unsupported cipher mode: {ModeValue}."),
        };

        IBlockCipherPadding? padding = PaddingValue switch
        {
            PaddingMode.None => null,
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroBytePadding(),
            PaddingMode.ANSIX923 => new X923Padding(),
            PaddingMode.ISO10126 => new ISO10126d2Padding(),
            _ => new Pkcs7Padding(),
        };

        var buffered = padding is not null
            ? new PaddedBufferedBlockCipher(cipher, padding)
            : new BufferedBlockCipher(cipher);

        buffered.Init(forEncryption, new ParametersWithIV(new KeyParameter(key), iv));
        return new AesCbcTransform(buffered, BlockSizeInBytes);
    }

    #endregion

    #region GCM 高级方法

    /// <summary>
    /// 使用 AES-GCM 加密数据（含认证加密，自动生成或使用指定 Nonce）。
    /// 输出格式：<c>[12 字节 Nonce][密文][16 字节 GCM Tag]</c>。
    /// </summary>
    /// <param name="plaintext">明文字节数组。</param>
    /// <param name="key">AES 密钥（16、24 或 32 字节）。</param>
    /// <param name="nonce">
    /// 12 字节 Nonce；传入 <c>null</c> 时自动生成随机 Nonce 并嵌入输出。
    /// 同一密钥下每次加密必须使用不同 Nonce。
    /// </param>
    /// <param name="associatedData">附加认证数据（AAD），可为 <c>null</c>。</param>
    /// <returns>格式为 <c>[Nonce(12)][密文][Tag(16)]</c> 的字节数组。</returns>
    /// <exception cref="ArgumentException">密钥或 Nonce 长度不合规时抛出。</exception>
    public static byte[] EncryptGcm(
        byte[] plaintext,
        byte[] key,
        byte[]? nonce = null,
        byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length is not 16 and not 24 and not 32)
            throw new ArgumentException("Key must be 16, 24, or 32 bytes.", nameof(key));

        nonce ??= GenerateGcmNonce();

        if (nonce.Length != GcmNonceSizeInBytes)
            throw new ArgumentException($"Nonce must be {GcmNonceSizeInBytes} bytes for GCM.", nameof(nonce));

        var gcm = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(key),
            macSize: GcmTagSizeInBits,
            nonce,
            associatedData);

        gcm.Init(true, parameters);

        var ciphertextWithTag = new byte[gcm.GetOutputSize(plaintext.Length)];
        var bytesWritten = gcm.ProcessBytes(plaintext, 0, plaintext.Length, ciphertextWithTag, 0);
        bytesWritten += gcm.DoFinal(ciphertextWithTag, bytesWritten);

        // 输出：nonce(12) + 密文 + tag(16)
        var output = new byte[GcmNonceSizeInBytes + bytesWritten];
        nonce.CopyTo(output, 0);
        ciphertextWithTag.AsSpan(0, bytesWritten).CopyTo(output.AsSpan(GcmNonceSizeInBytes));
        return output;
    }

    /// <summary>
    /// 使用 AES-GCM 解密数据（含认证验证）。
    /// 输入格式（由 <see cref="EncryptGcm"/> 生成）：<c>[12 字节 Nonce][密文][16 字节 GCM Tag]</c>。
    /// </summary>
    /// <param name="ciphertextWithNonce">格式为 <c>[Nonce(12)][密文][Tag(16)]</c> 的字节数组。</param>
    /// <param name="key">AES 密钥（16、24 或 32 字节）。</param>
    /// <param name="associatedData">附加认证数据（AAD），必须与加密时一致，可为 <c>null</c>。</param>
    /// <returns>解密后的明文字节数组。</returns>
    /// <exception cref="ArgumentException">输入长度不足时抛出。</exception>
    /// <exception cref="CryptographicException">GCM 认证标签验证失败（数据被篡改）时抛出。</exception>
    public static byte[] DecryptGcm(
        byte[] ciphertextWithNonce,
        byte[] key,
        byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(ciphertextWithNonce);
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length is not 16 and not 24 and not 32)
            throw new ArgumentException("Key must be 16, 24, or 32 bytes.", nameof(key));

        var minLength = GcmNonceSizeInBytes + GcmTagSizeInBytes;
        if (ciphertextWithNonce.Length < minLength)
            throw new ArgumentException(
                $"Input must be at least {minLength} bytes (nonce + tag). Got {ciphertextWithNonce.Length} bytes.",
                nameof(ciphertextWithNonce));

        var nonce = ciphertextWithNonce.AsSpan(0, GcmNonceSizeInBytes).ToArray();
        var ciphertextAndTag = ciphertextWithNonce.AsSpan(GcmNonceSizeInBytes).ToArray();

        var gcm = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(key),
            macSize: GcmTagSizeInBits,
            nonce,
            associatedData);

        gcm.Init(false, parameters);

        try
        {
            var plaintext = new byte[gcm.GetOutputSize(ciphertextAndTag.Length)];
            var bytesWritten = gcm.ProcessBytes(ciphertextAndTag, 0, ciphertextAndTag.Length, plaintext, 0);
            bytesWritten += gcm.DoFinal(plaintext, bytesWritten);

            if (bytesWritten < plaintext.Length)
            {
                var result = new byte[bytesWritten];
                plaintext.AsSpan(0, bytesWritten).CopyTo(result);
                return result;
            }
            return plaintext;
        }
        catch (InvalidCipherTextException ex)
        {
            throw new CryptographicException("GCM authentication tag verification failed. Data may have been tampered with.", ex);
        }
    }

    #endregion

    #region 内部 CBC Transform 实现

    private sealed class AesCbcTransform : ICryptoTransform
    {
        private readonly BufferedBlockCipher _cipher;
        private readonly int _blockSize;
        private bool _disposed;

        public AesCbcTransform(BufferedBlockCipher cipher, int blockSize)
        {
            _cipher = cipher;
            _blockSize = blockSize;
        }

        public bool CanReuseTransform => false;
        public bool CanTransformMultipleBlocks => true;
        public int InputBlockSize => _blockSize;
        public int OutputBlockSize => _blockSize;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AesCbcTransform));
            ArgumentNullException.ThrowIfNull(inputBuffer);
            ArgumentNullException.ThrowIfNull(outputBuffer);
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputCount < 0) throw new ArgumentOutOfRangeException(nameof(inputCount));
            if (inputOffset + inputCount > inputBuffer.Length)
                throw new ArgumentException("Input buffer too small.");
            if (outputOffset < 0) throw new ArgumentOutOfRangeException(nameof(outputOffset));

            var outputLength = _cipher.GetUpdateOutputSize(inputCount);
            if (outputOffset + outputLength > outputBuffer.Length)
                throw new ArgumentException("Output buffer too small.");

            return _cipher.ProcessBytes(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AesCbcTransform));
            ArgumentNullException.ThrowIfNull(inputBuffer);
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputCount < 0) throw new ArgumentOutOfRangeException(nameof(inputCount));
            if (inputOffset + inputCount > inputBuffer.Length)
                throw new ArgumentException("Input buffer too small.");

            try
            {
                var output = new byte[_cipher.GetOutputSize(inputCount)];
                var len = _cipher.ProcessBytes(inputBuffer, inputOffset, inputCount, output, 0);
                len += _cipher.DoFinal(output, len);

                if (len < output.Length)
                {
                    var result = new byte[len];
                    Array.Copy(output, 0, result, 0, len);
                    return result;
                }
                return output;
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error occurred during AES CBC cryptographic operation.", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cipher.Reset();
                _disposed = true;
            }
        }
    }

    #endregion
}
