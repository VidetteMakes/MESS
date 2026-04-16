using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartTraceabilityView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW ""PartTraceabilityView"" AS
                SELECT
                    sp.""Id"" AS ""PartId"",
                    sp.""SerialNumber"",
                    pd.""Name"" AS ""PartName"",

                    -- Earliest ProductionLog timestamp for this part
                    MIN(pl.""CreatedOn"") AS ""CreatedOn"",

                    -- Boolean: has at least one parent
                    EXISTS (
                        SELECT 1
                        FROM ""SerializablePartRelationships"" rel_parent
                        WHERE rel_parent.""ChildPartId"" = sp.""Id""
                    ) AS ""HasParents"",

                    -- Boolean: has at least one child
                    EXISTS (
                        SELECT 1
                        FROM ""SerializablePartRelationships"" rel_child
                        WHERE rel_child.""ParentPartId"" = sp.""Id""
                    ) AS ""HasChildren""

                FROM ""SerializableParts"" sp

                LEFT JOIN ""PartDefinitions"" pd
                    ON sp.""PartDefinitionId"" = pd.""Id""

                LEFT JOIN ""ProductionLogParts"" plp
                    ON plp.""SerializablePartId"" = sp.""Id""

                LEFT JOIN ""ProductionLogs"" pl
                    ON pl.""Id"" = plp.""ProductionLogId""

                GROUP BY
                    sp.""Id"",
                    sp.""SerialNumber"",
                    pd.""Name""

                ORDER BY
                    sp.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP VIEW IF EXISTS ""PartTraceabilityView"";
            ");
        }
    }
}