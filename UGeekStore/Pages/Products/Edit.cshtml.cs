using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Products;

[Authorize(Roles = Roles.Management)]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly FileStorage _files;
    private readonly Clock _clock;
    public EditModel(AppDbContext db, FileStorage files, Clock clock) { _db = db; _files = files; _clock = clock; }

    [BindProperty(SupportsGet = true)] public int? Id { get; set; }
    [BindProperty, Required(ErrorMessage = "Անվանումը պարտադիր է")] public string Name { get; set; } = "";
    [BindProperty] public string? Category { get; set; }
    [BindProperty, Range(0, 100_000_000)] public decimal Price { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public bool NotifyAfterSave { get; set; } = true;
    public string? PhotoPath { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id == null) return Page();
        var p = await _db.Products.FindAsync(Id);
        if (p == null) return NotFound();
        (Name, Category, Price, Description, IsActive, PhotoPath) = (p.Name, p.Category, p.Price, p.Description, p.IsActive, p.PhotoPath);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? photo)
    {
        Product? p = Id == null ? new Product { CreatedAt = _clock.Now } : await _db.Products.FindAsync(Id);
        if (p == null) return NotFound();
        PhotoPath = p.PhotoPath;
        if (!ModelState.IsValid) return Page();
        try { p.PhotoPath = await _files.SaveAsync(photo, "products") ?? p.PhotoPath; }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return Page(); }
        p.Name = Name.Trim(); p.Category = Category?.Trim(); p.Price = Price; p.Description = Description?.Trim(); p.IsActive = IsActive;
        if (Id == null) _db.Products.Add(p);
        await _db.SaveChangesAsync();
        TempData["ok"] = "Պահպանված է";
        return Id == null && NotifyAfterSave ? Redirect($"/Products/Notify/{p.Id}") : Redirect("/Products");
    }
}
