using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SekProduction.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventScheduleId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AuthorName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventNotes_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventNotes_EventSchedules_EventScheduleId",
                        column: x => x.EventScheduleId,
                        principalTable: "EventSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventNotes_AuthorId",
                table: "EventNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventNotes_EventScheduleId_Scope",
                table: "EventNotes",
                columns: new[] { "EventScheduleId", "Scope" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventNotes");
        }
    }
}
