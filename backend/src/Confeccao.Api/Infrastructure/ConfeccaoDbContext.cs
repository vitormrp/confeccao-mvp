using Confeccao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Confeccao.Api.Infrastructure;

public class ConfeccaoDbContext : DbContext
{
    public ConfeccaoDbContext(DbContextOptions<ConfeccaoDbContext> options) : base(options)
    {
    }

    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Model> Models => Set<Model>();
    public DbSet<ModelStage> ModelStages => Set<ModelStage>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Price> Prices => Set<Price>();
    public DbSet<PriceTier> PriceTiers => Set<PriceTier>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PipelineItem> PipelineItems => Set<PipelineItem>();
    public DbSet<PipelineEvent> PipelineEvents => Set<PipelineEvent>();
    public DbSet<LaundryPackage> LaundryPackages => Set<LaundryPackage>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MiscCost> MiscCosts => Set<MiscCost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Color>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
            b.Property(c => c.HexCode).HasMaxLength(9).IsRequired();
            b.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
            b.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Model>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Name).HasMaxLength(100).IsRequired();
            b.Property(m => m.CreatedAt).HasDefaultValueSql("now()");
            b.HasIndex(m => m.Name).IsUnique();

            b.HasMany(m => m.Stages)
                .WithOne(s => s.Model)
                .HasForeignKey(s => s.ModelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModelStage>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Stage).HasConversion<string>().HasMaxLength(20);
            b.HasIndex(s => new { s.ModelId, s.Sequence }).IsUnique();
            b.HasIndex(s => new { s.ModelId, s.Stage }).IsUnique();
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Name).HasMaxLength(100).IsRequired();
            b.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            b.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            b.HasIndex(u => u.Role);
        });

        modelBuilder.Entity<Price>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Amount).HasPrecision(12, 4);
            b.Property(p => p.LiningExtra).HasPrecision(12, 4);
            b.Property(p => p.InterfacingExtra).HasPrecision(12, 4);
            b.Property(p => p.CoveredButtonPrice).HasPrecision(12, 4);
            b.Property(p => p.ReadyButtonPrice).HasPrecision(12, 4);
            b.Property(p => p.Note).HasMaxLength(500);

            b.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.Tiers)
                .WithOne(t => t.Price)
                .HasForeignKey(t => t.PriceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(p => new { p.UserId, p.EffectiveFrom });
        });

        modelBuilder.Entity<PriceTier>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Amount).HasPrecision(12, 4);
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.Number).ValueGeneratedOnAdd().UseSequence("order_number_seq");
            b.Property(o => o.FabricCode).HasMaxLength(100).IsRequired();
            b.Property(o => o.Instructions).HasMaxLength(1000);
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(o => o.CreatedAt).HasDefaultValueSql("now()");

            b.HasOne(o => o.Color)
                .WithMany()
                .HasForeignKey(o => o.ColorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(o => o.Status);
            b.HasIndex(o => o.Number).IsUnique();
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Size).HasConversion<string>().HasMaxLength(2);

            b.HasOne(i => i.Model)
                .WithMany()
                .HasForeignKey(i => i.ModelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(i => new { i.OrderId, i.ModelId, i.Size }).IsUnique();
        });

        modelBuilder.Entity<PipelineItem>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Size).HasConversion<string>().HasMaxLength(2);
            b.Property(p => p.Stage).HasConversion<string>().HasMaxLength(20);
            b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(p => p.ColorNameSnapshot).HasMaxLength(100);
            b.Property(p => p.FabricCodeSnapshot).HasMaxLength(100);
            b.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

            b.HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.OrderItem)
                .WithMany()
                .HasForeignKey(p => p.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.Model)
                .WithMany()
                .HasForeignKey(p => p.ModelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.AssignedUser)
                .WithMany()
                .HasForeignKey(p => p.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(p => p.LaundryPackage)
                .WithMany()
                .HasForeignKey(p => p.LaundryPackageId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(p => new { p.Stage, p.Status });
            b.HasIndex(p => p.OrderId);
            b.HasIndex(p => p.AssignedUserId);
            b.HasIndex(p => p.LaundryPackageId);
        });

        modelBuilder.Entity<LaundryPackage>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Number).ValueGeneratedOnAdd().UseSequence("laundry_package_number_seq");
            b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(p => p.SentAt).HasDefaultValueSql("now()");

            b.HasOne(p => p.CompletedByUser)
                .WithMany()
                .HasForeignKey(p => p.CompletedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(p => p.Number).IsUnique();
            b.HasIndex(p => p.Status);
        });

        modelBuilder.Entity<Credit>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Amount).HasPrecision(12, 4);
            b.Property(c => c.Stage).HasConversion<string>().HasMaxLength(20);
            b.Property(c => c.Size).HasConversion<string>().HasMaxLength(2);

            b.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.Order)
                .WithMany()
                .HasForeignKey(c => c.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(c => c.PipelineItem)
                .WithMany()
                .HasForeignKey(c => c.PipelineItemId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(c => c.Model)
                .WithMany()
                .HasForeignKey(c => c.ModelId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.Payment)
                .WithMany(p => p.Credits)
                .HasForeignKey(c => c.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(c => new { c.UserId, c.PaymentId });
            b.HasIndex(c => c.OccurredAt);
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Number).ValueGeneratedOnAdd().UseSequence("payment_number_seq");
            b.Property(p => p.Amount).HasPrecision(12, 4);
            b.Property(p => p.Note).HasMaxLength(500);

            b.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(p => p.UserId);
            b.HasIndex(p => p.Number).IsUnique();
        });

        modelBuilder.Entity<MiscCost>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Description).HasMaxLength(200).IsRequired();
            b.Property(m => m.Category).HasMaxLength(50);
            b.Property(m => m.Amount).HasPrecision(12, 4);
            b.Property(m => m.CreatedAt).HasDefaultValueSql("now()");
            b.HasIndex(m => m.Date);
        });

        modelBuilder.Entity<PipelineEvent>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            b.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
            b.Property(e => e.OccurredAt).HasDefaultValueSql("now()");

            b.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(e => e.PipelineItem)
                .WithMany()
                .HasForeignKey(e => e.PipelineItemId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(e => e.OccurredAt);
            b.HasIndex(e => e.EventType);
            b.HasIndex(e => e.OrderId);
        });
    }
}
