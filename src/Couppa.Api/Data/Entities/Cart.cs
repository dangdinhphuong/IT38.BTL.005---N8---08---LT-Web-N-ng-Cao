namespace Couppa.Api.Data.Entities;

/// <summary>
/// DD-02: giỏ hàng luôn lưu DB kể cả của Guest. Đúng 1 trong 2 (UserId, SessionId) khác NULL (BR-12) —
/// ràng buộc CHECK tương ứng được khai báo ở AppDbContext.OnModelCreating.
/// </summary>
public class Cart
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ApplicationUser? User { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
