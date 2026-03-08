using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeddingWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDonationToRegistryItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "IsDonation", table: "RegistryItems", type: "INTEGER", defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDonation", table: "RegistryItems");
        }
    }
}
