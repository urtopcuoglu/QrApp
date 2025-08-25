namespace QrApp.Dtos;

public record UpdateQrDto(
    string? Name,
    string? TargetUrl,
    bool? Active,
    bool? OneYear,     // null: dokunma; true: 1 yıl; false: sınırsız
    bool? ResetOneYear // true ise CreatedAt'ten tekrar 1 yıl hesapla
);