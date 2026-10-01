using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
        public DbSet<DailyTask> Tasks => Set<DailyTask>();
        public DbSet<Note> Notes => Set<Note>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TiDB Cloud yêu cầu utf8mb4_general_ci
            modelBuilder.UseCollation("utf8mb4_general_ci");

            // Cấu hình User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasColumnType("varchar(36)");
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.FullName).HasMaxLength(100);
            });

            // Cấu hình Wallet
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Id).HasColumnType("varchar(36)");
                entity.Property(w => w.UserId).HasColumnType("varchar(36)");
                entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
                entity.Property(w => w.MonthlyBudget).HasPrecision(18, 2);
            });

            // Cấu hình Transaction & Quan hệ 1 - N
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).HasColumnType("varchar(36)");
                entity.Property(t => t.WalletId).HasColumnType("varchar(36)");
                entity.Property(t => t.Amount).HasPrecision(18, 2);
                entity.Property(t => t.Category).HasMaxLength(100);
                entity.Property(t => t.Note).HasMaxLength(500);

                entity.HasOne(t => t.Wallet)
                      .WithMany(w => w.Transactions)
                      .HasForeignKey(t => t.WalletId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình CalendarEvent
            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnType("varchar(36)");
                entity.Property(e => e.UserId).HasColumnType("varchar(36)");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
            });

            // Cấu hình DailyTask
            modelBuilder.Entity<DailyTask>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).HasColumnType("varchar(36)");
                entity.Property(t => t.UserId).HasColumnType("varchar(36)");
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Priority).HasMaxLength(20);
            });

            // Cấu hình Note
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Id).HasColumnType("varchar(36)");
                entity.Property(n => n.UserId).HasColumnType("varchar(36)");
                entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
                entity.Property(n => n.Color).HasMaxLength(20);
            });
        }
    }
}
