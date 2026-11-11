using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Customers;

[Authorize(Roles = Roles.CashDesk)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public IndexModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public record DistrictStat(string District, int Orders, int Customers);

    public string? Q { get; set; }
    public string? District { get; set; }
    public List<Customer> Customers { get; set; } = new();
    public List<DistrictStat> DistrictStats { get; set; } = new();

    public async Task OnGetAsync(string? q, string? district)
    {
        Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        District = string.IsNullOrWhiteSpace(district) ? null : district;
        var query = _db.Customers.Include(c => c.Orders).Include(c => c.Issues).AsQueryable();
        if (Q != null)
        {
            var p = Phone.Normalize(Q);
            query = query.Where(c => c.Name.Contains(Q) || (c.Address != null && c.Address.Contains(Q)) || (p.Length > 3 && c.Phone.Contains(p)));
        }
        if (District != null) query = query.Where(c => c.District == District);
        Customers = await query.OrderBy(c => c.Name).Take(300).ToListAsync();

        var since = _clock.Today.AddDays(-90);
        var orders = await _db.Orders.Where(o => o.CreatedAt >= since)
            .Select(o => new { D = o.DeliveryDistrict ?? o.Customer!.District, o.CustomerId }).ToListAsync();
        DistrictStats = orders.GroupBy(o => string.IsNullOrWhiteSpace(o.D) ? "Նշված չէ" : o.D!)
            .Select(g => new DistrictStat(g.Key, g.Count(), g.Select(x => x.CustomerId).Distinct().Count()))
            .OrderByDescending(d => d.Orders).ToList();
    }
}
