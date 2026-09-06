using MediCoreSupply.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Data;

public class MediCoreSupplyDbContext : DbContext
{
    public MediCoreSupplyDbContext(DbContextOptions<MediCoreSupplyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Sku).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(32).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Sku).IsUnique();

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(400).IsRequired();
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();

            entity.HasOne(e => e.Product)
                .WithMany(p => p.InventoryItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.InventoryItems)
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(400).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.OrderNumber).HasMaxLength(32).IsRequired();
            entity.Property(e => e.ShippingAddress).HasMaxLength(400).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(e => e.OrderNumber).IsUnique();

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
