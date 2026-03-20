using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeddingWebsite.Data;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260321020000_FixRegistryClaimDateTimeColumnTypes")]
    public partial class FixRegistryClaimDateTimeColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RegistryItemClaims.ClaimedAt: INTEGER (SQLite, stored as ticks) → timestamp with time zone
            // Existing integer values are treated as Unix epoch seconds for any surviving data.
            // In practice this table was empty on PostgreSQL because ticks (~638B) overflow a 4-byte INTEGER.
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemClaims\" ALTER COLUMN \"ClaimedAt\" " +
                "TYPE timestamp with time zone USING to_timestamp(\"ClaimedAt\")");

            // RegistryItemClaims.CompletedAt: INTEGER (nullable) → timestamp with time zone
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemClaims\" ALTER COLUMN \"CompletedAt\" " +
                "TYPE timestamp with time zone USING CASE WHEN \"CompletedAt\" IS NULL THEN NULL " +
                "ELSE to_timestamp(\"CompletedAt\") END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemClaims\" ALTER COLUMN \"ClaimedAt\" " +
                "TYPE integer USING EXTRACT(EPOCH FROM \"ClaimedAt\")::integer");

            migrationBuilder.Sql(
                "ALTER TABLE \"RegistryItemClaims\" ALTER COLUMN \"CompletedAt\" " +
                "TYPE integer USING CASE WHEN \"CompletedAt\" IS NULL THEN NULL " +
                "ELSE EXTRACT(EPOCH FROM \"CompletedAt\")::integer END");
        }
    }
}
