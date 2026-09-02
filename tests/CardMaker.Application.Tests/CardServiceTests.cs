using CardMaker.Application.Cards;
using CardMaker.Application.Content;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Infrastructure.Cards;
using CardMaker.Infrastructure.Content;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class CardServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CardMakerDbContext _db;
    private readonly CardService _sut;

    public CardServiceTests()
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
    public async Task GetGameCardTypesAsyncReturnsPopulatedGraph()
    {
        var cardTypes = await _sut.GetGameCardTypesAsync("yugioh");

        Assert.NotEmpty(cardTypes);
        var normalMonster = cardTypes.First(ct => ct.Key == "monster-normal");
        Assert.Equal("Normal Monster", normalMonster.Name);
        Assert.NotEmpty(normalMonster.Fields);
        Assert.NotEmpty(normalMonster.Templates);
    }

    [Fact]
    public async Task UserCardLifecycleCreateReadUpdateDuplicateDelete()
    {
        const string userId = "user-123";
        var cardTypes = await _sut.GetGameCardTypesAsync("yugioh");
        var ct = cardTypes.First(c => c.Key == "monster-effect");
        var game = await _db.Games.FirstAsync(g => g.Key == "yugioh");
        var template = ct.Templates[0];
        var templateVersion = template.Versions.First();

        var values = new Dictionary<string, CardValue>
        {
            ["name"] = CardValue.FromText("Mago Nero"),
            ["atk"] = CardValue.FromText("2500"),
            ["def"] = CardValue.FromText("2100"),
        };

        // 1. Create
        var created = await _sut.CreateCardAsync(new SaveCardRequest
        {
            Title = "Il mio Mago Nero",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = templateVersion.Id,
            Values = values,
            SelectedTraits = ["effect"],
        }, userId);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Il mio Mago Nero", created.Title);
        Assert.Equal("Mago Nero", created.Values["name"].AsText());

        // 2. Read list
        var userCards = await _sut.GetUserCardsAsync(userId);
        Assert.Single(userCards);
        Assert.Equal("Il mio Mago Nero", userCards[0].Title);

        // Verify other user cannot see
        var otherCards = await _sut.GetUserCardsAsync("other-user");
        Assert.Empty(otherCards);

        // 3. Read single
        var fetched = await _sut.GetCardAsync(created.Id, userId);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);

        // 4. Update
        var updatedValues = new Dictionary<string, CardValue>(values)
        {
            ["name"] = CardValue.FromText("Mago Nero Supremo")
        };
        var updated = await _sut.UpdateCardAsync(created.Id, new SaveCardRequest
        {
            Title = "Mago Nero Aggiornato",
            GameId = game.Id,
            CardTypeId = ct.Id,
            TemplateVersionId = templateVersion.Id,
            Values = updatedValues,
            SelectedTraits = ["effect"],
        }, userId);

        Assert.Equal("Mago Nero Aggiornato", updated.Title);
        Assert.Equal("Mago Nero Supremo", updated.Values["name"].AsText());

        // 5. Duplicate
        var duplicated = await _sut.DuplicateCardAsync(created.Id, userId);
        Assert.NotEqual(created.Id, duplicated.Id);
        Assert.Equal("Mago Nero Aggiornato (Copia)", duplicated.Title);
        Assert.Equal("Mago Nero Supremo", duplicated.Values["name"].AsText());

        var cardsAfterDup = await _sut.GetUserCardsAsync(userId);
        Assert.Equal(2, cardsAfterDup.Count);

        // 6. Delete
        var deleted = await _sut.DeleteCardAsync(created.Id, userId);
        Assert.True(deleted);

        var cardsAfterDel = await _sut.GetUserCardsAsync(userId);
        Assert.Single(cardsAfterDel);
        Assert.Equal(duplicated.Id, cardsAfterDel[0].Id);
    }
}

