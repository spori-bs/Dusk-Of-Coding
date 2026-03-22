using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuskOfCoding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHelloWorldSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "TestBundleReference",
                value: "using System;\r\nusing Xunit;\r\n\r\npublic class SolutionTests \r\n{\r\n    [Fact]\r\n    public void TestHelloWorld() \r\n    {\r\n        var result = Solution.GetHelloWorld();\r\n        Assert.Equal(\"Hello World!\", result);\r\n    }\r\n}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "TestBundleReference",
                value: "tasks/helloworld/tests.csproj");
        }
    }
}
