using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeddingWebsite.Data;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260321010000_FixRegistryDecimalColumnTypes")]
    public partial class FixRegistryDecimalColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RegistryItemPurchaseMethods.Cost: REAL (SQLite double) → numeric (PostgreSQL decimal)
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"Cost\" TYPE numeric USING \"Cost\"::numeric");

            // RegistryItemPurchaseMethods.DeliveryCost: REAL → numeric
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"DeliveryCost\" DROP DEFAULT");
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"DeliveryCost\" TYPE numeric USING \"DeliveryCost\"::numeric");
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"DeliveryCost\" SET DEFAULT 0");

            // RegistryItemClaims.Contribution: REAL → numeric
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemClaims\" ALTER COLUMN \"Contribution\" TYPE numeric USING \"Contribution\"::numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"Cost\" TYPE real USING \"Cost\"::real");

            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"DeliveryCost\" DROP DEFAULT");
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"DeliveryCost\" TYPE real USING \"DeliveryCost\"::real");
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemPurchaseMethods\" ALTER COLUMN \"DeliveryCost\" SET DEFAULT 0");

            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemClaims\" ALTER COLUMN \"Contribution\" TYPE real USING \"Contribution\"::real");
        }
    }
}
