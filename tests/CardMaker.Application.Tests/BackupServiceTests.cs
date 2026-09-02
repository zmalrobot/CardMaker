using CardMaker.Infrastructure.Admin;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CardMaker.Application.Tests;

public sealed class BackupServiceTests
{
    [Fact]
    public async Task CreateBackupAndVerifyIntegrityWithSqlite()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CardMaker_BackupTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "cardmaker.db");

        try
        {
            var options = new DbContextOptionsBuilder<CardMakerDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using (var db = new CardMakerDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
            }

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:DataRoot"] = tempDir,
                })
                .Build();

            using (var db = new CardMakerDbContext(options))
            {
                var service = new BackupService(db, config, NullLogger<BackupService>.Instance);

                var integrityReport = await service.VerifyDatabaseIntegrityAsync();
                Assert.True(integrityReport.IsHealthy);
                Assert.Equal("ok", integrityReport.CheckResult);

                var backupInfo = await service.CreateBackupAsync("admin-user-1");
                Assert.NotNull(backupInfo);
                Assert.True(File.Exists(backupInfo.FilePath));
                Assert.True(backupInfo.SizeBytes > 0);

                var backups = await service.ListBackupsAsync();
                Assert.Single(backups);
                Assert.Equal(backupInfo.FileName, backups[0].FileName);

                var deleted = await service.DeleteBackupAsync(backupInfo.FileName, "admin-user-1");
                Assert.True(deleted);

                var remainingBackups = await service.ListBackupsAsync();
                Assert.Empty(remainingBackups);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore transient lock release
                }
            }
        }
    }
}

