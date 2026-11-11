using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;

namespace UGeekStore.Pages.Employees;

[Authorize(Roles = Roles.Admin)]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public int? Id { get; set; }
    [BindProperty, Required(ErrorMessage = "Անունը պարտադիր է")] public string FullName { get; set; } = "";
    [BindProperty, Required(ErrorMessage = "Էլ. փոստը պարտադիր է"), EmailAddress] public string Email { get; set; } = "";
    [BindProperty] public string? PhoneNumber { get; set; }
    [BindProperty] public UserRole Role { get; set; } = UserRole.Cashier;
    [BindProperty, MinLength(6, ErrorMessage = "Առնվազն 6 նիշ")] public string? Password { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id == null) return Page();
        var e = await _db.Employees.FindAsync(Id);
        if (e == null) return NotFound();
        (FullName, Email, PhoneNumber, Role, IsActive) = (e.FullName, e.Email, e.Phone, e.Role, e.IsActive);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = Email.Trim().ToLowerInvariant();
        if (await _db.Employees.AnyAsync(e => e.Email.ToLower() == email && e.Id != (Id ?? 0)))
            ModelState.AddModelError(nameof(Email), "Այս էլ. փոստով աշխատակից արդեն կա");
        if (!ModelState.IsValid) return Page();
        var emp = Id == null ? new Employee() : await _db.Employees.FindAsync(Id);
        if (emp == null) return NotFound();
        emp.FullName = FullName.Trim(); emp.Email = email; emp.Phone = PhoneNumber?.Trim(); emp.Role = Role; emp.IsActive = IsActive;
        if (!string.IsNullOrEmpty(Password)) emp.PasswordHash = new PasswordHasher<Employee>().HashPassword(emp, Password);
        if (Id == null) _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        TempData["ok"] = "Պահպանված է";
        return Redirect("/Employees");
    }
}
