using Microsoft.AspNetCore.Components;
using System.Reflection;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class LocalizationAndLegalTests
{
    [Fact]
    public void DisclaimerComponentHasExpectedRoutes()
    {
        var disclaimerType = typeof(CardMaker.UI.Pages.Legal.Disclaimer);
        Assert.NotNull(disclaimerType);

        var routeAttributes = disclaimerType.GetCustomAttributes<RouteAttribute>().Select(r => r.Template).ToList();
        Assert.Contains("/disclaimer", routeAttributes);
        Assert.Contains("/terms", routeAttributes);
    }

    [Fact]
    public void DisclaimerSourceContainsRequiredLegalDisclaimers()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? disclaimerPath = null;

        while (dir != null && dir.Exists)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CardMaker.UI", "Pages", "Legal", "Disclaimer.razor");
            if (File.Exists(candidate))
            {
                disclaimerPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(disclaimerPath);
        Assert.True(File.Exists(disclaimerPath));

        var content = File.ReadAllText(disclaimerPath);

        // Verifies mandatory intellectual property disclaimers
        Assert.Contains("Konami", content);
        Assert.Contains("Nintendo", content);
        Assert.Contains("Wizards of the Coast", content);
        Assert.Contains("Fan-Made", content);
        Assert.Contains("Non Commerciale", content);
    }
}

