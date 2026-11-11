using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Customers;

[Authorize(Roles = Roles.CashDesk)]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public EditModel(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    [BindProperty] public Customer Customer { get; set; } = new();
    public List<Order> Orders { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return Page();
        var c = await _db.Customers.Include(x => x.Issues).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        Customer = c;
        Orders = await _db.Orders.Where(o => o.CustomerId == c.Id).OrderByDescending(o => o.DueAt).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        ModelState.Remove("Customer.Orders"); ModelState.Remove("Customer.Issues");
        var phone = Phone.Normalize(Customer.Phone);
        if (phone.Length < 8) ModelState.AddModelError("Customer.Phone", "Սխալ հեռախոսահամար");
        else if (await _db.Customers.AnyAsync(c => c.Phone == phone && c.Id != (id ?? 0)))
            ModelState.AddModelError("Customer.Phone", "Այս համարով հաճախորդ արդեն կա");
        if (!ModelState.IsValid) return Page();

        Customer entity;
        if (id == null) { entity = new Customer { CreatedAt = _clock.Now }; _db.Customers.Add(entity); }
        else entity = await _db.Customers.FindAsync(id) ?? throw new InvalidOperationException();
        entity.Name = Customer.Name.Trim();
        entity.Phone = phone;
        entity.Address = Customer.Address?.Trim();
        entity.District = Customer.District?.Trim();
        entity.Notes = Customer.Notes?.Trim();
        entity.MarketingConsent = Customer.MarketingConsent;
        await _db.SaveChangesAsync();
        TempData["ok"] = "Պահպանված է";
        return RedirectToPage(new { id = entity.Id });
    }

    public async Task<IActionResult> OnPostIssueAsync(int id, string description)
    {
        if (!string.IsNullOrWhiteSpace(description) && await _db.Customers.AnyAsync(c => c.Id == id))
        {
            _db.CustomerIssues.Add(new CustomerIssue { CustomerId = id, CreatedAt = _clock.Now, Description = description.Trim(), CreatedById = User.EmployeeId() });
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResolveAsync(int id, int issueId)
    {
        var i = await _db.CustomerIssues.FirstOrDefaultAsync(x => x.Id == issueId && x.CustomerId == id);
        if (i != null) { i.Resolved = true; await _db.SaveChangesAsync(); }
        return RedirectToPage(new { id });
    }
}
