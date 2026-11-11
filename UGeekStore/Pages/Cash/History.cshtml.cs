using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Cash;

[Authorize(Roles = Roles.Management)]
public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public HistoryModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<CashShift> Shifts { get; set; } = new();

    public async Task OnGetAsync(DateTime? from, DateTime? to)
    {
        To = (to ?? _clock.Today).Date;
        From = (from ?? To.AddDays(-14)).Date;
        Shifts = await _db.CashShifts.Include(s => s.Transactions).Include(s => s.CashRegister).Include(s => s.ClosedBy)
            .Where(s => s.Date >= From && s.Date <= To).OrderByDescending(s => s.Date).ThenBy(s => s.CashRegisterId).ToListAsync();
    }

    public async Task<IActionResult> OnPostVoidAsync(int id, string? reason)
    {
        var tx = await _db.CashTransactions.FindAsync(id);
        if (tx == null) return NotFound();
        tx.IsVoided = true;
        tx.VoidReason = $"{User.FullName()}: {reason}";
        await _db.SaveChangesAsync();
        TempData["ok"] = "Գործարքը չեղարկված է";
        return RedirectToPage();
    }
}
