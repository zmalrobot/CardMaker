using CardMaker.Application.Admin;
using CardMaker.Contracts.Geometry;
using CardMaker.Contracts.Layout;
using CardMaker.Infrastructure.Admin;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class TemplateAdminServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly TemplateAdminService _sut;

    public TemplateAdminServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new CardMakerDbContext(options);
        _db.Database.EnsureCreated();

        var seeder = new YuGiOhContentSeeder(_db);
        seeder.SeedAsync().GetAwaiter().GetResult();

        _sut = new TemplateAdminService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task TemplateLifecycleCreateVersionPublish()
    {
        const string userId = "admin-1";
        var ct = await _db.CardTypes.FirstAsync(c => c.Key == "monster-normal");

        // 1. Create Template
        var template = await _sut.SaveTemplateAsync(new SaveTemplateRequest
        {
            CardTypeId = ct.Id,
            Key = "normal-custom-v1",
            Name = "Template Normal Custom",
            IsDefault = false,
            SortOrder = 10,
        }, userId);

        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.Single(template.Versions);
        Assert.Equal(1, template.Versions[0].VersionNumber);
        Assert.True(template.Versions[0].IsPublished);

        // 2. Create Version 2 (draft)
        var layoutV2 = new CardLayout
        {
            Layers =
            [
                new TextLayer
                {
                    Name = "CardName",
                    Rect = new NormalizedRect(0.08, 0.05, 0.75, 0.06),
                    Source = "{{name}}",
                }
            ]
        };

        var v2 = await _sut.CreateVersionAsync(template.Id, LayoutSerializer.Serialize(layoutV2), "Aggiunto testo nome", userId);
        Assert.Equal(2, v2.VersionNumber);
        Assert.False(v2.IsPublished);

        // 3. Publish Version 2
        var pubV2 = await _sut.PublishVersionAsync(v2.Id, userId);
        Assert.True(pubV2.IsPublished);

        var details = await _sut.GetTemplateDetailAsync(template.Id);
        Assert.NotNull(details);
        Assert.Equal(2, details.Versions.Count);
        Assert.Equal(2, details.CurrentVersion?.VersionNumber);

        // 4. Delete Template
        var deleted = await _sut.DeleteTemplateAsync(template.Id, userId);
        Assert.True(deleted);

        var detailsAfter = await _sut.GetTemplateDetailAsync(template.Id);
        Assert.Null(detailsAfter);
    }

    [Fact]
    public async Task LayoutValidationDetectsUnmappedFieldsAndZeroDimensions()
    {
        var ct = await _db.CardTypes.Include(c => c.Fields).FirstAsync(c => c.Key == "monster-normal");

        var invalidLayout = new CardLayout
        {
            Layers =
            [
                new TextLayer
                {
                    Name = "ValidName",
                    Rect = new NormalizedRect(0.08, 0.05, 0.75, 0.06),
                    Source = "{{name}}", // Valid field
                },
                new TextLayer
                {
                    Name = "InvalidFieldText",
                    Rect = new NormalizedRect(0.08, 0.20, 0.75, 0.06),
                    Source = "{{nonExistentField_123}}", // Unmapped field
                },
                new StaticImageLayer
                {
                    Name = "ZeroSizeFrame",
                    Rect = new NormalizedRect(0, 0, 0, 0), // Zero size error
                }
            ]
        };

        var report = await _sut.ValidateLayoutAsync(ct.Id, invalidLayout);

        Assert.False(report.IsValid); // Has error because of zero size
        Assert.Contains(report.Issues, i => i.Code == "text.binding_unmapped");
        Assert.Contains(report.Issues, i => i.Code == "layer.zero_size");
    }
}
