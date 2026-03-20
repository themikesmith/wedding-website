using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimedByColumnNameMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: the DB column has always been "ClaimedBy" (created in AddRegistryTable).
            // This migration exists only to update the EF model snapshot to reflect the
            // HasColumnName("ClaimedBy") mapping added to ApplicationDbContext.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: nothing was changed in Up, nothing to revert.
        }
    }
}
