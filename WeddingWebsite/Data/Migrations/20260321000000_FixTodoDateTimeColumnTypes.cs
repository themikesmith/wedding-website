using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeddingWebsite.Data;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260321000000_FixTodoDateTimeColumnTypes")]
    public partial class FixTodoDateTimeColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert TodoItems.WaitingUntil from TEXT (SQLite legacy) to timestamp with time zone
            migrationBuilder.Sql(
                "ALTER TABLE \"TodoItems\" ALTER COLUMN \"WaitingUntil\" TYPE timestamp with time zone " +
                "USING \"WaitingUntil\"::timestamp with time zone");

            // Convert TodoItems.CompletedAt from TEXT (SQLite legacy) to timestamp with time zone
            migrationBuilder.Sql(
                "ALTER TABLE \"TodoItems\" ALTER COLUMN \"CompletedAt\" TYPE timestamp with time zone " +
                "USING \"CompletedAt\"::timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"TodoItems\" ALTER COLUMN \"WaitingUntil\" TYPE text " +
                "USING \"WaitingUntil\"::text");

            migrationBuilder.Sql(
                "ALTER TABLE \"TodoItems\" ALTER COLUMN \"CompletedAt\" TYPE text " +
                "USING \"CompletedAt\"::text");
        }
    }
}
