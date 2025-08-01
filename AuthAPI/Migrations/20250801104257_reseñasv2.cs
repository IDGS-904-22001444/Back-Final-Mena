using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthAPI.Migrations
{
    /// <inheritdoc />
    public partial class reseñasv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "CustomerReviews",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "RepliedAt",
                table: "CustomerReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReviews_ClientId",
                table: "CustomerReviews",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerReviews_AspNetUsers_ClientId",
                table: "CustomerReviews",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerReviews_AspNetUsers_ClientId",
                table: "CustomerReviews");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReviews_ClientId",
                table: "CustomerReviews");

            migrationBuilder.DropColumn(
                name: "RepliedAt",
                table: "CustomerReviews");

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "CustomerReviews",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
