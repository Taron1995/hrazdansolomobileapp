using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Data;

namespace UGeekStore.Services;

public record ExecutorLoad(int Id, string Name, int ActiveOrders, int OrdersOnDay, decimal ItemsOnDay)
{
    public string Label => $"{Name} — ակտիվ՝ {ActiveOrders}, այդ օրը՝ {OrdersOnDay} պատվեր / {ItemsOnDay:0.#} հատ";
    /// <summary>0 = free, 1 = normal, 2 = busy, 3 = overloaded.</summary>
    public int Level => OrdersOnDay switch { 0 => 0, <= 3 => 1, <= 6 => 2, _ => 3 };
}

/// <summary>How many open orders each confectioner has — shown when choosing who will make an order.</summary>
public class WorkloadService
{
    private readonly AppDbContext _db;
    public WorkloadService(AppDbContext db) => _db = db;

    private static readonly OrderStatus[] Open = { OrderStatus.New, OrderStatus.InProduction };

    public async Task<List<ExecutorLoad>> ForDayAsync(DateTime day)
    {
        var start = day.Date; var end = start.AddDays(1);
        var execs = await _db.Employees.Where(e => e.IsActive && e.Role == UserRole.Confectioner)
            .OrderBy(e => e.FullName).Select(e => new { e.Id, e.FullName }).ToListAsync();
        var open = await _db.Orders.Where(o => o.ExecutorId != null && Open.Contains(o.Status))
            .Select(o => new { o.ExecutorId, o.DueAt, Qty = o.Items.Sum(i => (double)i.Quantity) })
            .ToListAsync();
        return execs.Select(e =>
        {
            var mine = open.Where(o => o.ExecutorId == e.Id).ToList();
            var day = mine.Where(o => o.DueAt >= start && o.DueAt < end).ToList();
            return new ExecutorLoad(e.Id, e.FullName, mine.Count, day.Count, (decimal)day.Sum(o => o.Qty));
        }).ToList();
    }
}
