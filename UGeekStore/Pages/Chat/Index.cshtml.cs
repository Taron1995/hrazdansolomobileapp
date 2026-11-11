using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core.Entities;
using UGeekStore.Data;

namespace UGeekStore.Pages.Chat;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;
    public List<ChatMessage> Messages { get; set; } = new();

    public async Task OnGetAsync()
    {
        Messages = await _db.ChatMessages.OrderByDescending(m => m.Id).Take(200).ToListAsync();
        Messages.Reverse();
    }
}
