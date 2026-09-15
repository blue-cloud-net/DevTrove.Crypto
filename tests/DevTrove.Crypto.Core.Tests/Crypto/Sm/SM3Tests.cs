using DevTrove.Crypto.Crypto.Sm;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;

namespace DevTrove.Crypto.Core.Tests.Crypto.Sm;

/// <summary>
/// SM3 类的单元测试
/// </summary>
public class SM3Tests
{
    #region 1. 构造函数和基本属性测试

    [Fact]
    public void Constructor_ShouldInitializeWithCorrectHashSize()
    {
        // Arrange & Act
        using var sm3 = new SM3();

        // Assert
        sm3.HashSize.Should().Be(256, "SM3 produces 256-bit hash");
    }

    [Fact]
    public void Constructor_ShouldSetCorrectBlockSizes()
    {
        // Arrange & Act
        using var sm3 = new SM3();

        // Assert
        sm3.InputBlockSize.Should().Be(64, "SM3 input block size is 64 bytes");
        sm3.OutputBlockSize.Should().Be(32, "SM3 output block size is 32 bytes");
    }

    #endregion

    #region 2. 静态工厂方法测试

    [Fact]
    public void Create_ShouldReturnNewInstance()
    {
        // Act
        using var sm3 = SM3.Create();

        // Assert
        sm3.Should().NotBeNull();
        sm3.Should().BeOfType<SM3>();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldReturnDifferentInstances()
    {
        // Act
        using var sm3_1 = SM3.Create();
        using var sm3_2 = SM3.Create();

        // Assert
        sm3_1.Should().NotBeSameAs(sm3_2);
    }

    #endregion

    #region 3. 基本哈希计算测试

    [Fact]
    public void ComputeHash_WithEmptyData_ShouldReturnValidHash()
    {
        // Arrange
        using var sm3 = new SM3();
        var emptyData = Array.Empty<byte>();

        // Act
        var hash = sm3.ComputeHash(emptyData);

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32, "SM3 always produces 32-byte hash");

        // SM3("") 的标准测试向量
        var expectedHash = Convert.FromHexString("1AB21D8355CFA17F8E61194831E81A8F22BEC8C728FEFB747ED035EB5082AA2B");
        hash.Should().Equal(expectedHash);
    }

    [Fact]
    public void ComputeHash_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var sm3 = new SM3();

        // Act & Assert
        var action = () => sm3.ComputeHash((byte[])null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeHash_WithValidData_ShouldReturnExpectedHash()
    {
        // Arrange
        using var sm3 = new SM3();
        var data = Encoding.UTF8.GetBytes("abc");

        // Act
        var hash = sm3.ComputeHash(data);

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32);

        // SM3("abc") 的标准测试向量
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        hash.Should().Equal(expectedHash);
    }

    [Fact]
    public void ComputeHash_SameData_ShouldProduceSameHash()
    {
        // Arrange
        using var sm3 = new SM3();
        var data = Encoding.UTF8.GetBytes("Hello SM3!");

        // Act
        var hash1 = sm3.ComputeHash(data);
        var hash2 = sm3.ComputeHash(data);

        // Assert
        hash1.Should().Equal(hash2, "Same input should produce same hash");
    }

    [Fact]
    public void ComputeHash_DifferentData_ShouldProduceDifferentHash()
    {
        // Arrange
        using var sm3 = new SM3();
        var data1 = Encoding.UTF8.GetBytes("Hello SM3!");
        var data2 = Encoding.UTF8.GetBytes("Hello SM4!");

        // Act
        var hash1 = sm3.ComputeHash(data1);
        var hash2 = sm3.ComputeHash(data2);

        // Assert
        hash1.Should().NotEqual(hash2, "Different input should produce different hash");
    }

    #endregion

    #region 4. 静态方法 HashData 测试

    [Fact]
    public void HashData_ByteArray_WithNullData_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => SM3.HashData((byte[])null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HashData_ByteArray_WithValidData_ShouldReturnExpectedHash()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");

        // Act
        var hash = SM3.HashData(data);

        // Assert
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        hash.Should().Equal(expectedHash);
    }

    [Fact]
    public void HashData_Span_WithEmptyData_ShouldReturnValidHash()
    {
        // Arrange
        var emptyData = ReadOnlySpan<byte>.Empty;

        // Act
        var hash = SM3.HashData(emptyData);

        // Assert
        hash.Length.Should().Be(32);
        var expectedHash = Convert.FromHexString("1AB21D8355CFA17F8E61194831E81A8F22BEC8C728FEFB747ED035EB5082AA2B");
        hash.Should().Equal(expectedHash);
    }

    [Fact]
    public void HashData_SpanToSpan_WithValidData_ShouldWriteToDestination()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        var destination = new byte[32];

        // Act
        var bytesWritten = SM3.HashData(data, destination);

        // Assert
        bytesWritten.Should().Be(32);
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        destination.Should().Equal(expectedHash);
    }

    [Fact]
    public void HashData_SpanToSpan_WithShortDestination_ShouldThrowArgumentException()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        var destination = new byte[16]; // 太短

        // Act & Assert
        var action = () => SM3.HashData(data, destination);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void TryHashData_WithValidDestination_ShouldReturnTrue()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        var destination = new byte[32];

        // Act
        var result = SM3.TryHashData(data, destination, out var bytesWritten);

        // Assert
        result.Should().BeTrue();
        bytesWritten.Should().Be(32);
    }

    [Fact]
    public void TryHashData_WithShortDestination_ShouldReturnFalse()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        var destination = new byte[16];

        // Act
        var result = SM3.TryHashData(data, destination, out var bytesWritten);

        // Assert
        result.Should().BeFalse();
        bytesWritten.Should().Be(0);
    }

    #endregion

    #region 5. 流哈希计算测试

    [Fact]
    public void HashData_Stream_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => SM3.HashData((Stream)null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HashData_Stream_WithValidStream_ShouldReturnExpectedHash()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        using var stream = new MemoryStream(data);

        // Act
        var hash = SM3.HashData(stream);

        // Assert
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        hash.Should().Equal(expectedHash);
    }

    [Fact]
    public void HashData_Stream_WithNonReadableStream_ShouldThrowArgumentException()
    {
        // Arrange
        var stream = new NonReadableStream();

        // Act & Assert
        var action = () => SM3.HashData(stream);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*not support reading*");
    }

    [Fact]
    public void HashData_StreamToSpan_WithValidStream_ShouldWriteToDestination()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        using var stream = new MemoryStream(data);
        var destination = new byte[32];

        // Act
        var bytesWritten = SM3.HashData(stream, destination);

        // Assert
        bytesWritten.Should().Be(32);
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        destination.Should().Equal(expectedHash);
    }

    [Fact]
    public void HashData_LargeStream_ShouldHandleCorrectly()
    {
        // Arrange
        var largeData = new byte[10000];
        Random.Shared.NextBytes(largeData);
        using var stream = new MemoryStream(largeData);

        // Act
        var hash = SM3.HashData(stream);

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32);
    }

    #endregion

    #region 6. 异步流哈希计算测试

    [Fact]
    public async Task HashDataAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await SM3.HashDataAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HashDataAsync_WithValidStream_ShouldReturnExpectedHash()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        using var stream = new MemoryStream(data);

        // Act
        var hash = await SM3.HashDataAsync(stream);

        // Assert
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        hash.Should().Equal(expectedHash);
    }

    [Fact]
    public async Task HashDataAsync_WithNonReadableStream_ShouldThrowArgumentException()
    {
        // Arrange
        var stream = new NonReadableStream();

        // Act & Assert
        var action = async () => await SM3.HashDataAsync(stream);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not support reading*");
    }

    [Fact]
    public async Task HashDataAsync_ToMemory_WithValidStream_ShouldWriteToDestination()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("abc");
        using var stream = new MemoryStream(data);
        var destination = new byte[32];

        // Act
        var bytesWritten = await SM3.HashDataAsync(stream, destination);

        // Assert
        bytesWritten.Should().Be(32);
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        destination.Should().Equal(expectedHash);
    }

    [Fact]
    public async Task HashDataAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var largeData = new byte[100000];
        using var stream = new MemoryStream(largeData);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await SM3.HashDataAsync(stream, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region 7. 增量哈希计算测试

    [Fact]
    public void Initialize_ShouldResetHashState()
    {
        // Arrange
        using var sm3 = new SM3();
        var data1 = Encoding.UTF8.GetBytes("first");
        var data2 = Encoding.UTF8.GetBytes("abc");

        // Act
        sm3.ComputeHash(data1);
        sm3.Initialize();
        var hash = sm3.ComputeHash(data2);

        // Assert
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        hash.Should().Equal(expectedHash);
    }

    [Fact]
    public void TransformBlock_ShouldProcessMultipleBlocks()
    {
        // Arrange
        using var sm3 = new SM3();
        var part1 = Encoding.UTF8.GetBytes("ab");
        var part2 = Encoding.UTF8.GetBytes("c");
        var outputBuffer = new byte[sm3.OutputBlockSize];

        // Act
        sm3.TransformBlock(part1, 0, part1.Length, outputBuffer, 0);
        sm3.TransformBlock(part2, 0, part2.Length, outputBuffer, 0);
        sm3.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hash = sm3.Hash;

        // Assert
        var expectedHash = Convert.FromHexString("66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0");
        hash.Should().Equal(expectedHash);
    }

    #endregion

    #region 8. 标准测试向量验证

    [Theory]
    [InlineData("", "1AB21D8355CFA17F8E61194831E81A8F22BEC8C728FEFB747ED035EB5082AA2B")]
    [InlineData("abc", "66C7F0F462EEEDD9D1F2D46BDC10E4E24167C4875CF2F7A2297DA02B8F4BA8E0")]
    [InlineData("abcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcd",
              "DEBE9FF92275B8A138604889C18E5A4D6FDB70E5387E5765293DCBA39C0C5732")]
    public void HashData_StandardTestVectors_ShouldMatchExpected(string input, string expectedHex)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes(input);
        var expectedHash = Convert.FromHexString(expectedHex);

        // Act
        var hash = SM3.HashData(data);

        // Assert
        hash.Should().Equal(expectedHash);
    }

    #endregion

    #region 9. 边界情况测试

    [Fact]
    public void HashData_VeryLargeData_ShouldHandleCorrectly()
    {
        // Arrange
        var largeData = new byte[1_000_000]; // 1MB
        Random.Shared.NextBytes(largeData);

        // Act
        var hash = SM3.HashData(largeData);

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32);
    }

    [Fact]
    public void HashData_SingleByte_ShouldProduceValidHash()
    {
        // Arrange
        var data = new byte[] { 0x61 }; // 'a'

        // Act
        var hash = SM3.HashData(data);

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32);
    }

    [Fact]
    public void HashData_AllZeros_ShouldProduceValidHash()
    {
        // Arrange
        var data = new byte[100];

        // Act
        var hash = SM3.HashData(data);

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32);
    }

    [Fact]
    public void HashData_AllOnes_ShouldProduceValidHash()
    {
        // Arrange
        var data = Enumerable.Repeat((byte)0xFF, 100).ToArray();

        // Act
        var hash = SM3.HashData(data);

        // Assert
        hash.Should().NotBeNull();
        hash.Length.Should().Be(32);
    }

    #endregion

    #region 10. Dispose 和资源管理测试

    [Fact]
    public void Dispose_ShouldClearResources()
    {
        // Arrange
        var sm3 = new SM3();
        var data = Encoding.UTF8.GetBytes("test");
        sm3.ComputeHash(data);

        // Act
        sm3.Dispose();

        // Assert - 再次调用应该抛出异常
        var action = () => sm3.ComputeHash(data);
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var sm3 = new SM3();

        // Act & Assert
        var action = () =>
        {
            sm3.Dispose();
            sm3.Dispose();
            sm3.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void UsingPattern_ShouldAutomaticallyDispose()
    {
        byte[]? hash = null;

        using (var sm3 = new SM3())
        {
            var data = Encoding.UTF8.GetBytes("test");
            hash = sm3.ComputeHash(data);
        }

        hash.Should().NotBeNull();
        hash!.Length.Should().Be(32);
    }

    #endregion

    #region 11. 并发测试

    [Fact]
    public async Task ConcurrentHashing_WithDifferentInstances_ShouldBeSafe()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("concurrent test");
        var tasks = new List<Task<byte[]>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                using var sm3 = new SM3();
                return sm3.ComputeHash(data);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(hash =>
        {
            hash.Should().NotBeNull();
            hash.Length.Should().Be(32);
        });

        // 所有结果应该相同
        for (int i = 1; i < results.Length; i++)
        {
            results[i].Should().Equal(results[0]);
        }
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// 不可读流，用于测试错误处理
    /// </summary>
    private class NonReadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { }
    }

    #endregion
}
