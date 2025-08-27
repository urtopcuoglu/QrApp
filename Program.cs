using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QrApp.Data;
using QrApp.Dtos;
using QrApp.Models;

var builder = WebApplication.CreateBuilder(args);

// SQL Server (bağlantı dizesini appsettings.json’dan al)
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Default")
             ?? "Server=.\\SQLEXPRESS;Database=QrAppDb;Trusted_Connection=True;TrustServerCertificate=True;";
    opt.UseSqlServer(cs);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Startup’ta migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basit admin anahtarı
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/admin"))
    {
        var need = app.Configuration["Admin:ApiKey"];
        var got = ctx.Request.Headers["X-API-KEY"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(need) || got != need)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Unauthorized");
            return;
        }
    }
    await next();
});

// --- Public ---
app.MapGet("/q/{shortCode}", async ([FromRoute] string shortCode, AppDbContext db) =>
{
    var entry = await db.QRCodes.FirstOrDefaultAsync(x => x.ShortCode == shortCode);
    if (entry is null) return Results.NotFound("QR bulunamadı.");

    var now = DateTime.UtcNow;
    if (!entry.Active || (entry.ExpireAt.HasValue && entry.ExpireAt.Value < now))
        return Results.NotFound("QR aktif değil veya süresi dolmuş.");

    entry.ScanCount++;
    await db.SaveChangesAsync();
    return Results.Redirect(entry.TargetUrl);
});

app.MapGet("/q/{shortCode}/png", async ([FromRoute] string shortCode, AppDbContext db, HttpContext http) =>
{
    var entry = await db.QRCodes.AsNoTracking().FirstOrDefaultAsync(x => x.ShortCode == shortCode);
    if (entry is null) return Results.NotFound();

    var content = entry.TargetUrl; // <-- değişiklik

    using var gen = new QRCodeGenerator();
    using var data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
    var pngQr = new PngByteQRCode(data);
    var png = pngQr.GetGraphic(10);

    return Results.File(png, "image/png");
});


// --- Admin ---
app.MapPost("/admin/qrcodes", async ([FromBody] CreateQrDto dto, AppDbContext db) =>
{
    if (!Uri.IsWellFormedUriString(dto.TargetUrl, UriKind.Absolute))
        return Results.BadRequest("TargetUrl mutlak bir URL olmalı.");

    var code = string.IsNullOrWhiteSpace(dto.ShortCode) ? GenerateShortCode() : dto.ShortCode!.Trim();

    if (await db.QRCodes.AnyAsync(x => x.ShortCode == code))
        return Results.Conflict("ShortCode zaten kullanılıyor.");

    var entry = new QRCodeEntry
    {
        Name = string.IsNullOrWhiteSpace(dto.Name) ? "Untitled" : dto.Name!.Trim(),
        ShortCode = code,
        TargetUrl = dto.TargetUrl.Trim(),
        CreatedAt = DateTime.UtcNow,
        Active = dto.Active,
        ExpireAt = dto.OneYear ? DateTime.UtcNow.AddYears(1) : (DateTime?)null
    };

    db.QRCodes.Add(entry);
    await db.SaveChangesAsync();
    return Results.Created($"/admin/qrcodes/{entry.Id}", entry);
});

app.MapGet("/admin/qrcodes", async ([FromQuery] int page, [FromQuery] int pageSize, AppDbContext db) =>
{
    var p = page <= 0 ? 1 : page;
    var ps = pageSize <= 0 || pageSize > 100 ? 20 : pageSize;

    var query = db.QRCodes.OrderByDescending(x => x.Id);
    var total = await query.CountAsync();
    var items = await query.Skip((p - 1) * ps).Take(ps).ToListAsync();

    return Results.Ok(new { total, page = p, pageSize = ps, items });
});

app.MapGet("/admin/qrcodes/{id:int}", async (int id, AppDbContext db) =>
{
    var entry = await db.QRCodes.FindAsync(id);
    return entry is null ? Results.NotFound() : Results.Ok(entry);
});

app.MapPut("/admin/qrcodes/{id:int}", async (int id, [FromBody] UpdateQrDto dto, AppDbContext db) =>
{
    var entry = await db.QRCodes.FindAsync(id);
    if (entry is null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(dto.Name)) entry.Name = dto.Name!.Trim();

    if (dto.TargetUrl is not null)
    {
        if (!Uri.IsWellFormedUriString(dto.TargetUrl, UriKind.Absolute))
            return Results.BadRequest("TargetUrl mutlak bir URL olmalı.");
        entry.TargetUrl = dto.TargetUrl;
    }

    if (dto.Active.HasValue) entry.Active = dto.Active.Value;

    if (dto.OneYear.HasValue)
    {
        if (dto.OneYear.Value)
            entry.ExpireAt = dto.ResetOneYear == true ? entry.CreatedAt.AddYears(1) : (entry.ExpireAt ?? entry.CreatedAt.AddYears(1));
        else
            entry.ExpireAt = null;
    }
    else if (dto.ResetOneYear == true)
    {
        entry.ExpireAt = entry.CreatedAt.AddYears(1);
    }

    await db.SaveChangesAsync();
    return Results.Ok(entry);
});

app.MapPost("/admin/qrcodes/{id:int}/rotate-code", async (int id, AppDbContext db) =>
{
    var entry = await db.QRCodes.FindAsync(id);
    if (entry is null) return Results.NotFound();

    string newCode;
    do { newCode = GenerateShortCode(); }
    while (await db.QRCodes.AnyAsync(x => x.ShortCode == newCode));

    entry.ShortCode = newCode;
    await db.SaveChangesAsync();
    return Results.Ok(new { entry.Id, entry.ShortCode });
});

app.MapDelete("/admin/qrcodes/{id:int}", async (int id, AppDbContext db) =>
{
    var entry = await db.QRCodes.FindAsync(id);
    if (entry is null) return Results.NotFound();

    db.QRCodes.Remove(entry);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/admin.html"));
app.Run();

static string GenerateShortCode(int len = 6)
{
    const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    Span<byte> buf = stackalloc byte[len];
    RandomNumberGenerator.Fill(buf);
    var sb = new StringBuilder(len);
    for (int i = 0; i < len; i++)
        sb.Append(alphabet[buf[i] % alphabet.Length]);
    return sb.ToString();
}
