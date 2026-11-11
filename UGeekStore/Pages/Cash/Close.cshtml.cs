using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Services;

namespace UGeekStore.Pages.Cash;

[Authorize(Roles = Roles.CashDesk)]
public class CloseModel : PageModel
{
    private readonly CashService _cash;
    public CloseModel(CashService cash) => _cash = cash;

    [BindProperty(SupportsGet = true)] public int? RegisterId { get; set; }
    [BindProperty] public int[] Counts { get; set; } = new int[Banknotes.Denominations.Length];
    [BindProperty] public string? Comment { get; set; }

    public CashShift? Shift { get; set; }
    public ShiftTotals? Totals { get; set; }
    public string RegisterName { get; set; } = "";

    private async Task<bool> LoadAsync()
    {
        var reg = await _cash.ResolveRegisterAsync(RegisterId);
        if (reg == null) return false;
        RegisterId = reg.Id; RegisterName = reg.Name;
        Shift = await _cash.OpenShiftAsync(reg.Id);
        if (Shift != null) Totals = CashService.Totals(Shift.Transactions);
        return true;
    }

    public async Task<IActionResult> OnGetAsync() => await LoadAsync() ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await LoadAsync()) return NotFound();
        if (Shift == null) return Redirect("/Cash");
        if (Counts.Length != Banknotes.Denominations.Length || Counts.Any(c => c < 0))
        {
            TempData["err"] = "Սխալ քանակներ";
            return Page();
        }
        await _cash.CloseShiftAsync(Shift, User.EmployeeId(), Counts, Comment?.Trim());
        TempData["ok"] = $"Կասսան փակված է։ Ընդհանուր՝ {Shift.CountedTotal:#,0} ֏";
        return Redirect("/");
    }
}
