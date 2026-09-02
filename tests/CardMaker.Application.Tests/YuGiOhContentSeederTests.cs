using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Application.Tests;

public class YuGiOhContentSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CardMakerDbContext> _options;

    public YuGiOhContentSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new CardMakerDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SeedInizialeCreaTuttiIContenuti()
    {
        await using var db = new CardMakerDbContext(_options);
        var seeder = new YuGiOhContentSeeder(db);

        var result = await seeder.SeedAsync();

        Assert.True(result.Created);
        Assert.Equal("yugioh", result.GameKey);
        Assert.Equal(26, result.CardTypeCount);
        Assert.Equal(28, result.TemplateCount);
        Assert.Equal(5, result.SymbolSetCount);
        Assert.Equal(4, result.OptionListCount);
        Assert.Equal(6, result.TraitCount);

        // Verifica entità persistite
        Assert.Equal(1, await db.Games.CountAsync());
        Assert.Equal(26, await db.CardTypes.CountAsync());
        Assert.Equal(28, await db.Templates.CountAsync());
        Assert.Equal(28, await db.TemplateVersions.CountAsync());
        Assert.Equal(5, await db.SymbolSets.CountAsync());
        Assert.Equal(4, await db.OptionLists.CountAsync());
        Assert.Equal(6, await db.Traits.CountAsync());
    }

    [Fact]
    public async Task EsecuzioneMultiplaEIdempotente()
    {
        await using var db = new CardMakerDbContext(_options);
        var seeder = new YuGiOhContentSeeder(db);

        var firstResult = await seeder.SeedAsync();
        Assert.True(firstResult.Created);

        var secondResult = await seeder.SeedAsync();
        Assert.False(secondResult.Created);
        Assert.Equal("yugioh", secondResult.GameKey);
        Assert.Equal(26, secondResult.CardTypeCount);

        Assert.Equal(1, await db.Games.CountAsync());
        Assert.Equal(26, await db.CardTypes.CountAsync());
    }
}

