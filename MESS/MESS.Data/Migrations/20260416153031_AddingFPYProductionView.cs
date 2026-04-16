using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingFPYProductionView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW ""FPYProductionView"" AS
                WITH ""LogStatus"" AS (
                    SELECT
                        ""pl"".""Id"" AS ""ProductionLogId"",
                        ""pl"".""WorkInstructionId"",
                        ""pl"".""ProductId"",
                        ""pl"".""CreatedOn"" AS ""ProductionLogTime"",
                        CASE
                            WHEN EXISTS (
                                SELECT 1
                                FROM ""ProductionLogSteps"" ""pls""
                                JOIN ""ProductionLogStepAttempts"" ""a""
                                    ON ""a"".""ProductionLogStepId"" = ""pls"".""Id""
                                WHERE ""pls"".""ProductionLogId"" = ""pl"".""Id""
                                  AND NOT ""a"".""Success""
                            )
                                THEN 'Has Failures'
                            ELSE 'All Successes'
                        END AS ""LogStatus""
                    FROM ""ProductionLogs"" ""pl""
                )
                SELECT
                    ""ls"".""ProductionLogId"",
                    ""ls"".""LogStatus"",
                    ""pd"".""Name"" AS ""ProductName"",
                    ""wi"".""Title"" AS ""WorkInstructionTitle"",
                    ""wi"".""Version"" AS ""WorkInstructionVersion"",
                    ""ls"".""ProductionLogTime""
                FROM ""LogStatus"" ""ls""
                JOIN ""Products"" ""p""
                    ON ""p"".""Id"" = ""ls"".""ProductId""
                JOIN ""PartDefinitions"" ""pd""
                    ON ""pd"".""Id"" = ""p"".""PartDefinitionId""
                JOIN ""WorkInstructions"" ""wi""
                    ON ""wi"".""Id"" = ""ls"".""WorkInstructionId""
                ORDER BY ""ls"".""ProductionLogTime"" DESC;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP VIEW IF EXISTS ""FPYProductionView"";
            ");
        }
    }
}