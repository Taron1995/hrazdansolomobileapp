using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;

namespace UGeekStore.Services;

public record ChecklistRow(ChecklistItem Item, ChecklistEntry? Entry);

public static class ChecklistLogic
{
    public static DateTime WeekStart(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    /// <summary>Items for the role today, with the entry that completes them (today for daily, this week for weekly).</summary>
    public static async Task<List<ChecklistRow>> TodayAsync(AppDbContext db, UserRole role, DateTime today)
    {
        var items = await db.ChecklistItems.Where(i => i.IsActive && (i.Role == null || i.Role == role || role == UserRole.Admin))
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToListAsync();
        var week = WeekStart(today);
        var entries = await db.ChecklistEntries.Include(e => e.Employee).Where(e => e.Date >= week && e.Date <= today).ToListAsync();
        return items.Select(i => new ChecklistRow(i, entries
            .Where(e => e.ChecklistItemId == i.Id && (i.Frequency == ChecklistFrequency.Weekly || e.Date == today))
            .OrderByDescending(e => e.CompletedAt).FirstOrDefault())).ToList();
    }
}
