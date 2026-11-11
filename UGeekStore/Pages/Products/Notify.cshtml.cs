using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Products;

[Authorize(Roles = Roles.Management)]
public class NotifyModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly MessagingService _msg;
    private readonly IConfiguration _cfg;
    public NotifyModel(AppDbContext db, MessagingService msg, IConfiguration cfg) { _db = db; _msg = msg; _cfg = cfg; }

    public Product Product { get; set; } = null!;
    public List<Customer> Recipients { get; set; } = new();
    public string Text { get; set; } = "";
    public bool WhatsAppApi => _msg.WhatsAppApiConfigured;
    public bool SmsApi => _msg.SmsConfigured;

    private string? PhotoUrl()
    {
        if (Product.PhotoPath == null) return null;
        var baseUrl = _cfg["App:PublicBaseUrl"];
        if (string.IsNullOrEmpty(baseUrl)) baseUrl = $"{Request.Scheme}://{Request.Host}";
        return baseUrl.TrimEnd('/') + Product.PhotoPath;
    }

    private async Task<bool> LoadAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;
        Product = p;
        Recipients = await _db.Customers.Where(c => c.MarketingConsent).OrderBy(c => c.Name).ToListAsync();
        var photo = PhotoUrl();
        Text = $"Նոր տեսականի՝ {p.Name}\nԳին՝ {p.Price:#,0} ֏" + (photo != null ? $"\n{photo}" : "");
        return true;
    }

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(int id, string channel, string? text)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (!string.IsNullOrWhiteSpace(text)) Text = text.Trim();
        int ok = 0;
        foreach (var c in Recipients)
        {
            var sent = channel == "sms"
                ? await _msg.SendSmsAsync(c.Phone, Text)
                : await _msg.SendWhatsAppAsync(c.Phone, Text, PhotoUrl());
            if (sent) ok++;
        }
        TempData[ok > 0 ? "ok" : "err"] = $"Ուղարկված է {ok} / {Recipients.Count}";
        return RedirectToPage(new { id });
    }
}
