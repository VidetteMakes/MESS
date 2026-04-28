using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESS.Data.Migrations;

/// <summary>
/// Moves traceability/input type from PartNodes to PartDefinitions (single source per part).
/// </summary>
public partial class MovePartInputTypeToPartDefinition : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InputType",
            table: "PartDefinitions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE "PartDefinitions" pd
            SET "InputType" = sub."InputType"
            FROM (
                SELECT DISTINCT ON ("PartDefinitionId") "PartDefinitionId", "InputType"
                FROM "PartNodes"
                ORDER BY "PartDefinitionId", "Id"
            ) sub
            WHERE pd."Id" = sub."PartDefinitionId";
            """);

        migrationBuilder.DropColumn(
            name: "InputType",
            table: "PartNodes");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InputType",
            table: "PartNodes",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE "PartNodes" pn
            SET "InputType" = pd."InputType"
            FROM "PartDefinitions" pd
            WHERE pn."PartDefinitionId" = pd."Id";
            """);

        migrationBuilder.DropColumn(
            name: "InputType",
            table: "PartDefinitions");
    }
}