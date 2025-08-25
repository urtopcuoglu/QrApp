namespace QrApp.Models;

public class QRCodeEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = "Untitled";
    public string ShortCode { get; set; } = default!;
    public string TargetUrl { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpireAt { get; set; } // 1 yıl veya sınırsız(null)
    public bool Active { get; set; } = true;
    public long ScanCount { get; set; } = 0;
}