using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QrApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QRCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    ScanCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_ShortCode",
                table: "QRCodes",
                column: "ShortCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QRCodes");
        }
    }
}
