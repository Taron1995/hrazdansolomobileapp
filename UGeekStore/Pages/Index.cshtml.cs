using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public IndexModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public DateTime Today { get; set; }
    public string RoleName { get; set; } = "";
    public bool ShiftOpen { get; set; }
    public int OrdersToday { get; set; }
    public int OrdersOpen { get; set; }
    public int PurchasesOpen { get; set; }
    public int MissedOpen { get; set; }
    public int ChecklistLeft { get; set; }

    public async Task OnGetAsync()
    {
        Today = _clock.Today;
        var role = Enum.TryParse<UserRole>(User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value, out var r) ? r : UserRole.Cashier;
        RoleName = role.Hy();
        ShiftOpen = await _db.CashShifts.AnyAsync(s => s.Status == ShiftStatus.Open);
        var tomorrow = Today.AddDays(1);
        var orders = _db.Orders.AsQueryable();
        if (role == UserRole.Confectioner) { var me = User.EmployeeId(); orders = orders.Where(o => o.ExecutorId == me); }
        OrdersToday = await orders.CountAsync(o => o.DueAt >= Today && o.DueAt < tomorrow && o.Status != OrderStatus.Cancelled);
        OrdersOpen = await orders.CountAsync(o => o.Status == OrderStatus.New || o.Status == OrderStatus.InProduction || o.Status == OrderStatus.Ready);
        PurchasesOpen = await _db.PurchaseRequests.CountAsync(p => p.Status == PurchaseStatus.Requested || p.Status == PurchaseStatus.Approved);
        MissedOpen = await _db.MissedCalls.CountAsync(c => !c.CalledBack);
        ChecklistLeft = (await ChecklistLogic.TodayAsync(_db, role, Today)).Count(x => x.Entry == null);
    }
}
