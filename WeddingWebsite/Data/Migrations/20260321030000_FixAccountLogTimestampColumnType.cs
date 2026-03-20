using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeddingWebsite.Data;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260321030000_FixAccountLogTimestampColumnType")]
    public partial class FixAccountLogTimestampColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AccountLog.Timestamp was created as INTEGER (int4) but the application wrote
            // .NET DateTime.Ticks (~638 trillion), which silently overflows int4 (max ~2.1 billion).
            // The stored values are int4-wrapped remnants of those ticks — not recoverable.
            // We convert the column type and set all existing rows to the Unix epoch as a placeholder;
            // the actual log timestamps are lost.
            migrationBuilder.Sql(
                "ALTER TABLE \"AccountLog\" ALTER COLUMN \"Timestamp\" " +
                "TYPE timestamp with time zone USING '1970-01-01 00:00:00+00'::timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"AccountLog\" ALTER COLUMN \"Timestamp\" " +
                "TYPE integer USING EXTRACT(EPOCH FROM \"Timestamp\")::integer");
        }
    }
}
