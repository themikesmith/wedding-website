using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeddingWebsite.Data;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260319000000_FixBooleanColumnTypes")]
    public partial class FixBooleanColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix RegistryItems.AllowsPartialContributions
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"AllowsPartialContributions\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"AllowsPartialContributions\" TYPE boolean USING CAST(\"AllowsPartialContributions\"::int AS boolean)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"AllowsPartialContributions\" SET DEFAULT false");

            // Fix RegistryItems.IsDonation
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"IsDonation\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"IsDonation\" TYPE boolean USING CAST(\"IsDonation\"::int AS boolean)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"IsDonation\" SET DEFAULT false");

            // Fix RegistryItemPurchaseMethods.AllowBringOnDay
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowBringOnDay\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowBringOnDay\" TYPE boolean USING CAST(\"AllowBringOnDay\"::int AS boolean)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowBringOnDay\" SET DEFAULT true");

            // Fix RegistryItemPurchaseMethods.AllowDeliverToUs
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowDeliverToUs\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowDeliverToUs\" TYPE boolean USING CAST(\"AllowDeliverToUs\"::int AS boolean)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowDeliverToUs\" SET DEFAULT true");

            // Fix RsvpFormResponses.IsAttending
            migrationBuilder.Sql("ALTER TABLE \"RsvpFormResponses\" ALTER COLUMN \"IsAttending\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RsvpFormResponses\" ALTER COLUMN \"IsAttending\" TYPE boolean USING CAST(\"IsAttending\"::int AS boolean)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert RegistryItems.AllowsPartialContributions
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"AllowsPartialContributions\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"AllowsPartialContributions\" TYPE integer USING CAST(\"AllowsPartialContributions\" AS int)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"AllowsPartialContributions\" SET DEFAULT 0");

            // Revert RegistryItems.IsDonation
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"IsDonation\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"IsDonation\" TYPE integer USING CAST(\"IsDonation\" AS int)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"IsDonation\" SET DEFAULT 0");

            // Revert RegistryItemPurchaseMethods.AllowBringOnDay
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowBringOnDay\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowBringOnDay\" TYPE integer USING CAST(\"AllowBringOnDay\" AS int)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowBringOnDay\" SET DEFAULT 1");

            // Revert RegistryItemPurchaseMethods.AllowDeliverToUs
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowDeliverToUs\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowDeliverToUs\" TYPE integer USING CAST(\"AllowDeliverToUs\" AS int)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"AllowDeliverToUs\" SET DEFAULT 1");

            // Revert RsvpFormResponses.IsAttending
            migrationBuilder.Sql("ALTER TABLE \"RsvpFormResponses\" ALTER COLUMN \"IsAttending\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RsvpFormResponses\" ALTER COLUMN \"IsAttending\" TYPE integer USING CAST(\"IsAttending\" AS int)");
        }
    }
}
