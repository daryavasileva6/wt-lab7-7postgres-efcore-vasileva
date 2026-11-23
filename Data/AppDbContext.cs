using Microsoft.EntityFrameworkCore;
using wt_lab7_7postgres_efcore_vasileva.Models;

namespace wt_lab7_7postgres_efcore_vasileva.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Начальные данные (Блок 23)
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Ноутбук", Category = "Компьютеры", Price = 2500, IsAvailable = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Product { Id = 2, Name = "Мышь", Category = "Аксессуары", Price = 80, IsAvailable = true, CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new Product { Id = 3, Name = "Клавиатура", Category = "Аксессуары", Price = 150, IsAvailable = true, CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}