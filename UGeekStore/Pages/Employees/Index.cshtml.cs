using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;

namespace UGeekStore.Pages.Employees;

[Authorize(Roles = Roles.Admin)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;
    public List<Employee> Employees { get; set; } = new();
    public List<CashRegister> Registers { get; set; } = new();

    public async Task OnGetAsync()
    {
        Employees = await _db.Employees.OrderBy(e => e.FullName).ToListAsync();
        Registers = await _db.CashRegisters.OrderBy(r => r.Id).ToListAsync();
    }

    public async Task<IActionResult> OnPostRegisterAsync(int? id, string name, bool active)
    {
        if (string.IsNullOrWhiteSpace(name)) return RedirectToPage();
        var r = id == null ? new CashRegister() : await _db.CashRegisters.FindAsync(id);
        if (r == null) return NotFound();
        r.Name = name.Trim(); r.IsActive = active;
        if (id == null) _db.CashRegisters.Add(r);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }
}
