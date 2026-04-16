using System;
using MESS.Data.Context;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MESS.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationContext))]
    [Migration("20260416090000_AddDevelopmentBoardSettings")]
    public partial class AddDevelopmentBoardSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevelopmentBoardSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConnectionMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HostAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    ApiPath = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    WifiSsid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WifiPassword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SoftwareEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccessToken = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TimeoutMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentBoardSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevelopmentBoardSettings");
        }
    }
}
