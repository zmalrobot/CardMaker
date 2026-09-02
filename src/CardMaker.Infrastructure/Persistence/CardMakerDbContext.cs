using CardMaker.Domain.Assets;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Identity;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;
using CardMaker.Domain.Templates;
using CardMaker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Persistence;

public class CardMakerDbContext(DbContextOptions<CardMakerDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Game> Games => Set<Game>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<FontAsset> FontAssets => Set<FontAsset>();

    public DbSet<SymbolSet> SymbolSets => Set<SymbolSet>();

    public DbSet<Symbol> Symbols => Set<Symbol>();

    public DbSet<OptionList> OptionLists => Set<OptionList>();

    public DbSet<OptionItem> OptionItems => Set<OptionItem>();

    public DbSet<CardType> CardTypes => Set<CardType>();

    public DbSet<Trait> Traits => Set<Trait>();

    public DbSet<CardTypeTrait> CardTypeTraits => Set<CardTypeTrait>();

    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();

    public DbSet<Template> Templates => Set<Template>();

    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();

    public DbSet<Card> Cards => Set<Card>();

    public DbSet<CardRender> CardRenders => Set<CardRender>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    /// <summary>
    /// LocalizedText va dichiarato qui: nelle convenzioni pre-modello, altrimenti EF lo tratta
    /// come entita' a se' stante e pretende una chiave primaria.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<LocalizedText>()
            .HaveConversion<LocalizedTextConverter, LocalizedTextComparer>()
            .HaveColumnType("TEXT");

        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToTicksConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Game>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.DefaultCulture).HasMaxLength(16);
        });

        builder.Entity<Asset>(e =>
        {
            e.HasIndex(x => x.Sha256);
            e.HasIndex(x => new { x.GameId, x.Category });
            e.Property(x => x.Sha256).HasMaxLength(64);
            e.Property(x => x.ContentType).HasMaxLength(128);
            e.Property(x => x.OriginalFileName).HasMaxLength(260);
            e.HasOne(x => x.Game).WithMany(g => g.Assets)
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FontAsset>(e =>
        {
            e.HasIndex(x => new { x.GameId, x.Alias }).IsUnique();
            e.Property(x => x.Alias).HasMaxLength(64);
            e.HasOne(x => x.Asset).WithMany()
                .HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Game).WithMany(g => g.Fonts)
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SymbolSet>(e =>
        {
            e.HasIndex(x => new { x.GameId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.HasOne(x => x.Game).WithMany(g => g.SymbolSets)
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Symbol>(e =>
        {
            e.HasIndex(x => new { x.SymbolSetId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.InlineToken).HasMaxLength(128);
            e.HasOne(x => x.SymbolSet).WithMany(s => s.Symbols)
                .HasForeignKey(x => x.SymbolSetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Asset).WithMany()
                .HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OptionList>(e =>
        {
            e.HasIndex(x => new { x.GameId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.HasOne(x => x.Game).WithMany(g => g.OptionLists)
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OptionItem>(e =>
        {
            e.HasIndex(x => new { x.OptionListId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.HasOne(x => x.OptionList).WithMany(l => l.Items)
                .HasForeignKey(x => x.OptionListId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Symbol).WithMany()
                .HasForeignKey(x => x.SymbolId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CardType>(e =>
        {
            e.HasIndex(x => new { x.GameId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.HasOne(x => x.Game).WithMany(g => g.CardTypes)
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.IconAsset).WithMany()
                .HasForeignKey(x => x.IconAssetId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Trait>(e =>
        {
            e.HasIndex(x => new { x.GameId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.Property(x => x.Group).HasMaxLength(64);
            e.HasOne(x => x.Game).WithMany(g => g.Traits)
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CardTypeTrait>(e =>
        {
            e.HasKey(x => new { x.CardTypeId, x.TraitId });
            e.HasOne(x => x.CardType).WithMany(c => c.AllowedTraits)
                .HasForeignKey(x => x.CardTypeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Trait).WithMany(t => t.CardTypes)
                .HasForeignKey(x => x.TraitId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FieldDefinition>(e =>
        {
            e.HasIndex(x => new { x.CardTypeId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.HasOne(x => x.CardType).WithMany(c => c.Fields)
                .HasForeignKey(x => x.CardTypeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OptionList).WithMany()
                .HasForeignKey(x => x.OptionListId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SymbolSet).WithMany()
                .HasForeignKey(x => x.SymbolSetId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Template>(e =>
        {
            e.HasIndex(x => new { x.CardTypeId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64);
            e.HasOne(x => x.CardType).WithMany(c => c.Templates)
                .HasForeignKey(x => x.CardTypeId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TemplateVersion>(e =>
        {
            e.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique();
            e.HasOne(x => x.Template).WithMany(t => t.Versions)
                .HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Card>(e =>
        {
            e.HasIndex(x => new { x.OwnerUserId, x.UpdatedAtUtc });
            e.Property(x => x.OwnerUserId).HasMaxLength(450);
            e.Property(x => x.Title).HasMaxLength(200);
            e.HasOne(x => x.Game).WithMany()
                .HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CardType).WithMany()
                .HasForeignKey(x => x.CardTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TemplateVersion).WithMany()
                .HasForeignKey(x => x.TemplateVersionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BackTemplateVersion).WithMany()
                .HasForeignKey(x => x.BackTemplateVersionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ThumbnailAsset).WithMany()
                .HasForeignKey(x => x.ThumbnailAssetId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CardRender>(e =>
        {
            e.HasIndex(x => x.CacheKey).IsUnique();
            e.Property(x => x.CacheKey).HasMaxLength(128);
            e.Property(x => x.Sha256).HasMaxLength(64);
            e.HasOne(x => x.Card).WithMany(c => c.Renders)
                .HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Invitation>(e =>
        {
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.Email);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.Property(x => x.Role).HasMaxLength(64);
        });

        builder.Entity<AuditLogEntry>(e =>
        {
            e.HasIndex(x => x.CreatedAtUtc);
            e.Property(x => x.Action).HasMaxLength(128);
            e.Property(x => x.EntityName).HasMaxLength(128);
            e.Property(x => x.IpAddress).HasMaxLength(64);
        });
    }
}
