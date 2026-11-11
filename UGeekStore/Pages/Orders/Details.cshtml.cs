using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    private readonly WorkloadService _workload;
    public DetailsModel(AppDbContext db, Clock clock, WorkloadService workload) { _db = db; _clock = clock; _workload = workload; }

    public Order Order { get; set; } = null!;
    public List<CustomerIssue> Issues { get; set; } = new();
    public List<ExecutorLoad> Executors { get; set; } = new();
    public List<OrderStatus> NextStatuses { get; set; } = new();

    private async Task<bool> LoadAsync(int id)
    {
        var o = await _db.Orders.Include(x => x.Customer).Include(x => x.Items).Include(x => x.Executor)
            .Include(x => x.Courier).Include(x => x.CreatedBy).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return false;
        Order = o;
        Issues = await _db.CustomerIssues.Where(i => i.CustomerId == o.CustomerId).OrderByDescending(i => i.CreatedAt).ToListAsync();
        Executors = await _workload.ForDayAsync(o.DueAt.Date);
        NextStatuses = Enum.GetValues<OrderStatus>().Where(s => s != o.Status && Allowed(s)).ToList();
        return true;
    }

    private bool Allowed(OrderStatus s)
    {
        if (User.IsAny(Roles.CashDesk)) return true;
        if (User.IsInRole(Roles.Confectioner)) return s is OrderStatus.InProduction or OrderStatus.Ready;
        if (User.IsInRole(Roles.Courier)) return s is OrderStatus.OutForDelivery or OrderStatus.Completed;
        return false;
    }

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostStatusAsync(int id, OrderStatus status)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (!Allowed(status)) return Forbid();
        Order.Status = status;
        await _db.SaveChangesAsync();
        TempData["ok"] = $"Կարգավիճակ՝ {status.Hy()}";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostExecutorAsync(int id, int? executorId)
    {
        if (!User.IsAny(Roles.CashDesk)) return Forbid();
        var o = await _db.Orders.FindAsync(id);
        if (o == null) return NotFound();
        o.ExecutorId = executorId;
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostIssueAsync(int id, string description)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(description))
        {
            _db.CustomerIssues.Add(new CustomerIssue
            {
                CustomerId = o.CustomerId, OrderId = o.Id, CreatedAt = _clock.Now,
                Description = description.Trim(), CreatedById = User.EmployeeId(),
            });
            await _db.SaveChangesAsync();
            TempData["ok"] = "Խնդիրը ֆիքսված է հաճախորդի քարտում";
        }
        return RedirectToPage(new { id });
    }
}
