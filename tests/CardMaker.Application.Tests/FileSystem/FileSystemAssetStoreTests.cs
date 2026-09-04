using CardMaker.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using Xunit;

namespace CardMaker.Application.Tests.FileSystem;

public sealed class FileSystemAssetStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemAssetStore _sut;

    public FileSystemAssetStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CardMaker_Test_Assets_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        var options = Options.Create(new AssetStoreOptions { RootPath = _tempDir });
        _sut = new FileSystemAssetStore(options, NullLogger<FileSystemAssetStore>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task TEST_FS_001_MemoryStreamSavesAndCalculatesSha256FastPath()
    {
        // Arrange
        var content = "CardMaker Test Asset Content"u8.ToArray();
        using var stream = new MemoryStream(content);
        var expectedHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        // Act - FS-PERF-001: in-memory SHA256 fast path
        var blob = await _sut.SaveAsync(stream);

        // Assert
        Assert.Equal(expectedHash, blob.Sha256);
        Assert.Equal(content.Length, blob.ByteSize);

        // Saving again the same content must recognize the duplicate without error
        using var stream2 = new MemoryStream(content);
        var duplicateBlob = await _sut.SaveAsync(stream2);
        Assert.Equal(expectedHash, duplicateBlob.Sha256);
    }

    [Fact]
    public async Task TEST_FS_002_OpenReadAsyncReadsExistingFileWithOptimizedBuffer()
    {
        // Arrange
        var content = "Optimized 4KB buffer content for FileStream"u8.ToArray();
        using var stream = new MemoryStream(content);
        var blob = await _sut.SaveAsync(stream);

        // Act - FS-PERF-002 & MEM-PERF-001: direct FileStream open with 4096 buffer
        using var readStream = await _sut.OpenReadAsync(blob.Sha256);
        Assert.NotNull(readStream);

        using var ms = new MemoryStream();
        await readStream.CopyToAsync(ms);

        // Assert
        Assert.Equal(content, ms.ToArray());
    }

    [Fact]
    public async Task TEST_FS_003_OpenReadAsyncReturnsNullForMissingAsset()
    {
        // Arrange - random non-existent SHA256 hash
        var missingHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act & Assert - FS-PERF-002: direct open returns null without File.Exists check
        var stream = await _sut.OpenReadAsync(missingHash);
        Assert.Null(stream);
    }

    [Fact]
    public async Task TEST_FS_004_RoundtripPreservesIntegrityAcrossSubdirectories()
    {
        // Arrange: binary data simulating an image
        var binary = new byte[8192];
        Random.Shared.NextBytes(binary);
        using var writeStream = new MemoryStream(binary);

        // Act
        var blob = await _sut.SaveAsync(writeStream);

        // Verify file was placed in 2-char prefix subfolder
        var prefix = blob.Sha256[..2];
        var expectedFolder = Path.Combine(_tempDir, prefix);
        Assert.True(Directory.Exists(expectedFolder), "La sottodirectory con prefisso a 2 caratteri deve esistere.");

        // Read back
        using var readStream = await _sut.OpenReadAsync(blob.Sha256);
        Assert.NotNull(readStream);

        using var mem = new MemoryStream();
        await readStream.CopyToAsync(mem);

        // Assert
        Assert.Equal(binary, mem.ToArray());
    }

    [Fact]
    public async Task TEST_FS_005_SaveAsyncThrowsOnNullStream()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SaveAsync(null!));
    }
}
