using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESS.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartRelationshipsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW ""PartRelationshipsView"" AS
                SELECT
                    child.""Id"" AS ""ChildPartId"",
                    child.""SerialNumber"" AS ""ChildSerialNumber"",
                    childDef.""Name"" AS ""ChildPartName"",
                    parent.""Id"" AS ""ParentPartId"",
                    parent.""SerialNumber"" AS ""ParentSerialNumber"",
                    parentDef.""Name"" AS ""ParentPartName"",
                    rel.""LastUpdated""
                FROM
                    ""SerializablePartRelationships"" AS rel
                LEFT JOIN
                    ""SerializableParts"" AS child
                    ON rel.""ChildPartId"" = child.""Id""
                LEFT JOIN
                    ""PartDefinitions"" AS childDef
                    ON child.""PartDefinitionId"" = childDef.""Id""
                LEFT JOIN
                    ""SerializableParts"" AS parent
                    ON rel.""ParentPartId"" = parent.""Id""
                LEFT JOIN
                    ""PartDefinitions"" AS parentDef
                    ON parent.""PartDefinitionId"" = parentDef.""Id""
                ORDER BY
                    parent.""Id"" NULLS FIRST,
                    child.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""PartRelationshipsView"";");
        }
    }
}