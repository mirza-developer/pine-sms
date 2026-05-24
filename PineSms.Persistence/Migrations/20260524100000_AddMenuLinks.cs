using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace PineSms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuLink",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(maxLength: 128, nullable: false),
                    Url = table.Column<string>(maxLength: 256, nullable: false),
                    IconName = table.Column<string>(maxLength: 64, nullable: false),
                    SectionLabel = table.Column<string>(maxLength: 64, nullable: false),
                    DisplayOrder = table.Column<int>(nullable: false),
                    IsShown = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuLink", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMenuLink",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(nullable: false),
                    MenuLinkId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMenuLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMenuLink_MenuLink_MenuLinkId",
                        column: x => x.MenuLinkId,
                        principalTable: "MenuLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MenuLink",
                columns: new[] { "Id", "DisplayOrder", "IconName", "IsShown", "SectionLabel", "Title", "Url" },
                values: new object[,]
                {
                    { 1, 1, "bi-house-door-fill", true, "منو", "خانه", "/" },
                    { 2, 2, "bi-person-plus-fill", true, "مشتریان", "افزودن مشتری", "/customer/add" },
                    { 3, 3, "bi-file-earmark-excel-fill", true, "مشتریان", "ورود از اکسل", "/customer/import" },
                    { 4, 4, "bi-tags-fill", true, "سفارشات", "وضعیت‌های سفارش", "/order/statuses" },
                    { 5, 5, "bi-box-seam-fill", true, "سفارشات", "بارکد پستی آناناس", "/order/ananas-tracking" },
                    { 6, 6, "bi-key-fill", true, "تنظیمات", "کلیدهای API", "/settings/apikeys" },
                    { 7, 7, "bi-chat-dots-fill", true, "ربات بله", "مکالمات کاربران", "/bot/conversations" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMenuLink_MenuLinkId",
                table: "UserMenuLink",
                column: "MenuLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMenuLink_UserId_MenuLinkId",
                table: "UserMenuLink",
                columns: new[] { "UserId", "MenuLinkId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserMenuLink");
            migrationBuilder.DropTable(name: "MenuLink");
        }
    }
}
