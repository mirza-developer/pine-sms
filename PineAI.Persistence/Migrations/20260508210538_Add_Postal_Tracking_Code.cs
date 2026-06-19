using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PineAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Postal_Tracking_Code : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostalTrackingCode",
                table: "CustomerOrder",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostalTrackingCode",
                table: "CustomerOrder");
        }
    }
}
