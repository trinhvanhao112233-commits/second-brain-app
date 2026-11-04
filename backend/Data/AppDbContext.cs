using PersonalFinance.API.Security;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.API.Models;

namespace PersonalFinance.API.Data
{
    public class AppDbContext : DbContext
    {
        private readonly CurrentUser _user;
        public AppDbContext(DbContextOptions<AppDbContext> options, CurrentUser user) : base(options) { _user = user; }

        public DbSet<ReminderDelivery> ReminderDeliveries => Set<ReminderDelivery>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<CalendarEvent> Events => Set<CalendarEvent>();
        public DbSet<DailyTask> Tasks => Set<DailyTask>();
        public DbSet<Note> Notes => Set<Note>();
        public DbSet<PushSubscriptionModel> PushSubscriptions => Set<PushSubscriptionModel>();
        public DbSet<DebtRecord> DebtRecords => Set<DebtRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ReminderDelivery>().Property(x => x.Id).HasMaxLength(64);
            modelBuilder.Entity<Wallet>().HasQueryFilter(x => x.UserId == _user.Id);
            modelBuilder.Entity<Transaction>().HasQueryFilter(x => x.Wallet != null && x.Wallet.UserId == _user.Id);
            modelBuilder.Entity<CalendarEvent>().HasQueryFilter(x => x.UserId == _user.Id);
            modelBuilder.Entity<DailyTask>().HasQueryFilter(x => x.UserId == _user.Id);
            modelBuilder.Entity<Note>().HasQueryFilter(x => x.UserId == _user.Id);
            modelBuilder.Entity<DebtRecord>().HasQueryFilter(x => x.UserId == _user.Id);
            modelBuilder.Entity<PushSubscriptionModel>().HasQueryFilter(x => x.UserId == _user.Id);

            // TiDB Cloud yêu cầu utf8mb4_general_ci
            modelBuilder.UseCollation("utf8mb4_general_ci");

            // Cấu hình User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.FullName).HasMaxLength(100);
            });

            // Cấu hình Wallet
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
                entity.Property(w => w.MonthlyBudget).HasPrecision(18, 2);
                entity.Property(w => w.IsMaster).HasDefaultValue(false);
            });

            // Cấu hình Transaction & Quan hệ 1 - N
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(t => t.Id);
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
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Color).HasMaxLength(20);
            });

            // Cấu hình DailyTask
            modelBuilder.Entity<DailyTask>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Priority).HasMaxLength(20);
            });

            // Cấu hình Note
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
                entity.Property(n => n.Color).HasMaxLength(20);
            });

            // Cấu hình PushSubscriptionModel
            modelBuilder.Entity<PushSubscriptionModel>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Endpoint).IsRequired().HasMaxLength(1000);
                entity.Property(s => s.P256dh).IsRequired().HasMaxLength(200);
                entity.Property(s => s.Auth).IsRequired().HasMaxLength(100);
            });

            // Cấu hình DebtRecord
            modelBuilder.Entity<DebtRecord>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Title).IsRequired().HasMaxLength(200);
                entity.Property(d => d.RecordType).IsRequired().HasMaxLength(30);
                entity.Property(d => d.TotalAmount).HasPrecision(18, 2);
                entity.Property(d => d.PaidAmount).HasPrecision(18, 2);
                entity.Property(d => d.BreakdownNote).HasMaxLength(500);
                entity.Property(d => d.PeriodLabel).HasMaxLength(50);
                entity.Property(d => d.CategoryTag).HasMaxLength(100);
            });

            // MySqlConnector exposes CHAR(36) UUID columns as Guid. Mapping these
            // properties through varchar adds a Guid-to-string converter and makes
            // EF call GetString on a Guid value when a row is materialized.
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var property in entity.GetProperties())
                    if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(Guid))
                        property.SetColumnType("char(36)");
        }
    }
}
