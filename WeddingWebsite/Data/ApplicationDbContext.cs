using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WeddingWebsite.Models.Registry;

namespace WeddingWebsite.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<Account>(options)
{
    public DbSet<RegistryItem> RegistryItems { get; set; }
    public DbSet<RegistryItemClaim> RegistryItemClaims { get; set; }
    public DbSet<RegistryItemPurchaseMethod> RegistryItemPurchaseMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure RegistryItem
        modelBuilder.Entity<RegistryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GenericName).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.MaxQuantity).HasDefaultValue(1);
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.Hide).HasColumnType("boolean").HasDefaultValue(false);
            entity.Property(e => e.AllowsPartialContributions).HasColumnType("boolean").HasDefaultValue(false);
            entity.Property(e => e.IsDonation).HasColumnType("boolean").HasDefaultValue(false);
        });

        // Configure RegistryItemPurchaseMethod
        modelBuilder.Entity<RegistryItemPurchaseMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.AllowBringOnDay).HasColumnType("boolean").HasDefaultValue(true);
            entity.Property(e => e.AllowDeliverToUs).HasColumnType("boolean").HasDefaultValue(true);
            entity.Property(e => e.DeliveryCost).HasDefaultValue(0m);
            
            // Shadow property for foreign key to RegistryItem (actual DB column is "ItemId")
            entity.Property<string>("ItemId").IsRequired();
            
            // Explicitly map the FK so EF uses "ItemId" not convention-based "RegistryItemId"
            entity.HasOne<RegistryItem>()
                .WithMany(r => r.PurchaseMethods)
                .HasForeignKey("ItemId");
        });

        // Configure RegistryItemClaim
        modelBuilder.Entity<RegistryItemClaim>(entity =>
        {
            // The original migration created the PK column as "ClaimedBy", but the C# property is UserId.
            entity.Property(e => e.UserId).HasColumnName("ClaimedBy");
            entity.HasKey(e => new { e.ItemId, e.UserId });
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.ClaimedAt).HasDefaultValueSql("now()");
            
            // Explicitly map the FK so EF uses "ItemId" not convention-based "RegistryItemId"
            entity.HasOne<RegistryItem>()
                .WithMany(r => r.Claims)
                .HasForeignKey(e => e.ItemId);
        });
    }
}

