namespace QrApp.Dtos;

// oneYear: true -> 1 yıl; false -> sınırsız
public record CreateQrDto(
    string? Name,
    string? ShortCode,
    string TargetUrl,
    bool OneYear,
    bool Active = true
);