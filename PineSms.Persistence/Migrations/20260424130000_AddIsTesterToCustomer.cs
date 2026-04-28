using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PineSms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTesterToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTester",
                table: "Customer",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTester",
                table: "Customer");
        }
    }
}
