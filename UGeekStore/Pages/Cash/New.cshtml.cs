using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Services;

namespace UGeekStore.Pages.Cash;

[Authorize(Roles = Roles.CashDesk)]
public class NewModel : PageModel
{
    private readonly CashService _cash;
    private readonly Clock _clock;
    public NewModel(CashService cash, Clock clock) { _cash = cash; _clock = clock; }

    [BindProperty(SupportsGet = true)] public int? RegisterId { get; set; }
    [BindProperty] public CashMovementType? Type { get; set; }
    [BindProperty] public string? Category { get; set; }
    [BindProperty, Required(ErrorMessage = "Գումարը պարտադիր է"), Range(1, 100_000_000, ErrorMessage = "Գումարը պետք է լինի դրական")]
    public decimal? Amount { get; set; }
    [BindProperty, MaxLength(1000)] public string? Comment { get; set; }
    [BindProperty, Required(ErrorMessage = "Գանձապահի անունը պարտադիր է")] public string CashierName { get; set; } = "";
    [BindProperty] public DateTime? DeviceTime { get; set; }

    public DateTime ShiftDate { get; set; }
    public DateTime ServerNow { get; set; }
    public string RegisterName { get; set; } = "";

    private async Task<CashRegister?> LoadAsync()
    {
        var reg = await _cash.ResolveRegisterAsync(RegisterId);
        if (reg == null) return null;
        RegisterId = reg.Id;
        RegisterName = reg.Name;
        ServerNow = _clock.Now;
        var open = await _cash.OpenShiftAsync(reg.Id);
        ShiftDate = open?.Date ?? ServerNow.Date;
        return reg;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await LoadAsync() == null) return NotFound();
        CashierName = User.FullName();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var reg = await LoadAsync();
        if (reg == null) return NotFound();
        if (Type == null) ModelState.AddModelError(nameof(Type), "Ընտրեք տեսակը (մուտք / ելք / պարտք)");
        else if (!CashCategories.IsValid(Type.Value, Category)) ModelState.AddModelError(nameof(Category), "Ընտրեք տարբերակը");
        if (!ModelState.IsValid) return Page();

        var shift = await _cash.GetOrOpenShiftAsync(reg.Id, User.EmployeeId());
        await _cash.AddTransactionAsync(shift, new CashTransaction
        {
            Type = Type!.Value,
            Category = Category!,
            Amount = Amount!.Value,
            Comment = Comment?.Trim(),
            CashierName = CashierName.Trim(),
            EmployeeId = User.EmployeeId(),
            DeviceTime = DeviceTime,
        });
        TempData["ok"] = $"Պահպանված է՝ {Type.Value.Hy()} · {Category} · {Amount:#,0} ֏";
        return Redirect($"/Cash?registerId={reg.Id}");
    }
}
