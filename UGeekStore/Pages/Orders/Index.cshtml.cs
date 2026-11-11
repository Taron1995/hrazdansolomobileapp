using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public IndexModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public string Scope { get; set; } = "today";
    public DateTime Date { get; set; }
    public string? Q { get; set; }
    public List<Order> Orders { get; set; } = new();

    public static string Badge(OrderStatus s) => s switch
    {
        OrderStatus.New => "bg-primary",
        OrderStatus.InProduction => "bg-warning text-dark",
        OrderStatus.Ready => "bg-success",
        OrderStatus.OutForDelivery => "bg-info text-dark",
        OrderStatus.Completed => "bg-secondary",
        _ => "bg-light text-dark border",
    };

    public async Task OnGetAsync(string? scope, DateTime? date, string? q)
    {
        Scope = scope ?? (User.IsInRole(Roles.Confectioner) ? "mine" : "today");
        Date = (date ?? _clock.Today).Date;
        Q = q;
        var query = _db.Orders.Include(o => o.Customer).Include(o => o.Executor).Include(o => o.Items).AsQueryable();
        var today = _clock.Today;
        query = Scope switch
        {
            "today" => query.Where(o => o.DueAt >= today && o.DueAt < today.AddDays(1)),
            "tomorrow" => query.Where(o => o.DueAt >= today.AddDays(1) && o.DueAt < today.AddDays(2)),
            "open" => query.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled),
            "mine" => query.Where(o => (o.ExecutorId == User.EmployeeId() || o.CourierId == User.EmployeeId())
                                       && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled),
            "date" => query.Where(o => o.DueAt >= Date && o.DueAt < Date.AddDays(1)),
            _ => query,
        };
        if (!string.IsNullOrWhiteSpace(q))
        {
            var p = Phone.Normalize(q);
            if (int.TryParse(q.TrimStart('#'), out var id) && q.Trim().Length < 7) query = query.Where(o => o.Id == id);
            else query = query.Where(o => o.Customer!.Name.Contains(q) || (p.Length > 3 && o.Customer!.Phone.Contains(p)));
        }
        Orders = await query.OrderBy(o => o.DueAt).Take(300).ToListAsync();
    }
}
