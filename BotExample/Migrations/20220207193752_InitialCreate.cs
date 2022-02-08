using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotExample.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CussLog",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CussWord = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CussLog", x => x.LogId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CussLog");
        }
    }
}
