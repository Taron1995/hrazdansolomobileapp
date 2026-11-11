using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.MissedCalls;

[Authorize(Roles = Roles.CashDesk)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public IndexModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public List<MissedCall> Open { get; set; } = new();
    public List<MissedCall> Done { get; set; } = new();
    public Dictionary<string, string> CustomerNames { get; set; } = new();
    public Dictionary<string, int> Repeats { get; set; } = new();
    public int TodayCount { get; set; }
    public int WeekCount { get; set; }

    public async Task OnGetAsync()
    {
        var today = _clock.Today;
        Open = await _db.MissedCalls.Where(c => !c.CalledBack).OrderByDescending(c => c.CalledAt).ToListAsync();
        Done = await _db.MissedCalls.Include(c => c.HandledBy).Where(c => c.CalledBack).OrderByDescending(c => c.CalledBackAt).Take(50).ToListAsync();
        TodayCount = await _db.MissedCalls.CountAsync(c => c.CalledAt >= today);
        WeekCount = await _db.MissedCalls.CountAsync(c => c.CalledAt >= today.AddDays(-6));
        Repeats = Open.GroupBy(c => c.Phone).ToDictionary(g => g.Key, g => g.Count());
        var phones = Repeats.Keys.ToList();
        CustomerNames = await _db.Customers.Where(c => phones.Contains(c.Phone)).ToDictionaryAsync(c => c.Phone, c => c.Name);
        // show one row per phone
        Open = Open.GroupBy(c => c.Phone).Select(g => g.First()).ToList();
    }

    public async Task<IActionResult> OnPostAddAsync(string phone)
    {
        var p = Phone.Normalize(phone);
        if (p.Length >= 8)
        {
            _db.MissedCalls.Add(new MissedCall { Phone = p, CalledAt = _clock.Now, Source = "manual" });
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    /// <summary>Marks every open missed call from the same number as called back.</summary>
    public async Task<IActionResult> OnPostDoneAsync(int id, string? notes)
    {
        var c = await _db.MissedCalls.FindAsync(id);
        if (c == null) return NotFound();
        var all = await _db.MissedCalls.Where(x => x.Phone == c.Phone && !x.CalledBack).ToListAsync();
        foreach (var x in all)
        {
            x.CalledBack = true; x.CalledBackAt = _clock.Now; x.HandledById = User.EmployeeId(); x.Notes = notes?.Trim();
        }
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
