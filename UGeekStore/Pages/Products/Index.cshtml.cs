using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;

namespace UGeekStore.Pages.Products;

[Authorize(Roles = Roles.Management)]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;
    public List<Product> Products { get; set; } = new();
    public async Task OnGetAsync() =>
        Products = await _db.Products.OrderByDescending(p => p.IsActive).ThenBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();
}
