using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuskOfCoding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpectedClassName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpectedClassName",
                table: "Tasks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "ExpectedClassName",
                value: "Solution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedClassName",
                table: "Tasks");
        }
    }
}
