using CardMaker.Application.Cards;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Infrastructure.Cards;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardMaker.Application.Tests.Cards;

public sealed class CardServiceHardeningTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly CardService _sut;

    public CardServiceHardeningTests()
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

        _sut = new CardService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task TEST_DB_003_GetUserCardsAsyncProjectionReturnsValidSummariesWithUserIsolation()
    {
        const string user1 = "user-alpha";
        const string user2 = "user-beta";

        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");
        var ct = await _db.CardTypes.Include(c => c.Templates).ThenInclude(t => t.Versions).FirstAsync(c => c.Key == "monster-normal");
        var tv = ct.Templates.First().Versions.First();

        // Create 3 cards for user1
        for (var i = 1; i <= 3; i++)
        {
            await _sut.CreateCardAsync(new SaveCardRequest
            {
                Title = $"Card {i}",
                GameId = game.Id,
                CardTypeId = ct.Id,
                TemplateVersionId = tv.Id,
                Values = new Dictionary<string, CardValue> { ["name"] = CardValue.FromText($"Name {i}") },
                SelectedTraits = [],
            }, user1);
        }

        // Create 1 card for user2
        await _sut.CreateCardAsync(new SaveCardRequest
        {
            Title = "User 2 Card",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = tv.Id,
            Values = new Dictionary<string, CardValue>(),
            SelectedTraits = [],
        }, user2);

        // Act - DB-PERF-004 & LINQ-PERF-001: scalar projection query
        var user1Cards = await _sut.GetUserCardsAsync(user1);
        var user2Cards = await _sut.GetUserCardsAsync(user2);

        // Assert
        Assert.Equal(3, user1Cards.Count);
        Assert.Single(user2Cards);

        Assert.All(user1Cards, c =>
        {
            Assert.NotEqual(Guid.Empty, c.Id);
            Assert.StartsWith("Card ", c.Title);
            Assert.Equal(game.Key, c.GameKey);
            Assert.NotEmpty(c.CardTypeName);
            Assert.True(c.UpdatedAtUtc > DateTimeOffset.MinValue);
        });
    }

    [Fact]
    public async Task TEST_DB_004_GetUserCardsAsyncWorksWithLargeValuesJson()
    {
        const string user = "user-large-lob";
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");
        var ct = await _db.CardTypes.Include(c => c.Templates).ThenInclude(t => t.Versions).FirstAsync(c => c.Key == "monster-normal");
        var tv = ct.Templates.First().Versions.First();

        // Generate 100 fields in values
        var largeValues = new Dictionary<string, CardValue>();
        for (var i = 0; i < 100; i++)
        {
            largeValues[$"field_{i}"] = CardValue.FromText(new string('X', 500));
        }

        await _sut.CreateCardAsync(new SaveCardRequest
        {
            Title = "Heavy Card",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = tv.Id,
            Values = largeValues,
            SelectedTraits = [],
        }, user);

        // Act - scalar projection does not need to parse large values
        var summaries = await _sut.GetUserCardsAsync(user);

        // Assert
        Assert.Single(summaries);
        Assert.Equal("Heavy Card", summaries[0].Title);
    }
}
