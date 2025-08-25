using Microsoft.EntityFrameworkCore;
using QrApp.Models;

namespace QrApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<QRCodeEntry> QRCodes => Set<QRCodeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QRCodeEntry>()
            .HasIndex(x => x.ShortCode)
            .IsUnique();

        modelBuilder.Entity<QRCodeEntry>()
            .Property(x => x.TargetUrl)
            .IsRequired();

        base.OnModelCreating(modelBuilder);
    }
}