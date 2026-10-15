using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SekProduction.Web.Models;

// Turne şehirlerinde kalınabilecek oteller. Fiyatlar TL ve gecelik tutulur.
public class Hotel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Şehir girin."), StringLength(100)]
    [Display(Name = "Şehir")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Otel adı girin."), StringLength(150)]
    [Display(Name = "Otel Adı")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 5)]
    [Display(Name = "Yıldız")]
    public int? Stars { get; set; }

    [StringLength(250)]
    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [StringLength(40)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [StringLength(120)]
    [Display(Name = "Yetkili Kişi")]
    public string? ContactName { get; set; }

    [StringLength(300)]
    [Url(ErrorMessage = "Web adresi http:// veya https:// ile başlamalı.")]
    [Display(Name = "Web Sitesi")]
    public string? WebsiteUrl { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 1000000, ErrorMessage = "Fiyat 0 ile 1.000.000 arasında olmalı.")]
    [Display(Name = "Tek Kişilik Oda (gecelik ₺)")]
    public decimal? SingleRoomPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 1000000, ErrorMessage = "Fiyat 0 ile 1.000.000 arasında olmalı.")]
    [Display(Name = "Çift Kişilik Oda (gecelik ₺)")]
    public decimal? DoubleRoomPrice { get; set; }

    [Display(Name = "Kahvaltı dahil")]
    public bool BreakfastIncluded { get; set; } = true;

    // Fiyatlar her kaydedildiğinde güncellenir; ekip bilginin ne kadar güncel olduğunu görür.
    [Display(Name = "Fiyat Güncellenme Tarihi")]
    public DateTime? PriceUpdatedAt { get; set; }

    [Display(Name = "Notlar")]
    public string? Notes { get; set; }

    [Display(Name = "Anlaşmalı otel")]
    public bool IsPreferred { get; set; }

    [Display(Name = "Listede göster")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public bool HasPrice => SingleRoomPrice is not null || DoubleRoomPrice is not null;

    [NotMapped]
    public string StarsText => Stars is null ? "—" : new string('★', Stars.Value);
}
