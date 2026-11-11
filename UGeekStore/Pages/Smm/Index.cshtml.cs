using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Smm;

[Authorize(Roles = Roles.Management)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public IndexModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public static readonly string[] Platforms = { "Instagram", "Facebook", "TikTok", "Telegram", "WhatsApp", "YouTube" };

    public DateTime Month { get; set; }
    public List<SmmPost> Posts { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();

    private static DateTime ParseMonth(string? m, DateTime fallback) =>
        DateTime.TryParseExact(m, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : new DateTime(fallback.Year, fallback.Month, 1);

    public async Task OnGetAsync(string? month)
    {
        Month = ParseMonth(month, _clock.Today);
        Posts = await _db.SmmPosts.Include(p => p.Responsible)
            .Where(p => p.PlannedDate >= Month && p.PlannedDate < Month.AddMonths(1)).OrderBy(p => p.PlannedDate).ToListAsync();
        Employees = await _db.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(DateTime plannedDate, string platform, string? contentType, string topic, string? caption, int? responsibleId)
    {
        _db.SmmPosts.Add(new SmmPost
        {
            PlannedDate = plannedDate.Date, Platform = platform, ContentType = contentType, Topic = topic.Trim(),
            Caption = caption?.Trim(), ResponsibleId = responsibleId, Status = SmmStatus.Planned,
        });
        await _db.SaveChangesAsync();
        return Redirect($"/Smm?month={plannedDate:yyyy-MM}");
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, SmmStatus status, bool delete, string? month)
    {
        var p = await _db.SmmPosts.FindAsync(id);
        if (p != null)
        {
            if (delete) _db.SmmPosts.Remove(p); else p.Status = status;
            await _db.SaveChangesAsync();
        }
        return Redirect($"/Smm?month={month}");
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile csv)
    {
        var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "M/d/yyyy" };
        int n = 0;
        foreach (var r in await Csv.ReadAsync(csv))
        {
            if (r.Length < 3 || !DateTime.TryParseExact(r[0].Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;
            _db.SmmPosts.Add(new SmmPost
            {
                PlannedDate = date, Platform = string.IsNullOrWhiteSpace(r[1]) ? "Instagram" : r[1].Trim(), Topic = r[2].Trim(),
                ContentType = r.Length > 3 ? r[3].Trim() : null, Caption = r.Length > 4 ? r[4].Trim() : null,
                Status = r.Length > 5 && Enum.TryParse<SmmStatus>(r[5].Trim(), true, out var s) ? s : SmmStatus.Planned,
            });
            n++;
        }
        await _db.SaveChangesAsync();
        TempData["ok"] = $"Ներմուծված է {n} գրառում";
        return RedirectToPage();
    }
}
