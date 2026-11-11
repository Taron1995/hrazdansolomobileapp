using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core.Entities;
using UGeekStore.Data;

namespace UGeekStore.Pages.Account;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public LoginModel(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
    public bool GoogleEnabled => !string.IsNullOrEmpty(_cfg["Authentication:Google:ClientId"]);

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = Email.Trim().ToLowerInvariant();
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Email.ToLower() == email && e.IsActive);
        if (emp?.PasswordHash == null ||
            new PasswordHasher<Employee>().VerifyHashedPassword(emp, emp.PasswordHash, Password) == PasswordVerificationResult.Failed)
        {
            Error = "Սխալ էլ. փոստ կամ գաղտնաբառ";
            return Page();
        }
        await SignInAsync(HttpContext, emp);
        return LocalRedirect(SafeReturn());
    }

    public IActionResult OnGetGoogle()
    {
        if (!GoogleEnabled) return RedirectToPage();
        var props = new AuthenticationProperties { RedirectUri = Url.Page("Login", "GoogleCallback", new { ReturnUrl }) };
        return Challenge(props, "Google");
    }

    public async Task<IActionResult> OnGetGoogleCallbackAsync()
    {
        var ext = await HttpContext.AuthenticateAsync(AuthSchemes.External);
        await HttpContext.SignOutAsync(AuthSchemes.External);
        var email = ext.Principal?.FindFirstValue(ClaimTypes.Email)?.ToLowerInvariant();
        if (email == null) { Error = "Google-ից էլ. փոստ չստացվեց"; return Page(); }
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Email.ToLower() == email && e.IsActive);
        if (emp == null)
        {
            Error = $"{email} հասցեն գրանցված չէ որպես աշխատակից։ Դիմեք ադմինիստրատորին։";
            return Page();
        }
        await SignInAsync(HttpContext, emp);
        return LocalRedirect(SafeReturn());
    }

    private string SafeReturn() => !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";

    public static Task SignInAsync(HttpContext http, Employee emp)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, emp.Id.ToString()),
            new(ClaimTypes.Name, emp.FullName),
            new(ClaimTypes.Email, emp.Email),
            new(ClaimTypes.Role, emp.Role.ToString()),
        };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id),
            new AuthenticationProperties { IsPersistent = true });
    }
}
