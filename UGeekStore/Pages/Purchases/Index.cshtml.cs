using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Purchases;

[Authorize(Roles = Roles.Production)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    private readonly CashService _cash;
    public IndexModel(AppDbContext db, Clock clock, CashService cash) { _db = db; _clock = clock; _cash = cash; }

    public List<PurchaseRequest> Requests { get; set; } = new();
    public List<Ingredient> Ingredients { get; set; } = new();
    public List<Ingredient> LowStock => Ingredients.Where(i => i.StockQty < i.MinQty).ToList();

    public async Task OnGetAsync()
    {
        var since = _clock.Today.AddDays(-30);
        Requests = await _db.PurchaseRequests.Include(r => r.CreatedBy)
            .Where(r => r.Status == PurchaseStatus.Requested || r.Status == PurchaseStatus.Approved || r.CreatedAt >= since)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        Ingredients = await _db.Ingredients.OrderBy(i => i.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string itemName, decimal quantity, string? unit, decimal? estimatedCost, string? supplier, string? notes)
    {
        var ing = await _db.Ingredients.FirstOrDefaultAsync(i => i.Name == itemName.Trim());
        _db.PurchaseRequests.Add(new PurchaseRequest
        {
            CreatedAt = _clock.Now, CreatedById = User.EmployeeId(), IngredientId = ing?.Id,
            ItemName = itemName.Trim(), Quantity = quantity, Unit = string.IsNullOrWhiteSpace(unit) ? ing?.Unit : unit.Trim(),
            EstimatedCost = estimatedCost ?? 0, Supplier = supplier?.Trim(), Notes = notes?.Trim(),
        });
        await _db.SaveChangesAsync();
        TempData["ok"] = "Հայտը ավելացված է";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, PurchaseStatus status)
    {
        if (!User.IsAny(Roles.Management)) return Forbid();
        var r = await _db.PurchaseRequests.FindAsync(id);
        if (r == null) return NotFound();
        if (status is PurchaseStatus.Approved or PurchaseStatus.Rejected) r.Status = status;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPurchasedAsync(int id, decimal actualCost, bool toCash)
    {
        if (!User.IsAny(Roles.Management)) return Forbid();
        var r = await _db.PurchaseRequests.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = PurchaseStatus.Purchased;
        r.ActualCost = actualCost;
        r.PurchasedAt = _clock.Now;
        if (r.IngredientId != null)
        {
            var ing = await _db.Ingredients.FindAsync(r.IngredientId);
            if (ing != null) ing.StockQty += r.Quantity;
        }
        await _db.SaveChangesAsync();

        if (toCash && actualCost > 0)
        {
            var reg = await _cash.ResolveRegisterAsync(null);
            if (reg != null)
            {
                var shift = await _cash.GetOrOpenShiftAsync(reg.Id, User.EmployeeId());
                await _cash.AddTransactionAsync(shift, new CashTransaction
                {
                    Type = CashMovementType.Expense, Category = "Գնում մատակարարից", Amount = actualCost,
                    Comment = $"{r.ItemName} {r.Quantity:0.##} {r.Unit} {r.Supplier}".Trim(),
                    CashierName = User.FullName(), EmployeeId = User.EmployeeId(),
                });
            }
        }
        TempData["ok"] = "Գնումը գրանցված է";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStockAsync(int id, decimal stockQty, decimal minQty)
    {
        var i = await _db.Ingredients.FindAsync(id);
        if (i == null) return NotFound();
        i.StockQty = stockQty; i.MinQty = minQty;
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostIngredientAsync(string name, string? unit, decimal? minQty)
    {
        if (!User.IsAny(Roles.Management)) return Forbid();
        _db.Ingredients.Add(new Ingredient { Name = name.Trim(), Unit = string.IsNullOrWhiteSpace(unit) ? "կգ" : unit.Trim(), MinQty = minQty ?? 0 });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
