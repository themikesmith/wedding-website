using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    public partial class FixRegistryItemsHideColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"Hide\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"Hide\" TYPE boolean USING CAST(\"Hide\"::int AS boolean)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"Hide\" SET DEFAULT false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"Hide\" DROP DEFAULT");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"Hide\" TYPE integer USING CAST(\"Hide\" AS int)");
            migrationBuilder.Sql("ALTER TABLE \"RegistryItems\" ALTER COLUMN \"Hide\" SET DEFAULT 0");
        }
    }
}
