using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TawtheefTest.Migrations
{
    public partial class EnhancedEvaluationSystemFinal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Exams_ExamId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ExamId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ExamId",
                table: "Questions");

            migrationBuilder.AlterColumn<string>(
                name: "NumberOfCorrectOptions",
                table: "QuestionSets",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DifficultyLevel",
                table: "Questions",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CompletionDuration",
                table: "CandidateExams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EasyQuestionsCorrect",
                table: "CandidateExams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HardQuestionsCorrect",
                table: "CandidateExams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxPossiblePoints",
                table: "CandidateExams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MediumQuestionsCorrect",
                table: "CandidateExams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RankPosition",
                table: "CandidateExams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "CandidateExams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TrueFalseAnswer",
                table: "CandidateAnswers",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DifficultyLevel",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CompletionDuration",
                table: "CandidateExams");

            migrationBuilder.DropColumn(
                name: "EasyQuestionsCorrect",
                table: "CandidateExams");

            migrationBuilder.DropColumn(
                name: "HardQuestionsCorrect",
                table: "CandidateExams");

            migrationBuilder.DropColumn(
                name: "MaxPossiblePoints",
                table: "CandidateExams");

            migrationBuilder.DropColumn(
                name: "MediumQuestionsCorrect",
                table: "CandidateExams");

            migrationBuilder.DropColumn(
                name: "RankPosition",
                table: "CandidateExams");

            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "CandidateExams");

            migrationBuilder.DropColumn(
                name: "TrueFalseAnswer",
                table: "CandidateAnswers");

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfCorrectOptions",
                table: "QuestionSets",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExamId",
                table: "Questions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExamId",
                table: "Questions",
                column: "ExamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Exams_ExamId",
                table: "Questions",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
