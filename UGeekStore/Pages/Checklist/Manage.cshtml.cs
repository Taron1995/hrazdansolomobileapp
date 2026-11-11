using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Checklist;

[Authorize(Roles = Roles.Management)]
public class ManageModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public ManageModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public DateTime Date { get; set; }
    public List<ChecklistItem> Items { get; set; } = new();
    public List<ChecklistEntry> Entries { get; set; } = new();

    public async Task OnGetAsync(DateTime? date)
    {
        Date = (date ?? _clock.Today).Date;
        Items = await _db.ChecklistItems.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToListAsync();
        Entries = await _db.ChecklistEntries.Include(e => e.ChecklistItem).Include(e => e.Employee)
            .Where(e => e.Date == Date).OrderBy(e => e.CompletedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(string title, UserRole? role, ChecklistFrequency frequency, bool media)
    {
        var max = await _db.ChecklistItems.Select(i => (int?)i.SortOrder).MaxAsync() ?? 0;
        _db.ChecklistItems.Add(new ChecklistItem { Title = title.Trim(), Role = role, Frequency = frequency, RequiresMediaReport = media, SortOrder = max + 1 });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var i = await _db.ChecklistItems.FindAsync(id);
        if (i != null) { i.IsActive = !i.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile csv)
    {
        var max = await _db.ChecklistItems.Select(i => (int?)i.SortOrder).MaxAsync() ?? 0;
        var rows = await Csv.ReadAsync(csv);
        if (rows.Count > 0 && (rows[0][0].Contains("Կետ") || rows[0][0].Contains("Title", StringComparison.OrdinalIgnoreCase))) rows.RemoveAt(0);
        var n = 0;
        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r[0])))
        {
            _db.ChecklistItems.Add(new ChecklistItem
            {
                Title = row[0].Trim(),
                Role = row.Length > 1 && Enum.TryParse<UserRole>(row[1].Trim(), true, out var r) ? r : null,
                Frequency = row.Length > 2 && row[2].Trim().StartsWith("w", StringComparison.OrdinalIgnoreCase) ? ChecklistFrequency.Weekly : ChecklistFrequency.Daily,
                RequiresMediaReport = row.Length > 3 && row[3].Trim().ToLowerInvariant() is "1" or "yes" or "այո" or "true",
                SortOrder = ++max,
            });
            n++;
        }
        await _db.SaveChangesAsync();
        TempData["ok"] = $"Ներմուծված է {n} կետ";
        return RedirectToPage();
    }
}
