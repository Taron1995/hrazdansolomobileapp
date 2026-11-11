using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Services;

namespace UGeekStore.Pages.Cash;

[Authorize(Roles = Roles.CashDesk)]
public class IndexModel : PageModel
{
    private readonly CashService _cash;
    private readonly Clock _clock;
    public IndexModel(CashService cash, Clock clock) { _cash = cash; _clock = clock; }

    public List<CashRegister> Registers { get; set; } = new();
    public CashRegister? Register { get; set; }
    public CashShift? Shift { get; set; }
    public ShiftTotals? Totals { get; set; }
    public DateTime Today { get; set; }

    public async Task OnGetAsync(int? registerId)
    {
        Today = _clock.Today;
        Registers = await _cash.RegistersAsync();
        Register = await _cash.ResolveRegisterAsync(registerId);
        if (Register == null) return;
        Shift = await _cash.OpenShiftAsync(Register.Id);
        if (Shift != null) Totals = CashService.Totals(Shift.Transactions);
    }
}
