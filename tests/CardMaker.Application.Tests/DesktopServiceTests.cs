using System.Security.Claims;
using CardMaker.Desktop.Services;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class DesktopServiceTests
{
    [Fact]
    public async Task DesktopAuthenticationStateProviderReturnsAuthenticatedAdmin()
    {
        var provider = new DesktopAuthenticationStateProvider();
        var state = await provider.GetAuthenticationStateAsync();

        Assert.NotNull(state);
        Assert.NotNull(state.User);
        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("PhotinoLocalBypass", state.User.Identity?.AuthenticationType);
        Assert.True(state.User.IsInRole("Admin"));
        Assert.Equal("desktop-local-admin", state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void DesktopPathResolverGeneratesExpectedStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CardMaker_DesktopTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var dbPath = DesktopPathResolver.GetDatabasePath(tempDir);
            var assetsDir = DesktopPathResolver.GetAssetsDirectory(tempDir);
            var fontsDir = DesktopPathResolver.GetFontsDirectory(tempDir);

            Assert.Equal(Path.Combine(tempDir, "CardMaker.db"), dbPath);
            Assert.Equal(Path.Combine(tempDir, "assets"), assetsDir);
            Assert.Equal(Path.Combine(tempDir, "fonts"), fontsDir);

            DesktopPathResolver.EnsureDirectoriesCreated(tempDir);

            Assert.True(Directory.Exists(tempDir));
            Assert.True(Directory.Exists(assetsDir));
            Assert.True(Directory.Exists(fontsDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}

