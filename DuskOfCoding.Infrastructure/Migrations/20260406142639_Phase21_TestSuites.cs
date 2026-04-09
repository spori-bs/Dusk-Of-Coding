using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuskOfCoding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase21_TestSuites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestBundleReference",
                table: "Tasks");

            migrationBuilder.CreateTable(
                name: "TaskTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTests_Tasks_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TaskTests",
                columns: new[] { "Id", "Code", "Name", "TaskDefinitionId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), "using System;\r\nusing Xunit;\r\n\r\npublic class SolutionTests \r\n{\r\n    [Fact]\r\n    public void TestHelloWorld() \r\n    {\r\n        var result = Solution.GetHelloWorld();\r\n        Assert.Equal(\"Hello World!\", result);\r\n    }\r\n}", "SolutionTests.cs", new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.CreateIndex(
                name: "IX_TaskTests_TaskDefinitionId",
                table: "TaskTests",
                column: "TaskDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskTests");

            migrationBuilder.AddColumn<string>(
                name: "TestBundleReference",
                table: "Tasks",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "TestBundleReference",
                value: "using System;\r\nusing Xunit;\r\n\r\npublic class SolutionTests \r\n{\r\n    [Fact]\r\n    public void TestHelloWorld() \r\n    {\r\n        var result = Solution.GetHelloWorld();\r\n        Assert.Equal(\"Hello World!\", result);\r\n    }\r\n}");
        }
    }
}
