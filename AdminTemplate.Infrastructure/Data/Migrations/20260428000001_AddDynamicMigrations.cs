using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminTemplate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DynamicMigrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MigrationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpSql = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DownSql = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicMigrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DynamicMigrations_MigrationName",
                table: "DynamicMigrations",
                column: "MigrationName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DynamicMigrations");
        }
    }
}
