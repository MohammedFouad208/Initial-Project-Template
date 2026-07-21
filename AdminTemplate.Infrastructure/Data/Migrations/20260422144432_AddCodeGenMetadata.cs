using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminTemplate.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeGenMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "EntityDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsUnique = table.Column<bool>(type: "bit", nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShowInList = table.Column<bool>(type: "bit", nullable: false),
                    ShowInForm = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityColumns_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ForeignKeyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NavigationPropertyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayColumn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityRelations_EntityDefinitions_EntityDefinitionId",
                        column: x => x.EntityDefinitionId,
                        principalTable: "EntityDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityColumns_EntityDefinitionId",
                table: "EntityColumns",
                column: "EntityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityDefinitions_Name",
                table: "EntityDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityRelations_EntityDefinitionId",
                table: "EntityRelations",
                column: "EntityDefinitionId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "EntityColumns");

            migrationBuilder.DropTable(
                name: "EntityRelations");

            migrationBuilder.DropTable(
                name: "EntityDefinitions");
        }
    }
}
