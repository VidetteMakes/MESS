using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESS.Data.Migrations
{
    /// <inheritdoc />
    public partial class IsSerialNumberUniqueFlagAndRenamingPartRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SerializablePartRelationship_SerializableParts_ChildPartId",
                table: "SerializablePartRelationship");

            migrationBuilder.DropForeignKey(
                name: "FK_SerializablePartRelationship_SerializableParts_ParentPartId",
                table: "SerializablePartRelationship");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SerializablePartRelationship",
                table: "SerializablePartRelationship");

            migrationBuilder.RenameTable(
                name: "SerializablePartRelationship",
                newName: "SerializablePartRelationships");

            migrationBuilder.RenameIndex(
                name: "IX_SerializablePartRelationship_ParentPartId",
                table: "SerializablePartRelationships",
                newName: "IX_SerializablePartRelationships_ParentPartId");

            migrationBuilder.RenameIndex(
                name: "IX_SerializablePartRelationship_ChildPartId",
                table: "SerializablePartRelationships",
                newName: "IX_SerializablePartRelationships_ChildPartId");

            migrationBuilder.AddColumn<bool>(
                name: "IsSerialNumberUnique",
                table: "PartDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SerializablePartRelationships",
                table: "SerializablePartRelationships",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SerializablePartRelationships_SerializableParts_ChildPartId",
                table: "SerializablePartRelationships",
                column: "ChildPartId",
                principalTable: "SerializableParts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SerializablePartRelationships_SerializableParts_ParentPartId",
                table: "SerializablePartRelationships",
                column: "ParentPartId",
                principalTable: "SerializableParts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SerializablePartRelationships_SerializableParts_ChildPartId",
                table: "SerializablePartRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_SerializablePartRelationships_SerializableParts_ParentPartId",
                table: "SerializablePartRelationships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SerializablePartRelationships",
                table: "SerializablePartRelationships");

            migrationBuilder.DropColumn(
                name: "IsSerialNumberUnique",
                table: "PartDefinitions");

            migrationBuilder.RenameTable(
                name: "SerializablePartRelationships",
                newName: "SerializablePartRelationship");

            migrationBuilder.RenameIndex(
                name: "IX_SerializablePartRelationships_ParentPartId",
                table: "SerializablePartRelationship",
                newName: "IX_SerializablePartRelationship_ParentPartId");

            migrationBuilder.RenameIndex(
                name: "IX_SerializablePartRelationships_ChildPartId",
                table: "SerializablePartRelationship",
                newName: "IX_SerializablePartRelationship_ChildPartId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SerializablePartRelationship",
                table: "SerializablePartRelationship",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SerializablePartRelationship_SerializableParts_ChildPartId",
                table: "SerializablePartRelationship",
                column: "ChildPartId",
                principalTable: "SerializableParts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SerializablePartRelationship_SerializableParts_ParentPartId",
                table: "SerializablePartRelationship",
                column: "ParentPartId",
                principalTable: "SerializableParts",
                principalColumn: "Id");
        }
    }
}
