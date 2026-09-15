namespace DevTrove.Crypto.Crypto.Sm;

/// <summary>
/// 表示必须从其继承 SM4 的所有实现的抽象基类
/// </summary>
public sealed class SM4 : SymmetricAlgorithm
{
    /// <summary>
    /// SM4 算法的密钥大小（位）
    /// </summary>
    public const int KeySizeInBits = 128;

    /// <summary>
    /// SM4 算法的密钥大小（字节）
    /// </summary>
    public const int KeySizeInBytes = 16;

    /// <summary>
    /// SM4 算法的块大小（位）
    /// </summary>
    public const int BlockSizeInBits = 128;

    /// <summary>
    /// SM4 算法的块大小（字节）
    /// </summary>
    public const int BlockSizeInBytes = 16;

    #region 构造函数

    /// <summary>
    /// 初始化 SM4 类的新实例
    /// </summary>
    public SM4()
    {
        // SM4 固定参数
        LegalBlockSizesValue = new[] { new KeySizes(BlockSizeInBits, BlockSizeInBits, 0) };
        LegalKeySizesValue = new[] { new KeySizes(KeySizeInBits, KeySizeInBits, 0) };

        BlockSizeValue = BlockSizeInBits;
        KeySizeValue = KeySizeInBits;
        FeedbackSizeValue = BlockSizeInBits;

        // 默认模式和填充
        ModeValue = CipherMode.CBC;
        PaddingValue = PaddingMode.PKCS7;

        // 生成随机密钥和 IV
        this.GenerateKey();
        this.GenerateIV();
    }

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置对称算法的运算模式
    /// </summary>
    public override CipherMode Mode
    {
        get => ModeValue;
        set
        {
            // SM4 支持的模式：ECB, CBC, CFB, OFB, CTS
            if (value != CipherMode.ECB &&
                value != CipherMode.CBC &&
                value != CipherMode.CFB &&
                value != CipherMode.OFB &&
                value != CipherMode.CTS)
            {
                throw new CryptographicException($"Specified cipher mode is not valid for this algorithm: {value}");
            }

            ModeValue = value;
        }
    }

    /// <summary>
    /// 获取或设置对称算法的填充模式
    /// </summary>
    public override PaddingMode Padding
    {
        get => PaddingValue;
        set
        {
            if (value is
                not PaddingMode.None and
                not PaddingMode.PKCS7 and
                not PaddingMode.Zeros and
                not PaddingMode.ANSIX923 and
                not PaddingMode.ISO10126)
            {
                throw new CryptographicException($"Specified padding mode is not valid for this algorithm: {value}");
            }

            PaddingValue = value;
        }
    }

    #endregion

    #region 密钥和 IV 生成

    /// <summary>
    /// 生成用于该算法的随机密钥
    /// </summary>
    public override void GenerateKey() => KeyValue = RandomNumberGenerator.GetBytes(KeySizeInBytes);

    /// <summary>
    /// 生成用于该算法的随机初始化向量
    /// </summary>
    public override void GenerateIV() => IVValue = RandomNumberGenerator.GetBytes(BlockSizeInBytes);

    #endregion

    #region 创建加密器和解密器

    /// <summary>
    /// 使用指定的密钥和初始化向量创建对称加密器对象
    /// </summary>
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        if (rgbKey == null)
            throw new ArgumentNullException(nameof(rgbKey));

        if (rgbKey.Length != KeySizeInBytes)
            throw new ArgumentException($"Specified key is not a valid size for this algorithm. Expected {KeySizeInBytes} bytes.", nameof(rgbKey));

        if (ModeValue != CipherMode.ECB)
        {
            if (rgbIV == null)
                throw new ArgumentNullException(nameof(rgbIV));

            if (rgbIV.Length != BlockSizeInBytes)
                throw new ArgumentException($"Specified initialization vector (IV) does not match the block size for this algorithm. Expected {BlockSizeInBytes} bytes.", nameof(rgbIV));
        }

        return this.CreateCryptoTransform(rgbKey, rgbIV, true);
    }

    /// <summary>
    /// 使用指定的密钥和初始化向量创建对称解密器对象
    /// </summary>
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        if (rgbKey == null)
            throw new ArgumentNullException(nameof(rgbKey));

        if (rgbKey.Length != KeySizeInBytes)
            throw new ArgumentException($"Specified key is not a valid size for this algorithm. Expected {KeySizeInBytes} bytes.", nameof(rgbKey));

        if (ModeValue != CipherMode.ECB)
        {
            if (rgbIV == null)
                throw new ArgumentNullException(nameof(rgbIV));

            if (rgbIV.Length != BlockSizeInBytes)
                throw new ArgumentException($"Specified initialization vector (IV) does not match the block size for this algorithm. Expected {BlockSizeInBytes} bytes.", nameof(rgbIV));
        }

        return this.CreateCryptoTransform(rgbKey, rgbIV, false);
    }

    #endregion

    #region 内部实现

    /// <summary>
    /// 创建加密/解密转换器
    /// </summary>
    private ICryptoTransform CreateCryptoTransform(byte[] key, byte[]? iv, bool forEncryption)
    {
        IBlockCipher engine = new SM4Engine();
        IBlockCipher cipher;

        // 根据模式选择合适的密码器
        cipher = ModeValue switch
        {
            CipherMode.ECB => engine,
            CipherMode.CBC => new CbcBlockCipher(engine),
            CipherMode.CFB => new CfbBlockCipher(engine, BlockSizeInBits),
            CipherMode.OFB => new OfbBlockCipher(engine, BlockSizeInBits),
            CipherMode.CTS => new CbcBlockCipher(engine),
            _ => throw new CryptographicException($"Cipher mode not supported: {ModeValue}")
        };

        // 根据填充模式选择合适的填充器
        IBlockCipherPadding? padding = PaddingValue switch
        {
            PaddingMode.None => null,
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroBytePadding(),
            PaddingMode.ANSIX923 => new X923Padding(),
            PaddingMode.ISO10126 => new ISO10126d2Padding(),
            _ => throw new CryptographicException($"Padding mode not supported: {PaddingValue}")
        };

        BufferedBlockCipher bufferedCipher;
        if (ModeValue is CipherMode.CTS)
        {
            bufferedCipher = new CtsBlockCipher(cipher);
        }
        else if (padding != null)
        {
            bufferedCipher = new PaddedBufferedBlockCipher(cipher, padding);
        }
        else
        {
            bufferedCipher = new BufferedBlockCipher(cipher);
        }

        var keyParam = new KeyParameter(key);
        ICipherParameters parameters = iv != null && ModeValue != CipherMode.ECB
            ? new ParametersWithIV(keyParam, iv)
            : keyParam;

        bufferedCipher.Init(forEncryption, parameters);

        return new Sm4CryptoTransform(bufferedCipher, BlockSizeInBytes);
    }

    #endregion

    #region 重写核心加密解密方法

    /// <summary>
    /// 尝试使用 ECB 模式加密数据到目标缓冲区
    /// </summary>
    protected override bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination,
        PaddingMode paddingMode, out int bytesWritten)
    {
        if (KeyValue is null)
        {
            throw new CryptographicException("Key is not set.");
        }

        try
        {
            var result = EncryptEcbInternal(plaintext.ToArray(), KeyValue, paddingMode);
            if (result.Length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            result.CopyTo(destination);
            bytesWritten = result.Length;
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// 尝试使用 ECB 模式解密数据到目标缓冲区
    /// </summary>
    protected override bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination,
        PaddingMode paddingMode, out int bytesWritten)
    {
        if (KeyValue is null)
        {
            throw new CryptographicException("Key is not set.");
        }

        try
        {
            var result = DecryptEcbInternal(ciphertext.ToArray(), KeyValue, paddingMode);
            if (result.Length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            result.CopyTo(destination);
            bytesWritten = result.Length;
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// 尝试使用 CBC 模式加密数据到目标缓冲区
    /// </summary>
    protected override bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv,
        Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
    {
        if (KeyValue is null)
        {
            throw new CryptographicException("Key is not set.");
        }

        try
        {
            var result = EncryptCbcInternal(plaintext.ToArray(), KeyValue, iv.ToArray(), paddingMode);
            if (result.Length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            result.CopyTo(destination);
            bytesWritten = result.Length;
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    /// <summary>
    /// 尝试使用 CBC 模式解密数据到目标缓冲区
    /// </summary>
    protected override bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv,
        Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
    {
        if (KeyValue is null)
        {
            throw new CryptographicException("Key is not set.");
        }

        try
        {
            var result = DecryptCbcInternal(ciphertext.ToArray(), KeyValue, iv.ToArray(), paddingMode);
            if (result.Length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            result.CopyTo(destination);
            bytesWritten = result.Length;
            return true;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    #endregion

    #region 内部加密解密实现

    /// <summary>
    /// 使用 ECB 模式加密数据
    /// </summary>
    private static byte[] EncryptEcbInternal(byte[] plaintext, byte[] key, PaddingMode paddingMode)
    {
        var engine = new SM4Engine();
        IBlockCipherPadding? padding = paddingMode switch
        {
            PaddingMode.None => null,
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroBytePadding(),
            PaddingMode.ANSIX923 => new X923Padding(),
            PaddingMode.ISO10126 => new ISO10126d2Padding(),
            _ => throw new CryptographicException($"Padding mode not supported: {paddingMode}")
        };

        var cipher = padding != null
            ? new PaddedBufferedBlockCipher(engine, padding)
            : new BufferedBlockCipher(engine);

        cipher.Init(true, new KeyParameter(key));

        var output = new byte[cipher.GetOutputSize(plaintext.Length)];
        var length = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
        length += cipher.DoFinal(output, length);

        if (length < output.Length)
        {
            var result = new byte[length];
            Array.Copy(output, 0, result, 0, length);
            return result;
        }

        return output;
    }

    /// <summary>
    /// 使用 ECB 模式解密数据
    /// </summary>
    private static byte[] DecryptEcbInternal(byte[] ciphertext, byte[] key, PaddingMode paddingMode)
    {
        var engine = new SM4Engine();
        IBlockCipherPadding? padding = paddingMode switch
        {
            PaddingMode.None => null,
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroBytePadding(),
            PaddingMode.ANSIX923 => new X923Padding(),
            PaddingMode.ISO10126 => new ISO10126d2Padding(),
            _ => throw new CryptographicException($"Padding mode not supported: {paddingMode}")
        };

        var cipher = padding != null
            ? new PaddedBufferedBlockCipher(engine, padding)
            : new BufferedBlockCipher(engine);

        cipher.Init(false, new KeyParameter(key));

        var output = new byte[cipher.GetOutputSize(ciphertext.Length)];
        var length = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, output, 0);
        length += cipher.DoFinal(output, length);

        if (length < output.Length)
        {
            var result = new byte[length];
            Array.Copy(output, 0, result, 0, length);
            return result;
        }

        return output;
    }

    /// <summary>
    /// 使用 CBC 模式加密数据
    /// </summary>
    private static byte[] EncryptCbcInternal(byte[] plaintext, byte[] key, byte[] iv, PaddingMode paddingMode)
    {
        var engine = new SM4Engine();
        var cbcCipher = new CbcBlockCipher(engine);

        IBlockCipherPadding? padding = paddingMode switch
        {
            PaddingMode.None => null,
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroBytePadding(),
            PaddingMode.ANSIX923 => new X923Padding(),
            PaddingMode.ISO10126 => new ISO10126d2Padding(),
            _ => throw new CryptographicException($"Padding mode not supported: {paddingMode}")
        };

        var cipher = padding != null
            ? new PaddedBufferedBlockCipher(cbcCipher, padding)
            : new BufferedBlockCipher(cbcCipher);

        var parameters = new ParametersWithIV(new KeyParameter(key), iv);
        cipher.Init(true, parameters);

        var output = new byte[cipher.GetOutputSize(plaintext.Length)];
        var length = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
        length += cipher.DoFinal(output, length);

        if (length < output.Length)
        {
            var result = new byte[length];
            Array.Copy(output, 0, result, 0, length);
            return result;
        }

        return output;
    }

    /// <summary>
    /// 使用 CBC 模式解密数据
    /// </summary>
    private static byte[] DecryptCbcInternal(byte[] ciphertext, byte[] key, byte[] iv, PaddingMode paddingMode)
    {
        var engine = new SM4Engine();
        var cbcCipher = new CbcBlockCipher(engine);

        IBlockCipherPadding? padding = paddingMode switch
        {
            PaddingMode.None => null,
            PaddingMode.PKCS7 => new Pkcs7Padding(),
            PaddingMode.Zeros => new ZeroBytePadding(),
            PaddingMode.ANSIX923 => new X923Padding(),
            PaddingMode.ISO10126 => new ISO10126d2Padding(),
            _ => throw new CryptographicException($"Padding mode not supported: {paddingMode}")
        };

        var cipher = padding != null
            ? new PaddedBufferedBlockCipher(cbcCipher, padding)
            : new BufferedBlockCipher(cbcCipher);

        var parameters = new ParametersWithIV(new KeyParameter(key), iv);
        cipher.Init(false, parameters);

        var output = new byte[cipher.GetOutputSize(ciphertext.Length)];
        var length = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, output, 0);
        length += cipher.DoFinal(output, length);

        if (length < output.Length)
        {
            var result = new byte[length];
            Array.Copy(output, 0, result, 0, length);
            return result;
        }

        return output;
    }

    #endregion

    #region 静态工厂方法

    /// <summary>
    /// 创建用于执行对称算法的加密对象
    /// </summary>
    /// <returns>用于执行对称算法的加密对象</returns>
    public new static SM4 Create() => new();

    #endregion

    #region 资源管理

    /// <summary>
    /// 释放由 SM4 使用的非托管资源，并可以选择释放托管资源
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 清理敏感数据
            if (KeyValue != null)
            {
                Array.Clear(KeyValue, 0, KeyValue.Length);
            }
            if (IVValue != null)
            {
                Array.Clear(IVValue, 0, IVValue.Length);
            }
        }
        base.Dispose(disposing);
    }

    #endregion

    #region 内部加密转换器类

    /// <summary>
    /// SM4 加密转换器实现
    /// </summary>
    private sealed class Sm4CryptoTransform : ICryptoTransform
    {
        private readonly BufferedBlockCipher _cipher;
        private readonly int _blockSize;
        private bool _disposed;

        public Sm4CryptoTransform(BufferedBlockCipher cipher, int blockSize)
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
                throw new ObjectDisposedException(nameof(Sm4CryptoTransform));

            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));

            if (outputBuffer == null)
                throw new ArgumentNullException(nameof(outputBuffer));

            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(inputOffset));

            if (inputCount < 0)
                throw new ArgumentOutOfRangeException(nameof(inputCount));

            if (inputOffset + inputCount > inputBuffer.Length)
                throw new ArgumentException("Input buffer too small");

            if (outputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(outputOffset));

            var outputLength = _cipher.GetUpdateOutputSize(inputCount);
            if (outputOffset + outputLength > outputBuffer.Length)
                throw new ArgumentException("Output buffer too small");

            return _cipher.ProcessBytes(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Sm4CryptoTransform));

            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));

            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(inputOffset));

            if (inputCount < 0)
                throw new ArgumentOutOfRangeException(nameof(inputCount));

            if (inputOffset + inputCount > inputBuffer.Length)
                throw new ArgumentException("Input buffer too small");

            try
            {
                var output = new byte[_cipher.GetOutputSize(inputCount)];
                var length = _cipher.ProcessBytes(inputBuffer, inputOffset, inputCount, output, 0);
                length += _cipher.DoFinal(output, length);

                if (length < output.Length)
                {
                    var result = new byte[length];
                    Array.Copy(output, 0, result, 0, length);
                    return result;
                }

                return output;
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Error occurred during cryptographic operation.", ex);
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
