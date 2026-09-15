using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SekProduction.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegroupProductionCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski: 0 Tiyatro, 1 Konser, 2 Müzik Videosu, 3 Sanatçı Yönetimi, 4 Diğer
            // Yeni: 0 Chaplin Sanat, 1 Chaplin Çocuk Sanat, 2 Sek Production
            // Tiyatro oyunları Chaplin Sanat'ta kalır, geri kalan her şey Sek Production'a taşınır.
            migrationBuilder.Sql("UPDATE Productions SET Category = 2 WHERE Category >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Productions SET Category = 0 WHERE Category = 1");
            migrationBuilder.Sql("UPDATE Productions SET Category = 1 WHERE Category = 2");
        }
    }
}
