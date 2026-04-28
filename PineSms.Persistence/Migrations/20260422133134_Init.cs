using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PineSms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    PhoneNumber = table.Column<string>(maxLength: 10, nullable: false),
                    SaveDate = table.Column<DateTime>(nullable: false),
                    SaveUserId = table.Column<string>(nullable: false),
                    SaveType = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: true),
                    Gender = table.Column<int>(nullable: true),
                    BirthYear = table.Column<int>(nullable: true),
                    BirthDate = table.Column<DateTime>(nullable: true),
                    LastUsageDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsLog",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    SendDate = table.Column<DateTime>(nullable: false),
                    SendUserId = table.Column<string>(nullable: false),
                    MessageText = table.Column<string>(nullable: false),
                    FromNumber = table.Column<string>(nullable: false),
                    RecipientsJson = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsSendJob",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(nullable: false),
                    FromNumber = table.Column<string>(nullable: false),
                    MessageText = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsSendJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsSendJobPart",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<int>(nullable: false),
                    PartNumber = table.Column<int>(nullable: false),
                    ScheduledAt = table.Column<DateTime>(nullable: false),
                    CustomerIdsJson = table.Column<string>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    SentCount = table.Column<int>(nullable: false),
                    ExecutedAt = table.Column<DateTime>(nullable: true),
                    ResultJson = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsSendJobPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsSendJobPart_SmsSendJob_JobId",
                        column: x => x.JobId,
                        principalTable: "SmsSendJob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmsSendJobPart_JobId",
                table: "SmsSendJobPart",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsSendJobPart_Status_ScheduledAt",
                table: "SmsSendJobPart",
                columns: new[] { "Status", "ScheduledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "SmsLog");

            migrationBuilder.DropTable(
                name: "SmsSendJobPart");

            migrationBuilder.DropTable(
                name: "SmsSendJob");
        }
    }
}
