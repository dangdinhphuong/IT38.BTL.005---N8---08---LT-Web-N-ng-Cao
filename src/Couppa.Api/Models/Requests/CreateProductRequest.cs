using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Couppa.Api.Models.Requests;

public class CreateProductRequest
{
    [Required]
    [MaxLength(50)]
    public required string Sku { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public long CategoryId { get; set; }

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    // SEC-06: chặn giá/tồn kho âm ngay ở model binding (BR-09/BR-10), trước khi vào Service.
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm không được âm.")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm.")]
    public int StockQuantity { get; set; }

    public bool IsFeatured { get; set; }

    /// <summary>
    /// Ảnh được gửi bằng multipart/form-data từ Admin. File sẽ được lưu vào
    /// wwwroot/uploads/products và chỉ URL tương đối được lưu trong database.
    /// </summary>
    public List<IFormFile> ImageFiles { get; set; } = new();

    // Giữ property này cho các test/service caller nội bộ cũ. Form Admin không
    // còn nhận URL ảnh từ người dùng.
    public string[] ImageUrls { get; set; } = Array.Empty<string>();
}
