using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Checklist;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    private readonly FileStorage _files;
    public IndexModel(AppDbContext db, Clock clock, FileStorage files) { _db = db; _clock = clock; _files = files; }

    public DateTime Today { get; set; }
    public List<ChecklistRow> Rows { get; set; } = new();

    private UserRole Role => Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var r) ? r : UserRole.Cashier;

    public async Task OnGetAsync()
    {
        Today = _clock.Today;
        Rows = await ChecklistLogic.TodayAsync(_db, Role, Today);
    }

    public async Task<IActionResult> OnPostAsync(int itemId, string? comment, IFormFile? photo, IFormFile? video)
    {
        var item = await _db.ChecklistItems.FindAsync(itemId);
        if (item == null) return NotFound();
        var file = video?.Length > 0 ? video : photo;
        if (item.RequiresMediaReport && (file == null || file.Length == 0))
        {
            TempData["err"] = "Այս կետի համար անհրաժեշտ է նկար կամ վիդեո հաշվետվություն";
            return RedirectToPage();
        }
        string? path;
        try { path = await _files.SaveAsync(file, "checklist"); }
        catch (InvalidOperationException ex) { TempData["err"] = ex.Message; return RedirectToPage(); }
        _db.ChecklistEntries.Add(new ChecklistEntry
        {
            ChecklistItemId = itemId, Date = _clock.Today, CompletedAt = _clock.Now,
            EmployeeId = User.EmployeeId(), Comment = comment?.Trim(), ReportFilePath = path,
        });
        await _db.SaveChangesAsync();
        TempData["ok"] = $"«{item.Title}» — կատարված է";
        return RedirectToPage();
    }
}
