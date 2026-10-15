using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SekProduction.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProductionAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProductions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductions_Productions_ProductionId",
                        column: x => x.ProductionId,
                        principalTable: "Productions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductions_ProductionId",
                table: "UserProductions",
                column: "ProductionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductions_UserId_ProductionId",
                table: "UserProductions",
                columns: new[] { "UserId", "ProductionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProductions");
        }
    }
}
