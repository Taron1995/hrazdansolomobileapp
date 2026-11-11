using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Production;

[Authorize(Roles = Roles.Production)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public IndexModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public DateTime Date { get; set; }
    public List<DateTime> Days { get; set; } = new();
    public Dictionary<string, Dictionary<DateTime, int>> Matrix { get; set; } = new();
    public List<IGrouping<string, Order>> Groups { get; set; } = new();
    public List<(string Name, decimal Qty)> Totals { get; set; } = new();

    public async Task OnGetAsync(DateTime? date)
    {
        Date = (date ?? _clock.Today).Date;
        var orders = await _db.Orders.Include(o => o.Items).Include(o => o.Customer).Include(o => o.Executor)
            .Where(o => o.DueAt >= Date && o.DueAt < Date.AddDays(1) && o.Status != OrderStatus.Cancelled)
            .ToListAsync();
        Groups = orders.GroupBy(o => o.Executor?.FullName ?? "Կատարող նշանակված չէ").OrderBy(g => g.Key).ToList();
        Totals = orders.Where(o => o.Status is OrderStatus.New or OrderStatus.InProduction)
            .SelectMany(o => o.Items).GroupBy(i => i.Name).Select(g => (g.Key, g.Sum(i => i.Quantity)))
            .OrderByDescending(x => x.Item2).ToList();

        var start = _clock.Today;
        Days = Enumerable.Range(0, 7).Select(i => start.AddDays(i)).ToList();
        var week = await _db.Orders.Include(o => o.Executor)
            .Where(o => o.DueAt >= start && o.DueAt < start.AddDays(7) && o.Status != OrderStatus.Cancelled)
            .Select(o => new { Name = o.Executor != null ? o.Executor.FullName : "Չնշանակված", Day = o.DueAt.Date })
            .ToListAsync();
        var execs = await _db.Employees.Where(e => e.IsActive && e.Role == UserRole.Confectioner).Select(e => e.FullName).ToListAsync();
        foreach (var e in execs) Matrix[e] = new();
        foreach (var w in week)
        {
            if (!Matrix.TryGetValue(w.Name, out var row)) Matrix[w.Name] = row = new();
            row[w.Day] = row.GetValueOrDefault(w.Day) + 1;
        }
    }
}
