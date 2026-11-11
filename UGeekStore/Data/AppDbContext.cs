using Microsoft.EntityFrameworkCore;
using UGeekStore.Core.Entities;

namespace UGeekStore.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashShift> CashShifts => Set<CashShift>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerIssue> CustomerIssues => Set<CustomerIssue>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<SmmPost> SmmPosts => Set<SmmPost>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<ChecklistEntry> ChecklistEntries => Set<ChecklistEntry>();
    public DbSet<MissedCall> MissedCalls => Set<MissedCall>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // SQLite cannot SUM/ORDER BY decimal columns; store money as REAL.
        b.Properties<decimal>().HaveConversion<double>();
        b.Properties<decimal?>().HaveConversion<double?>();
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Employee>().HasIndex(e => e.Email).IsUnique();
        m.Entity<Customer>().HasIndex(c => c.Phone).IsUnique();
        m.Entity<CashShift>().HasIndex(s => new { s.CashRegisterId, s.Date });

        m.Entity<CashShift>().HasOne(s => s.OpenedBy).WithMany().HasForeignKey(s => s.OpenedById).OnDelete(DeleteBehavior.Restrict);
        m.Entity<CashShift>().HasOne(s => s.ClosedBy).WithMany().HasForeignKey(s => s.ClosedById).OnDelete(DeleteBehavior.Restrict);

        m.Entity<Order>().HasOne(o => o.CreatedBy).WithMany().HasForeignKey(o => o.CreatedById).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Order>().HasOne(o => o.Executor).WithMany().HasForeignKey(o => o.ExecutorId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Order>().HasOne(o => o.Courier).WithMany().HasForeignKey(o => o.CourierId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Order>().HasIndex(o => o.DueAt);

        m.Entity<ChecklistEntry>().HasIndex(e => new { e.ChecklistItemId, e.Date });
        m.Entity<MissedCall>().HasIndex(c => c.Phone);
    }
}
