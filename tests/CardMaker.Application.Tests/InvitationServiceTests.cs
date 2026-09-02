using CardMaker.Application.Identity;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure.Identity;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class InvitationServiceTests
{
    private static (CardMakerDbContext Db, string TempDir) CreateTestContext()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CardMaker_InvTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "cardmaker.db");

        var options = new DbContextOptionsBuilder<CardMakerDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var db = new CardMakerDbContext(options);
        db.Database.EnsureCreated();
        return (db, tempDir);
    }

    [Fact]
    public async Task CreateInvitationGeneratesValidTokenAndStoresHash()
    {
        var (db, tempDir) = CreateTestContext();
        try
        {
            var service = new InvitationService(db);

            var request = new CreateInvitationRequest
            {
                Email = "newuser@example.com",
                ExpiresInDays = 7,
            };

            var created = await service.CreateInvitationAsync(request, "admin-1");

            Assert.NotNull(created);
            Assert.Equal("newuser@example.com", created.Email);
            Assert.StartsWith("inv_", created.Token);
            Assert.True(created.IsValid);
            Assert.Equal("Attivo", created.Status);

            var stored = await db.Invitations.FirstOrDefaultAsync(x => x.Id == created.Id);
            Assert.NotNull(stored);
            Assert.Equal(InvitationService.HashToken(created.Token), stored.TokenHash);
            Assert.NotEqual(created.Token, stored.TokenHash); // Token is hashed, never plaintext in DB
        }
        finally
        {
            db.Dispose();
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task ValidateInvitationSucceedsForValidTokenAndFailsForInvalid()
    {
        var (db, tempDir) = CreateTestContext();
        try
        {
            var service = new InvitationService(db);

            var created = await service.CreateInvitationAsync(new CreateInvitationRequest
            {
                Email = "valid@example.com",
                ExpiresInDays = 5,
            }, "admin-1");

            var validResult = await service.ValidateInvitationAsync(created.Token);
            Assert.True(validResult.IsValid);
            Assert.NotNull(validResult.Invitation);
            Assert.Equal("valid@example.com", validResult.Invitation.Email);

            var invalidResult = await service.ValidateInvitationAsync("inv_nonexistent");
            Assert.False(invalidResult.IsValid);
            Assert.Null(validResult.ErrorMessage);
            Assert.NotNull(invalidResult.ErrorMessage);
        }
        finally
        {
            db.Dispose();
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task ConsumeInvitationMarksUsedAndPreventsReplay()
    {
        var (db, tempDir) = CreateTestContext();
        try
        {
            var service = new InvitationService(db);

            var created = await service.CreateInvitationAsync(new CreateInvitationRequest
            {
                Email = "consume@example.com",
                ExpiresInDays = 3,
            }, "admin-1");

            var consumed = await service.ConsumeInvitationAsync(created.Token, "registered-user-99");
            Assert.True(consumed);

            // Replay attempt fails
            var replay = await service.ConsumeInvitationAsync(created.Token, "registered-user-100");
            Assert.False(replay);

            var validation = await service.ValidateInvitationAsync(created.Token);
            Assert.False(validation.IsValid);
            Assert.Contains("già stato utilizzato", validation.ErrorMessage ?? "");
        }
        finally
        {
            db.Dispose();
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task RevokeInvitationPreventsUsage()
    {
        var (db, tempDir) = CreateTestContext();
        try
        {
            var service = new InvitationService(db);

            var created = await service.CreateInvitationAsync(new CreateInvitationRequest
            {
                Email = "revoke@example.com",
                ExpiresInDays = 3,
            }, "admin-1");

            var revoked = await service.RevokeInvitationAsync(created.Id, "admin-1");
            Assert.True(revoked);

            var validation = await service.ValidateInvitationAsync(created.Token);
            Assert.False(validation.IsValid);
            Assert.Contains("revocato", validation.ErrorMessage ?? "");
        }
        finally
        {
            db.Dispose();
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}

