using Microsoft.EntityFrameworkCore;
using POC_Facturation.Domain;

namespace POC_Facturation.Data;

internal class InvoiceDbContext : DbContext
{
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; } = null!;
    public DbSet<DogDetail> DogDetails { get; set; } = null!;

    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration EF Core SQLite
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired();
            entity.Property(e => e.TotalHT).HasConversion<double>();
            entity.Property(e => e.TotalTVA).HasConversion<double>();
            entity.Property(e => e.TotalTTC).HasConversion<double>();
            
            entity.HasMany(e => e.LineItems)
                  .WithOne()
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPriceHT).HasConversion<double>();
            entity.Property(e => e.TvaRate).HasConversion<double>();

            entity.HasOne(e => e.DogDetail)
                  .WithMany()
                  .HasForeignKey(e => e.DogDetailId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DogDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IcadNumber).IsRequired().HasMaxLength(15);
            entity.Property(e => e.DogSex).HasConversion<string>(); // Stockage sous forme de chaîne de caractères
        });
    }
}
