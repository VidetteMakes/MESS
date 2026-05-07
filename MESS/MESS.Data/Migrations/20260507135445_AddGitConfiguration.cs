using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGitConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GitConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    RemoteUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    AuthType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CredentialReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitConfiguration", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GitConfiguration");
        }
    }
}
