using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TawtheefTest.Migrations
{
    public partial class RemoveContentSourceTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentSources");

            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "QuestionSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentSourceType",
                table: "QuestionSets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "QuestionSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUploadedCode",
                table: "QuestionSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "QuestionSets",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "QuestionSets");

            migrationBuilder.DropColumn(
                name: "ContentSourceType",
                table: "QuestionSets");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "QuestionSets");

            migrationBuilder.DropColumn(
                name: "FileUploadedCode",
                table: "QuestionSets");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "QuestionSets");

            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FileId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuestionSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedFileId = table.Column<int>(type: "INTEGER", nullable: true),
                    Content = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    ContentSourceType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentSources_QuestionSets_QuestionSetId",
                        column: x => x.QuestionSetId,
                        principalTable: "QuestionSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentSources_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentSources_QuestionSetId",
                table: "ContentSources",
                column: "QuestionSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentSources_UploadedFileId",
                table: "ContentSources",
                column: "UploadedFileId");
        }
    }
}
