using CardMaker.Application.Assets;

namespace CardMaker.Application.Tests;

public class UploadValidatorTests
{
    private static readonly UploadLimits Limits = new();

    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    [Fact]
    public void RiconosceUnPngDaiMagicBytes()
    {
        var result = UploadValidator.Validate(PngHeader, UploadKind.Image, Limits);

        Assert.True(result.IsValid);
        Assert.Equal("image/png", result.DetectedContentType);
    }

    [Fact]
    public void RifiutaUnSvg()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

        var result = UploadValidator.Validate(svg, UploadKind.Image, Limits);

        Assert.False(result.IsValid);
        Assert.Equal("upload.svgNotAllowed", result.ErrorCode);
    }

    [Fact]
    public void RifiutaUnFileConEstensioneIngannevole()
    {
        // Contenuto HTML rinominato .png: l'estensione non conta, contano i magic bytes.
        var html = "<html><body>payload</body></html>"u8.ToArray();

        var result = UploadValidator.Validate(html, UploadKind.Image, Limits);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RifiutaUnFileVuoto()
    {
        var result = UploadValidator.Validate([], UploadKind.Image, Limits);

        Assert.False(result.IsValid);
        Assert.Equal("upload.empty", result.ErrorCode);
    }

    [Fact]
    public void RifiutaUnImmagineOltreIlLimiteDiPeso()
    {
        var limits = Limits with { MaxImageBytes = 4 };

        var result = UploadValidator.Validate(PngHeader, UploadKind.Image, limits);

        Assert.False(result.IsValid);
        Assert.Equal("upload.tooLarge", result.ErrorCode);
    }

    [Fact]
    public void RiconosceUnFontOpenType()
    {
        var otf = "OTTO\0\0\0\0"u8.ToArray();

        var result = UploadValidator.Validate(otf, UploadKind.Font, Limits);

        Assert.True(result.IsValid);
        Assert.Equal("font/otf", result.DetectedContentType);
    }

    [Theory]
    [InlineData(1394, 2032, false)]
    [InlineData(20000, 100, true)]
    [InlineData(9000, 9000, true)]
    [InlineData(0, 100, true)]
    public void ApplicaIlBudgetDiPixel(int width, int height, bool shouldExceed)
    {
        Assert.Equal(shouldExceed, UploadValidator.ExceedsPixelBudget(width, height, Limits));
    }
}
