using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PineSms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBotChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotChatMessage",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    BaleUsername = table.Column<string>(maxLength: 64, nullable: false),
                    ChatId = table.Column<long>(nullable: false),
                    MessageText = table.Column<string>(nullable: false),
                    IsFromBot = table.Column<bool>(nullable: false),
                    SentAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotChatMessage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotChatMessage_BaleUsername",
                table: "BotChatMessage",
                column: "BaleUsername");

            migrationBuilder.CreateIndex(
                name: "IX_BotChatMessage_SentAt",
                table: "BotChatMessage",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BotChatMessage");
        }
    }
}
