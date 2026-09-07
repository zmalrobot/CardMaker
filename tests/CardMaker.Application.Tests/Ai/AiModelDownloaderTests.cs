using System.Net;
using CardMaker.AI.Models;
using CardMaker.Contracts.Ai;
using CardMaker.Infrastructure.Ai;

namespace CardMaker.Application.Tests.Ai;

public sealed class AiModelDownloaderTests : IDisposable
{
    private readonly string _testDir;

    public AiModelDownloaderTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "cardmaker-dl-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public void ValidateModelFile_FailsWhenFileDoesNotExist()
    {
        var downloader = new AiModelDownloader();
        var model = AiModelRegistry.Gemma2B;
        var nonExistentPath = Path.Combine(_testDir, "missing.gguf");

        var isValid = downloader.ValidateModelFile(model, nonExistentPath, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("non presente", error);
    }

    [Fact]
    public void ValidateModelFile_FailsWhenMagicHeaderIsInvalid()
    {
        var downloader = new AiModelDownloader();
        var model = new AiModelDefinition(
            Key: "test-model",
            DisplayName: "Test Model",
            FileName: "test.gguf",
            RecommendedRamGb: 4,
            EstimatedMemoryUsageGb: 0.1,
            Quantization: "Q4",
            Family: "Gemma",
            DownloadUrl: "http://example.com/test.gguf",
            DownloadUrlDirect: "http://example.com/test.gguf",
            ExpectedSizeBytes: 60L * 1024L * 1024L,
            Description: "Test");
        var fakePath = Path.Combine(_testDir, "corrupted.gguf");

        // Scrive 60 MB di byte spazzatura senza magic header GGUF
        using (var fs = new FileStream(fakePath, FileMode.Create, FileAccess.Write))
        {
            fs.SetLength(60L * 1024L * 1024L);
        }

        var isValid = downloader.ValidateModelFile(model, fakePath, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("GGUF", error);
    }

    [Fact]
    public void ValidateModelFile_SucceedsWhenMagicHeaderAndSizeAreValid()
    {
        var downloader = new AiModelDownloader();
        // Modello con expected size pari alla lunghezza del test
        var model = new AiModelDefinition(
            Key: "test-model",
            DisplayName: "Test Model",
            FileName: "test.gguf",
            RecommendedRamGb: 4,
            EstimatedMemoryUsageGb: 0.1,
            Quantization: "Q4",
            Family: "Gemma",
            DownloadUrl: "http://example.com/test.gguf",
            DownloadUrlDirect: "http://example.com/test.gguf",
            ExpectedSizeBytes: 60L * 1024L * 1024L,
            Description: "Test");

        var validPath = Path.Combine(_testDir, "valid.gguf");

        using (var fs = new FileStream(validPath, FileMode.Create, FileAccess.Write))
        {
            // Magic header GGUF: 'G', 'G', 'U', 'F'
            fs.Write([(byte)'G', (byte)'G', (byte)'U', (byte)'F']);
            fs.SetLength(60L * 1024L * 1024L);
        }

        var isValid = downloader.ValidateModelFile(model, validPath, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public async Task DownloadModelAsync_DownloadsAndReportsProgressSuccessfully()
    {
        var payloadSize = 60L * 1024L * 1024L;
        var model = new AiModelDefinition(
            Key: "test-model",
            DisplayName: "Test Model",
            FileName: "test.gguf",
            RecommendedRamGb: 4,
            EstimatedMemoryUsageGb: 0.1,
            Quantization: "Q4",
            Family: "Gemma",
            DownloadUrl: "http://example.com/test.gguf",
            DownloadUrlDirect: "http://example.com/test.gguf",
            ExpectedSizeBytes: payloadSize,
            Description: "Test");

        var targetPath = Path.Combine(_testDir, "test.gguf");

        var mockHandler = new MockHttpHandler((req) =>
        {
            var content = new byte[payloadSize];
            // Header GGUF
            content[0] = (byte)'G';
            content[1] = (byte)'G';
            content[2] = (byte)'U';
            content[3] = (byte)'F';

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            };
            resp.Content.Headers.ContentLength = payloadSize;
            return resp;
        });

        using var httpClient = new HttpClient(mockHandler);
        var downloader = new AiModelDownloader(httpClient);

        var progressUpdates = new List<AiModelDownloadProgress>();
        var progress = new Progress<AiModelDownloadProgress>(p => progressUpdates.Add(p));

        await downloader.DownloadModelAsync(model, targetPath, progress);

        Assert.True(File.Exists(targetPath), "Il file target finale deve esistere.");
        Assert.False(File.Exists(targetPath + ".download"), "Il file temporaneo .download deve essere stato rimosso o rinominato.");
        Assert.Equal(payloadSize, new FileInfo(targetPath).Length);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); } catch { }
        }
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
