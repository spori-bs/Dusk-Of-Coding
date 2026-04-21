using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuskOfCoding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmTelemetryTaskNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LlmTelemetryLogs_TaskId",
                table: "LlmTelemetryLogs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmTelemetryLogs_Timestamp",
                table: "LlmTelemetryLogs",
                column: "Timestamp");

            migrationBuilder.AddForeignKey(
                name: "FK_LlmTelemetryLogs_Tasks_TaskId",
                table: "LlmTelemetryLogs",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LlmTelemetryLogs_Tasks_TaskId",
                table: "LlmTelemetryLogs");

            migrationBuilder.DropIndex(
                name: "IX_LlmTelemetryLogs_TaskId",
                table: "LlmTelemetryLogs");

            migrationBuilder.DropIndex(
                name: "IX_LlmTelemetryLogs_Timestamp",
                table: "LlmTelemetryLogs");
        }
    }
}
